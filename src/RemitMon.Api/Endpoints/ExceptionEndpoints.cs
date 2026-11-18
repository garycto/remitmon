using Microsoft.EntityFrameworkCore;
using RemitMon.Api.Dtos;
using RemitMon.Domain.Enums;
using RemitMon.Infrastructure;

namespace RemitMon.Api.Endpoints;

public static class ExceptionEndpoints
{
    public static void MapExceptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/exceptions")
            .WithName("Exceptions");

        group.MapGet("/", GetExceptions)
            .WithName("GetExceptions")
            .WithSummary("Get all exceptions");

        group.MapGet("/{id}", GetException)
            .WithName("GetException")
            .WithSummary("Get exception by ID");

        group.MapPost("/{id}/approve", ApproveException)
            .WithName("ApproveException")
            .WithSummary("Approve exception");

        group.MapPost("/{id}/reject", RejectException)
            .WithName("RejectException")
            .WithSummary("Reject exception");
    }

    private static async Task<IResult> GetExceptions(RemitMonDbContext db)
    {
        var exceptions = await db.Exceptions
            .Include(e => e.Decision)
            .Select(e => new ExceptionDto(
                e.Id,
                e.DecisionId,
                e.Status.ToString(),
                e.AssignedToUserId,
                e.ExpiresAt,
                e.CreatedAt
            ))
            .ToListAsync();

        return Results.Ok(exceptions);
    }

    private static async Task<IResult> GetException(Guid id, RemitMonDbContext db)
    {
        var exception = await db.Exceptions
            .Include(e => e.Decision)
            .Where(e => e.Id == id)
            .Select(e => new ExceptionDto(
                e.Id,
                e.DecisionId,
                e.Status.ToString(),
                e.AssignedToUserId,
                e.ExpiresAt,
                e.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (exception is null)
            return Results.NotFound();

        return Results.Ok(exception);
    }

    private static async Task<IResult> ApproveException(Guid id, ApproveExceptionRequest request, RemitMonDbContext db)
    {
        var exception = await db.Exceptions.FirstOrDefaultAsync(e => e.Id == id);
        if (exception is null)
            return Results.NotFound();

        exception.Status = ExceptionStatus.Approved;
        exception.ResolutionNote = request.Note;
        exception.ResolvedAt = DateTime.UtcNow;
        exception.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new ExceptionDto(
            exception.Id,
            exception.DecisionId,
            exception.Status.ToString(),
            exception.AssignedToUserId,
            exception.ExpiresAt,
            exception.CreatedAt
        ));
    }

    private static async Task<IResult> RejectException(Guid id, RejectExceptionRequest request, RemitMonDbContext db)
    {
        var exception = await db.Exceptions.FirstOrDefaultAsync(e => e.Id == id);
        if (exception is null)
            return Results.NotFound();

        exception.Status = ExceptionStatus.Rejected;
        exception.ResolutionNote = $"{request.Reason}: {request.Note}";
        exception.ResolvedAt = DateTime.UtcNow;
        exception.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new ExceptionDto(
            exception.Id,
            exception.DecisionId,
            exception.Status.ToString(),
            exception.AssignedToUserId,
            exception.ExpiresAt,
            exception.CreatedAt
        ));
    }
}
