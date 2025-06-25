using dispatch_app.Models;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace dispatch_app.Controllers
{
    public class SecurityController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthUtil _authUtil;
        private readonly EmailUtil _emailUtil;
        private readonly IConfiguration _configuration;

        public SecurityController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<SmtpSettings> smtpSettings,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _authUtil = new AuthUtil(configuration, userManager);
            _emailUtil = new EmailUtil(smtpSettings.Value);
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData[MessageEnum.Warning.ToString()] = "Datos de entrada no válidos.";
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                    user = await _userManager.FindByNameAsync(model.Email);

                if (user == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Correo o contraseña incorrectos.";
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Correo o contraseña incorrectos.";
                    return View(model);
                }

                // Verificar si el usuario tiene roles
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Any())
                {
                    TempData[MessageEnum.Warning.ToString()] = "No tiene un rol asignado. Contacte al administrador para obtener acceso.";
                    return View(model);
                }

                var (token, expiration) = await _authUtil.GenerateJwtToken(_userManager, user);
                HttpContext.Session.SetString("JWToken", token);
                HttpContext.Session.SetString("GroupId", roles.FirstOrDefault());

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Warning.ToString()] = $"Error al iniciar sesión: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData[MessageEnum.Warning.ToString()] = "Datos de entrada no válidos.";
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData[MessageEnum.Information.ToString()] = "Si el correo existe, se enviará un enlace para restablecer la contraseña.";
                    return RedirectToAction("Login");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Security", new { token, email = user.Email }, protocol: Request.Scheme);

                var emailBody = $@"<p>Hola {user.FirstName},</p>
                    <p>Recibimos una solicitud para restablecer tu contraseña. Haz clic en el siguiente enlace para continuar:</p>
                    <p><a href='{callbackUrl}'>Restablecer Contraseña</a></p>
                    <p>Si no solicitaste este cambio, ignora este correo.</p>
                    <p>Saludos,<br>Equipo Dispatch App</p>";

                var (success, errorMessage) = await _emailUtil.SendEmailAsync(user.Email, "Restablecer Contraseña", emailBody);
                if (!success)
                {
                    TempData[MessageEnum.Error.ToString()] = errorMessage;
                    return View(model);
                }

                TempData[MessageEnum.Success.ToString()] = "Se ha enviado un enlace para restablecer tu contraseña.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al procesar la solicitud: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData[MessageEnum.Warning.ToString()] = "Enlace de restablecimiento inválido.";
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData[MessageEnum.Warning.ToString()] = "Datos de entrada no válidos.";
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Usuario no encontrado.";
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                if (!result.Succeeded)
                {
                    TempData[MessageEnum.Warning.ToString()] = string.Join(", ", result.Errors.Select(e => e.Description));
                    return View(model);
                }

                TempData[MessageEnum.Success.ToString()] = "Contraseña restablecida correctamente.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al restablecer la contraseña: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Remove("JWToken");
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cerrar sesión: {ex.Message}";
            }
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            var errorMessage = HttpContext.Items["ErrorMessage"]?.ToString() ?? "Acceso denegado. Contacte al administrador.";
            TempData[MessageEnum.Warning.ToString()] = errorMessage;
            return View();
        }
    }
}