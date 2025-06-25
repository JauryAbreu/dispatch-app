using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace dispatch_app.Controllers
{
    // Usar el atributo personalizado
    [MinimumRoleAuthorize(RoleLevel.Dispatcher)]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private IActionResult HandleIdentityErrors(IdentityResult result, string viewName, object model, string errorMessagePrefix)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            TempData["Error"] = $"{errorMessagePrefix} error.";
            return View(viewName, model);
        }

        // GET: /Account/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Security");

            var model = new UpdateProfileModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone
            };

            return View(model);
        }

        // POST: /Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UpdateProfileModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Security");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Phone = model.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Perfil actualizado correctamente.";
                return View(model);
            }

            return HandleIdentityErrors(result, "EditProfile", model, "Error al actualizar el perfil");
        }

        // Cambiar contraseña (similar)
        [HttpGet]
        public IActionResult EditPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPassword(UpdatePasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Security");

            if (model.NewPassword != model.RepeatPassword)
            {
                TempData["Error"] = "La nueva contraseña y su confirmación no coinciden.";
                return View(model);
            }

            if (model.LastPassword == model.NewPassword)
            {
                TempData["Error"] = "La nueva contraseña no puede ser igual a la contraseña actual.";
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.LastPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Contraseña actualizada correctamente.";
                return RedirectToAction(nameof(EditPassword));
            }

            return HandleIdentityErrors(result, nameof(EditPassword), model, "Error al actualizar la contraseña");
        }
    }
}
