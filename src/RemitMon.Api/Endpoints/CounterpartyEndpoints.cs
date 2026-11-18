using Microsoft.EntityFrameworkCore;
using RemitMon.Api.Dtos;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
using RemitMon.Infrastructure;

namespace RemitMon.Api.Endpoints;

public static class CounterpartyEndpoints
{
    public static void MapCounterpartyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/counterparties")
            .WithName("Counterparties");

        group.MapGet("/business/{businessId}", GetCounterpartiesByBusiness)
            .WithName("GetCounterpartiesByBusiness")
            .WithSummary("Get counterparties for a business customer");

        group.MapGet("/{id}", GetCounterparty)
            .WithName("GetCounterparty")
            .WithSummary("Get counterparty by ID");

        group.MapPost("/", CreateCounterparty)
            .WithName("CreateCounterparty")
            .WithSummary("Create new counterparty");

        group.MapPost("/{id}/approve", ApproveCounterparty)
            .WithName("ApproveCounterparty")
            .WithSummary("Approve counterparty");

        group.MapPost("/{id}/block", BlockCounterparty)
            .WithName("BlockCounterparty")
            .WithSummary("Block counterparty");

        group.MapGet("/{id}/changes", GetCounterpartyChanges)
            .WithName("GetCounterpartyChanges")
            .WithSummary("Get change history for counterparty");
    }

    private static async Task<IResult> GetCounterpartiesByBusiness(Guid businessId, RemitMonDbContext db)
    {
        var counterparties = await db.Counterparties
            .Where(c => c.BusinessCustomerId == businessId)
            .Select(c => new CounterpartyDto(
                c.Id,
                c.BusinessCustomerId,
                c.Name,
                c.Status,
                c.Source,
                c.AchCompanyIds,
                c.RoutingAccountFingerprints,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .ToListAsync();

        return Results.Ok(counterparties);
    }

    private static async Task<IResult> GetCounterparty(Guid id, RemitMonDbContext db)
    {
        var counterparty = await db.Counterparties
            .Where(c => c.Id == id)
            .Select(c => new CounterpartyDto(
                c.Id,
                c.BusinessCustomerId,
                c.Name,
                c.Status,
                c.Source,
                c.AchCompanyIds,
                c.RoutingAccountFingerprints,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        if (counterparty is null)
            return Results.NotFound();

        return Results.Ok(counterparty);
    }

    private static async Task<IResult> CreateCounterparty(CreateCounterpartyRequest request, RemitMonDbContext db)
    {
        var counterparty = new Counterparty
        {
            BusinessCustomerId = request.BusinessCustomerId,
            Name = request.Name,
            Status = CounterpartyStatus.Pending,
            Source = CounterpartySource.Manual,
            AchCompanyIds = request.AchCompanyIds ?? new(),
            RoutingAccountFingerprints = request.RoutingAccountFingerprints ?? new()
        };

        db.Counterparties.Add(counterparty);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/counterparties/{counterparty.Id}", new CounterpartyDto(
            counterparty.Id,
            counterparty.BusinessCustomerId,
            counterparty.Name,
            counterparty.Status,
            counterparty.Source,
            counterparty.AchCompanyIds,
            counterparty.RoutingAccountFingerprints,
            counterparty.CreatedAt,
            counterparty.UpdatedAt
        ));
    }

    private static async Task<IResult> ApproveCounterparty(Guid id, ApproveCounterpartyRequest request, RemitMonDbContext db)
    {
        var counterparty = await db.Counterparties.FirstOrDefaultAsync(c => c.Id == id);
        if (counterparty is null)
            return Results.NotFound();

        counterparty.Status = CounterpartyStatus.Approved;
        counterparty.ApprovedAt = DateTime.UtcNow;
        counterparty.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new CounterpartyDto(
            counterparty.Id,
            counterparty.BusinessCustomerId,
            counterparty.Name,
            counterparty.Status,
            counterparty.Source,
            counterparty.AchCompanyIds,
            counterparty.RoutingAccountFingerprints,
            counterparty.CreatedAt,
            counterparty.UpdatedAt
        ));
    }

    private static async Task<IResult> BlockCounterparty(Guid id, BlockCounterpartyRequest request, RemitMonDbContext db)
    {
        var counterparty = await db.Counterparties.FirstOrDefaultAsync(c => c.Id == id);
        if (counterparty is null)
            return Results.NotFound();

        counterparty.Status = CounterpartyStatus.Blocked;
        counterparty.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new CounterpartyDto(
            counterparty.Id,
            counterparty.BusinessCustomerId,
            counterparty.Name,
            counterparty.Status,
            counterparty.Source,
            counterparty.AchCompanyIds,
            counterparty.RoutingAccountFingerprints,
            counterparty.CreatedAt,
            counterparty.UpdatedAt
        ));
    }

    private static async Task<IResult> GetCounterpartyChanges(Guid id, RemitMonDbContext db)
    {
        var changes = await db.CounterpartyChangeLogs
            .Where(cl => cl.CounterpartyId == id)
            .OrderByDescending(cl => cl.ChangedAt)
            .Select(cl => new
            {
                cl.Id,
                cl.Field,
                cl.OldValue,
                cl.NewValue,
                cl.ChangedAt
            })
            .ToListAsync();

        return Results.Ok(changes);
    }
}
