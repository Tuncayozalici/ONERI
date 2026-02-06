using Microsoft.Extensions.DependencyInjection;
using ONERI.Models.Authorization;

namespace ONERI.Extensions
{
    public static class AuthorizationServiceCollectionExtensions
    {
        public static IServiceCollection AddAppAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                foreach (var permission in Permissions.All)
                {
                    options.AddPolicy(permission.Key, policy =>
                    {
                        policy.RequireAssertion(context =>
                            context.User.IsInRole(Permissions.SuperAdminRole) ||
                            context.User.HasClaim(Permissions.ClaimType, permission.Key));
                    });
                }
            });

            return services;
        }
    }
}
