using dispatch_app.Models.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace dispatch_app.Utils
{
    public class ProfileUtil
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileUtil(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> HasAccessAsync(HttpContext httpContext, string requiredRole)
        {
            var user = await _userManager.GetUserAsync(httpContext.User);
            if (user == null)
                return false;

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return true;

            if (roles.Contains(requiredRole))
                return true;

            if (roles.Contains("Supervisor") && (requiredRole == "Dispatcher"))
                return true;

            return false;
        }

        public async Task<(bool Success, string ErrorMessage)> SignInAsync(HttpContext httpContext, ApplicationUser user, string roleName)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Role, roleName)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Error al iniciar sesión: {ex.Message}");
            }
        }
    }
}