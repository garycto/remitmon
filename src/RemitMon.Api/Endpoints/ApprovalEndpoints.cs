using Microsoft.EntityFrameworkCore;
using RemitMon.Api.Dtos;
using RemitMon.Domain.Enums;
using RemitMon.Infrastructure;

namespace RemitMon.Api.Endpoints;

public static class ApprovalEndpoints
{
    public static void MapApprovalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/approvals")
            .WithName("Approvals");

        group.MapGet("/", GetApprovals)
            .WithName("GetApprovals")
            .WithSummary("Get all approvals");

        group.MapGet("/{id}", GetApproval)
            .WithName("GetApproval")
            .WithSummary("Get approval by ID");

        group.MapPost("/{id}/approve", ApproveApproval)
            .WithName("ApproveApproval")
            .WithSummary("Approve request");

        group.MapPost("/{id}/reject", RejectApproval)
            .WithName("RejectApproval")
            .WithSummary("Reject request");
    }

    private static async Task<IResult> GetApprovals(Guid? accountId, RemitMonDbContext db)
    {
        var approvalQuery = db.Approvals
            .AsNoTracking()
            .AsQueryable();
        if (accountId.HasValue)
            approvalQuery = approvalQuery.Where(approval =>
                approval.Decision != null
                && approval.Decision.PaymentEvent != null
                && approval.Decision.PaymentEvent.AccountId == accountId.Value);

        var approvals = await approvalQuery
            .Select(approval => new ApprovalDto(
                approval.Id,
                approval.DecisionId,
                approval.Status.ToString(),
                approval.AssignedToUserId,
                approval.ExpiresAt,
                approval.CreatedAt
            ))
            .ToListAsync();

        return Results.Ok(approvals);
    }

    private static async Task<IResult> GetApproval(Guid id, RemitMonDbContext db)
    {
        var approval = await db.Approvals
            .Include(item => item.Decision)
            .Where(item => item.Id == id)
            .Select(item => new ApprovalDto(
                item.Id,
                item.DecisionId,
                item.Status.ToString(),
                item.AssignedToUserId,
                item.ExpiresAt,
                item.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (approval is null)
            return Results.NotFound();

        return Results.Ok(approval);
    }

    private static async Task<IResult> ApproveApproval(Guid id, ApproveApprovalRequest request, RemitMonDbContext db)
    {
        var approval = await db.Approvals.FirstOrDefaultAsync(item => item.Id == id);
        if (approval is null)
            return Results.NotFound();

        approval.Status = ApprovalStatus.Approved;
        approval.ResolutionNote = request.Note;
        approval.ResolvedAt = DateTime.UtcNow;
        approval.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new ApprovalDto(
            approval.Id,
            approval.DecisionId,
            approval.Status.ToString(),
            approval.AssignedToUserId,
            approval.ExpiresAt,
            approval.CreatedAt
        ));
    }

    private static async Task<IResult> RejectApproval(Guid id, RejectApprovalRequest request, RemitMonDbContext db)
    {
        var approval = await db.Approvals.FirstOrDefaultAsync(item => item.Id == id);
        if (approval is null)
            return Results.NotFound();

        approval.Status = ApprovalStatus.Rejected;
        approval.ResolutionNote = $"{request.Reason}: {request.Note}";
        approval.ResolvedAt = DateTime.UtcNow;
        approval.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new ApprovalDto(
            approval.Id,
            approval.DecisionId,
            approval.Status.ToString(),
            approval.AssignedToUserId,
            approval.ExpiresAt,
            approval.CreatedAt
        ));
    }
}
