using System.Security.Claims;
using Grpc.Core;
using Slogtry.Abstractions;
using Slogtry.Grpc;
using UAM.Dtos.Users;
using UAM.Grpc.Users.V1;
using DtoUserTheme = UAM.Dtos.Users.UserTheme;
using GrpcUserTheme = UAM.Grpc.Users.V1.UserTheme;

namespace UAM.Services.Users;

public sealed class UserAccessGrpcService(
    IUserService userService,
    ITenantContextAccessor tenantContextAccessor) : UserAccess.UserAccessBase
{
    public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var user = await userService.GetAsync(request.UserId, request.IncludeDeleted, context.CancellationToken);
            if (user is null) throw NotFound($"User '{request.UserId}' was not found.");

            return new GetUserResponse
            {
                User = MapUser(user)
            };
        });
    }

    public override async Task<BatchGetUsersResponse> BatchGetUsers(BatchGetUsersRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var users = await userService.BatchGetAsync(request.UserIds, request.IncludeDeleted, context.CancellationToken);
            var response = new BatchGetUsersResponse();
            response.Users.AddRange(users.Select(MapUser));
            return response;
        });
    }

    public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var user = await userService.CreateAsync(new UserCreateRequest(
                request.ExternalAuthUserId,
                request.Email,
                request.DisplayName,
                NullIfEmpty(request.FirstName),
                NullIfEmpty(request.LastName),
                NullIfEmpty(request.PhoneNumber),
                request.IsActive,
                request.Preferences is null ? null : MapPreferencesRequest(request.Preferences)), context.CancellationToken);

            return new CreateUserResponse
            {
                User = MapUser(user)
            };
        });
    }

    public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var user = await userService.UpdateAsync(request.UserId, new UserUpdateRequest(
                request.Email,
                request.DisplayName,
                NullIfEmpty(request.FirstName),
                NullIfEmpty(request.LastName),
                NullIfEmpty(request.PhoneNumber),
                request.Preferences is null ? null : MapPreferencesRequest(request.Preferences)), context.CancellationToken);

            if (user is null) throw NotFound($"User '{request.UserId}' was not found.");

            return new UpdateUserResponse
            {
                User = MapUser(user)
            };
        });
    }

    public override async Task<PatchUserResponse> PatchUser(PatchUserRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var preferencesPatch = request.PreferencesPatch is null
                ? null
                : new UserPreferencesPatchRequest(
                    request.PreferencesPatch.HasLanguage ? request.PreferencesPatch.Language : null,
                    request.PreferencesPatch.HasTimeZone ? request.PreferencesPatch.TimeZone : null,
                    request.PreferencesPatch.HasTheme ? ToDtoTheme(request.PreferencesPatch.Theme) : null,
                    request.PreferencesPatch.HasEmailNotificationsEnabled ? request.PreferencesPatch.EmailNotificationsEnabled : null,
                    request.PreferencesPatch.HasSmsNotificationsEnabled ? request.PreferencesPatch.SmsNotificationsEnabled : null);

            var patch = request.Patch is null
                ? new UserPatchRequest(Preferences: preferencesPatch)
                : new UserPatchRequest(
                    request.Patch.HasEmail ? request.Patch.Email : null,
                    request.Patch.HasDisplayName ? request.Patch.DisplayName : null,
                    request.Patch.HasFirstName ? request.Patch.FirstName : null,
                    request.Patch.HasLastName ? request.Patch.LastName : null,
                    request.Patch.HasPhoneNumber ? request.Patch.PhoneNumber : null,
                    preferencesPatch);

            var user = await userService.PatchAsync(request.UserId, patch, context.CancellationToken);
            if (user is null) throw NotFound($"User '{request.UserId}' was not found.");

            return new PatchUserResponse
            {
                User = MapUser(user)
            };
        });
    }

    public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var success = await userService.DeleteAsync(request.UserId, context.CancellationToken);
            if (!success) throw NotFound($"User '{request.UserId}' was not found.");

            return new DeleteUserResponse
            {
                Success = true
            };
        });
    }

    public override async Task<RestoreUserResponse> RestoreUser(RestoreUserRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var user = await userService.RestoreAsync(request.UserId, context.CancellationToken);
            if (user is null) throw NotFound($"User '{request.UserId}' was not found.");

            return new RestoreUserResponse
            {
                User = MapUser(user)
            };
        });
    }

    public override async Task<SetUserStatusResponse> SetUserStatus(SetUserStatusRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var user = await userService.SetActiveAsync(request.UserId, request.IsActive, context.CancellationToken);
            if (user is null) throw NotFound($"User '{request.UserId}' was not found.");

            return new SetUserStatusResponse
            {
                User = MapUser(user)
            };
        });
    }

    public override async Task<SearchUsersResponse> SearchUsers(SearchUsersRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var page = await userService.ListAsync(new UserListQuery(
                request.Offset,
                request.Limit,
                NullIfEmpty(request.Search),
                request.HasIsActive ? request.IsActive : null,
                request.IncludeDeleted,
                NullIfEmpty(request.SortBy),
                string.IsNullOrWhiteSpace(request.SortDirection) ? "desc" : request.SortDirection), context.CancellationToken);

            var response = new SearchUsersResponse
            {
                TotalCount = page.TotalCount,
                Offset = page.Offset,
                Limit = page.Limit,
                HasMore = page.HasMore
            };
            response.Users.AddRange(page.Items.Select(MapUser));
            return response;
        });
    }

    public override async Task<GetUserProfileSummaryResponse> GetUserProfileSummary(GetUserProfileSummaryRequest request, ServerCallContext context)
    {
        return await ExecuteAsync(context, async () =>
        {
            var summary = await userService.GetProfileSummaryAsync(request.UserId, context.CancellationToken);
            if (summary is null) throw NotFound($"User '{request.UserId}' was not found.");

            return new GetUserProfileSummaryResponse
            {
                Summary = new UserProfileSummaryRecord
                {
                    Id = summary.Id,
                    ExternalAuthUserId = summary.ExternalAuthUserId,
                    Email = summary.Email,
                    DisplayName = summary.DisplayName,
                    IsActive = summary.IsActive
                }
            };
        });
    }

    private async Task<T> ExecuteAsync<T>(ServerCallContext context, Func<Task<T>> action)
    {
        try
        {
            EnsureTenantContext(context);
            return await action();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private void EnsureTenantContext(ServerCallContext context)
    {
        if (!string.IsNullOrWhiteSpace(tenantContextAccessor.Current?.TenantId))
            return;

        var httpContext = context.GetHttpContext();
        var tenantClaim = httpContext.User.FindFirstValue(TenancyConstants.TenantClaimName)?.Trim();
        var tenantHeader = httpContext.Request.Headers[TenancyConstants.TenantHeaderName].ToString().Trim();
        var tenantId = !string.IsNullOrWhiteSpace(tenantClaim) ? tenantClaim : tenantHeader;

        if (string.IsNullOrWhiteSpace(tenantId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Missing required header '{TenancyConstants.TenantHeaderName}'."));

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? httpContext.User.FindFirstValue("sub")
                     ?? "grpc-system";

        tenantContextAccessor.SetCurrent(new TenantContext(tenantId, userId, [], []));
    }

    private static RpcException NotFound(string message)
        => new(new Status(StatusCode.NotFound, message));

    private static UserPreferencesRequest MapPreferencesRequest(UserPreferences source)
    {
        return new UserPreferencesRequest(
            source.Language,
            source.TimeZone,
            ToDtoTheme(source.Theme),
            source.EmailNotificationsEnabled,
            source.SmsNotificationsEnabled);
    }

    private static UserRecord MapUser(UserResponse source)
    {
        return new UserRecord
        {
            Id = source.Id,
            TenantId = source.TenantId,
            ExternalAuthUserId = source.ExternalAuthUserId,
            Email = source.Email,
            DisplayName = source.DisplayName,
            FirstName = source.FirstName ?? string.Empty,
            LastName = source.LastName ?? string.Empty,
            PhoneNumber = source.PhoneNumber ?? string.Empty,
            IsActive = source.IsActive,
            Preferences = new UserPreferences
            {
                Language = source.Preferences.Language,
                TimeZone = source.Preferences.TimeZone,
                Theme = ToGrpcTheme(source.Preferences.Theme),
                EmailNotificationsEnabled = source.Preferences.EmailNotificationsEnabled,
                SmsNotificationsEnabled = source.Preferences.SmsNotificationsEnabled
            },
            IsDeleted = source.IsDeleted,
            DeletedAt = source.DeletedAt?.ToString("O") ?? string.Empty,
            DeletedBy = source.DeletedBy ?? string.Empty,
            CreatedAt = source.CreatedAt.ToString("O"),
            UpdatedAt = source.UpdatedAt.ToString("O"),
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy
        };
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static DtoUserTheme ToDtoTheme(GrpcUserTheme theme)
    {
        return theme switch
        {
            GrpcUserTheme.System => DtoUserTheme.System,
            GrpcUserTheme.Light => DtoUserTheme.Light,
            GrpcUserTheme.Dark => DtoUserTheme.Dark,
            _ => DtoUserTheme.System
        };
    }

    private static GrpcUserTheme ToGrpcTheme(DtoUserTheme theme)
    {
        return theme switch
        {
            DtoUserTheme.System => GrpcUserTheme.System,
            DtoUserTheme.Light => GrpcUserTheme.Light,
            DtoUserTheme.Dark => GrpcUserTheme.Dark,
            _ => GrpcUserTheme.System
        };
    }
}
