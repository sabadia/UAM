# Endpoint-to-Test Traceability

This map keeps `/api/v1` route coverage explicit and discoverable.

## Cross-cutting

- `GET /api/v1/health` -> `EndpointPlanTests.HealthCheck_Works_WithoutTenantHeader`
- Tenant header required for API groups -> `EndpointPlanTests.ApiRoutes_RequireTenantHeader`
- Auth required for API groups -> `EndpointPlanTests.ApiRoutes_RequireAuthentication`

## Users REST (`UserRoutesTests`)

- List/search/get/create/update/patch/delete/restore
- Status toggles: activate/deactivate
- Preferences: get/put
- Current profile: `GET /api/v1/me`, `PUT /api/v1/me`
- Missing-entity matrix (`UserMissingEndpointCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

## User gRPC (`UserGrpcServiceTests`)

- `CreateUser`, `GetUser`, `PatchUser`
- `DeleteUser`, `RestoreUser`
- `SearchUsers`
- `GetUserProfileSummary`
