using Microsoft.AspNetCore.Authorization;

namespace KadirliApp.Api.Authorization;

public sealed record PermissionRequirement(string Module, string Action) : IAuthorizationRequirement;
