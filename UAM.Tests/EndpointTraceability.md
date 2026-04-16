# Endpoint-to-Test Traceability

This map keeps `/api/v1` route coverage explicit and discoverable.

## Cross-cutting

- `GET /api/v1/health` -> `EndpointPlanTests.HealthCheck_Works_WithoutTenantHeader`
- Tenant header required for API groups -> `EndpointPlanTests.ApiRoutes_RequireTenantHeader`

## Stories (`StoryRoutesTests`)

- List/get/create/update/delete/restore/publish/unpublish
- Counters: views/likes/dislikes
- Relations: category/series/cover/tag/source attach+detach
- Story comments list/add
- Missing-entity matrices, relation-missing matrices
- Update payload partition matrix (`StoryUpdatePayloadCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

## Series (`SeriesRoutesTests`)

- List/get/create/update/delete/restore/publish/unpublish
- Child list: stories
- Missing-entity matrix
- Update payload partition matrix (`SeriesUpdatePayloadCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

## Categories (`CategoryRoutesTests`)

- List/get/create/update/delete/restore
- Child lists: stories/series
- Missing-entity matrix
- Update payload partition matrix (`CategoryUpdatePayloadCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

## Tags (`TagRoutesTests`)

- List/get/create/update/delete/restore
- Child list: stories
- Missing-entity matrix
- Update payload partition matrix (`TagUpdatePayloadCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

## Comments (`CommentRoutesTests`)

- List/get/create/update/delete/restore
- Missing-entity matrix
- Update payload partition matrix (`CommentUpdatePayloadCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

## Files (`FileRoutesTests`)

- List/get/create/update/delete/restore
- Missing-entity matrix
- Update payload partition matrix (`FileUpdatePayloadCases`)
- Delete/restore sequence matrix (`DeleteRestoreSequenceCases`)

