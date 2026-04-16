# Copilot instructions for UAM

## Build, test, and lint commands

Use .NET 10 commands from the repository root.

```bash
# Restore dependencies
dotnet restore UAM.sln

# Build application + tests
dotnet build UAM.sln

# Run full test suite
dotnet test UAM.Tests/UAM.Tests.csproj

# Run a single test method
dotnet test UAM.Tests/UAM.Tests.csproj --filter "FullyQualifiedName~UAM.Tests.UserRoutesTests.List_SearchIsCaseInsensitive"
```

No dedicated lint command is configured in this repository.

## High-level architecture

UAM is an ASP.NET Core Minimal API (`Program.cs`) composed through bootstrap extensions:

- `Bootstrap/ServiceCollectionExtensions.cs` wires OpenAPI/ProblemDetails, JSON conventions, DbContext, repositories, tenant provider, and domain services.
- `Bootstrap/ApplicationPipelineExtensions.cs` wires exception handling, OpenAPI/Scalar in development, tenant-header enforcement middleware, and API route registration.

HTTP routing is grouped under `/api/v1` in `Apis/ApiRegister.cs` with user route modules (`users` and `me`) delegating to a service layer in `Services/*`.

Persistence is EF Core with PostgreSQL in runtime (`Context/DbContext.cs`, `Npgsql`) and in-memory DB in tests (`UAM.Tests/EndpointTestSupport.cs`). `EfRepository<T>` is a thin query/add/find abstraction; business logic and validation live in services.

Multi-tenancy and soft delete are implemented at the data layer:

- Tenant header required: all `/api/v1/*` except `/api/v1/health` must include `X-Tenant-Id`.
- Global query filters apply `TenantId` and `!IsDeleted` to all entities.
- Restore paths use `includeDeleted`/`IgnoreQueryFilters()` flows to access soft-deleted rows.

## Key conventions in this codebase

- **Response shape:** non-health API endpoints return `ApiResponse<T>` / `PagedResponse<T>` envelopes via `RouteResults`; routes use `RouteExecution` helpers to map `InvalidOperationException` to HTTP 400.
- **Tenant behavior:** tenant scoping is automatic in `AppDbContext` via `ITenantProvider`; tests must set `X-Tenant-Id` on requests except health checks.
- **IDs and auditing:** entities inherit `BaseModel` (ULID string IDs). `AppDbContext.SaveChanges*` enforces tenant/audit fields and soft-delete metadata.
- **Validation style:** service/domain validation throws `InvalidOperationException` for client-facing bad requests; not-found flows generally return `null` and are mapped to HTTP 404 at route level.
- **Pagination/search style:** list endpoints use `OffsetPaginationQuery` normalization (`offset >= 0`, `limit <= 100`) and case-insensitive search helpers from `Services/Common`.
- **Design-time EF setup:** migrations use `AppDbContextFactory`, which loads connection strings from user-secrets/appsettings/environment and applies a fixed design-time tenant.
