# RemitMon — Technical Decisions

## API Style
**Decision**: Minimal APIs with extension methods for endpoint registration.
**Rationale**: Cleaner separation of concerns and easier to test than controllers. Follows modern ASP.NET 8 patterns.

## Logging and Observability
**Decision**: Serilog with console + rolling file sinks.
**Rationale**: Structured logging, easy local development, and production-ready format for Application Insights integration.

## Entity Framework Core
**Decision**: EF Core 8 with Npgsql for PostgreSQL, snake_case table names, UUID primary keys.
**Rationale**: Strong typing, migration support, and PostgreSQL is the specified database.

## Rules and Policy Engine
**Decision**: Pure C# classes with immutable records for contexts and results.
**Rationale**: Determinism, testability, and no external dependencies per spec.

## Multi-Tenancy
**Decision**: Global EF Core query filters keyed by authenticated user (bank_id or business_customer_id).
**Rationale**: Ensures all queries respect tenant boundaries at the DbContext level.

## AuditableEntity Base
**Decision**: All entities inherit from `AuditableEntity` with `Id`, `CreatedAt`, `UpdatedAt`.
**Rationale**: Consistent audit trail across all entities; supports `AuditEntry` hash-chain verification.

## Money Representation
**Decision**: `decimal(18,2)` for all monetary amounts, USD-only in prototype.
**Rationale**: Financial precision and simplicity per spec.

## Status Enumerations
**Decision**: Separate enums for each entity status (AccountStatus, PolicyStatus, etc.).
**Rationale**: Type-safe, explicit, and easier to extend.
