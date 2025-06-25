using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace dispatch_app.Utils
{
    public class MinimumRoleAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly RoleLevel _requiredRole;

        public MinimumRoleAuthorizeAttribute(RoleLevel requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Si el usuario no está autenticado, redirigir al login
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Security", null);
                return;
            }

            // Obtener el rol del usuario desde los claims
            var role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
            {
                context.HttpContext.Items["ErrorMessage"] = "No tiene un rol asignado. Contacte al administrador.";
                context.Result = new RedirectToActionResult("AccessDenied", "Security", null);
                return;
            }

            // Verificar si el rol cumple con el nivel mínimo requerido
            if (!HasMinimumRole(role))
            {
                context.HttpContext.Items["ErrorMessage"] = $"No tiene permisos suficientes. Se requiere el rol {_requiredRole} o superior.";
                context.Result = new RedirectToActionResult("AccessDenied", "Security", null);
                return;
            }
        }

        private bool HasMinimumRole(string userRole)
        {
            // Admin tiene acceso a todo
            if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Comparar roles basados en RoleLevel
            if (Enum.TryParse<RoleLevel>(userRole, true, out var userRoleEnum))
            {
                return userRoleEnum >= _requiredRole;
            }

            return false;
        }
    }
}