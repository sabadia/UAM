# UAM Microservice Template

This directory contains a `dotnet new` template for a tenant-aware .NET 10 microservice.

## Included scaffold

- ASP.NET Core Minimal API
- `X-Tenant-Id` + tenant JWT auth pipeline
- Tenant and identity authorization through gRPC clients
- EF Core + PostgreSQL DbContext with tenant/soft-delete query filters
- One sample module: `stories`
- xUnit test project with in-memory test host and fake remote clients

## Install template locally

```bash
dotnet new install /Users/mahamudul/work/dotnet/UAM.Template
```

## Generate a new microservice

```bash
dotnet new uam-ms --name CatalogService --output ./CatalogService
```

## Generated project setup

```bash
cd ./CatalogService
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=catalog;Username=postgres;Password=postgres"
dotnet restore
dotnet build
dotnet test CatalogService.Tests/CatalogService.Tests.csproj
dotnet run --project CatalogService.csproj
```

## Runtime notes

- `GET /api/v1/health` is anonymous.
- Other `/api/v1/*` routes require:
  - `Authorization: Bearer <jwt>`
  - `X-Tenant-Id`
- JWT signing keys and tenant access checks are resolved via gRPC endpoints configured under `RemoteServices`.
