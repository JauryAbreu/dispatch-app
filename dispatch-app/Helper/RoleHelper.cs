using dispatch_app.Utils;
using System.Security.Claims;

namespace dispatch_app.Helper
{
    public static class RoleHelper
    {
        private static readonly Dictionary<string, RoleLevel> RoleHierarchy = new()
    {
        { "Dispatcher", RoleLevel.Dispatcher },
        { "Supervisor", RoleLevel.Supervisor },
        { "Admin", RoleLevel.Admin }
    };

        public static bool HasMinimumRole(this ClaimsPrincipal user, RoleLevel requiredRole)
        {
            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);

            foreach (var role in roles)
            {
                if (RoleHierarchy.TryGetValue(role, out var roleLevel))
                {
                    if (roleLevel >= requiredRole)
                        return true;
                }
            }

            return false;
        }
    }
}
