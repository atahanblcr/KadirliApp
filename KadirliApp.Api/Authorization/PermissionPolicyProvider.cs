using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KadirliApp.Api.Authorization;

/// <summary>
/// "perm:{module}:{action}" adlı policy'leri uçuşta üretir; diğer tüm policy'ler
/// (AdminPanel, SuperAdmin) Program.cs'teki statik tanımlardan gelir.
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName[RequirePermissionAttribute.PolicyPrefix.Length..].Split(':');
            if (parts.Length == 2)
            {
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(parts[0], parts[1]))
                    .Build();
            }
        }

        return await base.GetPolicyAsync(policyName);
    }
}
