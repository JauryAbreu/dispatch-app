using dispatch_app.Models;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace dispatch_app.Controllers
{
    [MinimumRoleAuthorize(RoleLevel.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ProfileUtil _profileUtil;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _profileUtil = new ProfileUtil(userManager);
        }

        private async Task<List<SelectListItem>> GetRolesAsync()
        {
            return await _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();
        }

        private async Task<IActionResult> CheckAccessAsync(string actionDescription)
        {
            if (!await _profileUtil.HasAccessAsync(HttpContext, "Admin"))
            {
                TempData[MessageEnum.Warning.ToString()] = $"No tiene permisos para {actionDescription}.";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }


        private IActionResult HandleIdentityErrors(IdentityResult result, string viewName, object model, string errorMessagePrefix)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            TempData["Error"] = $"{errorMessagePrefix} error.";
            return View(viewName, model);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var redirect = await CheckAccessAsync("listar usuarios");
            if (redirect != null) return redirect;

            try
            {
                var users = await _userManager.Users
                    .ToListAsync();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    user.RoleName = roles.FirstOrDefault() ?? "Sin rol";
                }

                return View(users);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar la lista de usuarios: {ex.Message}";
                return View(new List<ApplicationUser>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var redirect = await CheckAccessAsync("crear");
            if (redirect != null) return redirect;

            ViewBag.Roles = await GetRolesAsync();
            return View(new CreateUserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserModel model)
        {
            var redirect = await CheckAccessAsync("crear");
            if (redirect != null) return redirect;
            ViewBag.Roles = await GetRolesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return HandleIdentityErrors(IdentityResult.Failed(), "Create", model, "Correo ya existe");

            if (await _userManager.FindByNameAsync(model.UserName) != null)
                return HandleIdentityErrors(IdentityResult.Failed(), "Create", model, "Nombre de usuario ya existe");

            if (string.IsNullOrEmpty(model.RoleName) || !await _roleManager.RoleExistsAsync(model.RoleName))
                return HandleIdentityErrors(IdentityResult.Failed(), "Create", model, "Rol no válido");

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return HandleIdentityErrors(result, "Create", model, "Error al crear usuario");

            await _userManager.AddToRoleAsync(user, model.RoleName);
            TempData["Success"] = "Usuario creado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var redirect = await CheckAccessAsync("editar");
            if (redirect != null) return redirect;

            if (string.IsNullOrEmpty(id))
            {
                TempData["Warning"] = "ID de usuario no proporcionado.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Warning"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = await GetRolesAsync();
            var roles = await _userManager.GetRolesAsync(user);

            var model = new UpdateProfileModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                RoleName = roles.FirstOrDefault()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateProfileModel model)
        {
            var redirect = await CheckAccessAsync("editar");
            if (redirect != null) return redirect;

            ViewBag.Roles = await GetRolesAsync();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return HandleIdentityErrors(IdentityResult.Failed(), "Edit", model, "Usuario no existe");

            if (string.IsNullOrEmpty(model.RoleName) || !await _roleManager.RoleExistsAsync(model.RoleName))
                return HandleIdentityErrors(IdentityResult.Failed(), "Edit", model, "Rol no válido");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Phone = model.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return HandleIdentityErrors(result, "Edit", model, "Error al actualizar usuario");

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.RoleName))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.RoleName);
            }

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var redirect = await CheckAccessAsync("eliminar");
            if (redirect != null) return redirect;

            if (string.IsNullOrEmpty(id))
            {
                TempData["Warning"] = "ID de usuario no proporcionado.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Warning"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var redirect = await CheckAccessAsync("eliminar");
            if (redirect != null) return redirect;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Warning"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Error al eliminar usuario.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Usuario eliminado correctamente.";
            return RedirectToAction("Index");
        }
    }
}