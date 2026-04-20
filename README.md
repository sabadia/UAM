# UAM Service

The UAM (User Account Management) service manages user profiles, preferences, and exposes a `UserAccess` gRPC service consumed by Identity, Slogtry, Storage, and EventNotification.

## Endpoints

All routes are prefixed `/api/v1`.

| Method | Route | Auth | Summary |
|--------|-------|------|---------|
| GET | `/users` | Bearer | List users (tenant-scoped) |
| GET | `/users/{id}` | Bearer | Get user by ID |
| POST | `/users` | Bearer | Create user profile |
| PUT | `/users/{id}` | Bearer | Update user profile |
| DELETE | `/users/{id}` | Bearer | Soft-delete user |
| GET | `/me` | Bearer | Get current user's profile |
| PUT | `/me` | Bearer | Update current user's profile |
| GET | `/health/ready` | Anonymous | Health check |

## gRPC Service

**UserAccess** (`UAM.Grpc.Users.V1`) — exposes user lookup and access validation. Requires `ApiAccessPolicy` authorization on the gRPC endpoint.

## Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=uam;Username=postgres;Password=postgres"
  },
  "RemoteServices": {
    "Tenant": { "GrpcEndpoint": "https://localhost:7134" },
    "Identity": { "GrpcEndpoint": "https://localhost:7133" }
  }
}
```

## Local Setup

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=uam;Username=postgres;Password=postgres"
dotnet ef database update --project Services/UAM
dotnet run --project Services/UAM
```

Development ports: `http://localhost:5265` (HTTP) / `https://localhost:7135` (HTTPS).

## Tests

```bash
dotnet test Services/UAM/UAM.Tests
```
