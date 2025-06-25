using dispatch_app.Models;
using dispatch_app.Models.Transactions;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dispatch_app.Controllers
{
    [MinimumRoleAuthorize(RoleLevel.Admin)]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string EntityName = "Cliente";

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<IActionResult> GetCustomerOrRedirectAsync(int? id, string viewName)
        {
            if (!id.HasValue)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de cliente no proporcionado.";
                return RedirectToAction("Index");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (customer == null)
            {
                TempData[MessageEnum.Warning.ToString()] = $"No se encontró el {EntityName.ToLower()}.";
                return RedirectToAction("Index");
            }

            return viewName == "View" ? View(customer) : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var customers = await _context.Customers
                    .AsNoTracking()
                    .ToListAsync();
                return View(customers);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar la lista de clientes: {ex.Message}";
                return View(new List<Customer>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,FirstName,LastName,VatNumber,Company,Email,Address,Phone,State,City")] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                TempData[MessageEnum.Warning.ToString()] = "Datos de entrada no válidos.";
                return View(customer);
            }

            try
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData[MessageEnum.Success.ToString()] = $"Operación completada: {EntityName} creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al crear el {EntityName.ToLower()}: {ex.Message}";
                return View(customer);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var result = await GetCustomerOrRedirectAsync(id, "View");
            if (result != null) return result;

            var customer = await _context.Customers.FindAsync(id);
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,FirstName,LastName,VatNumber,Company,Email,Address,Phone,State,City")] Customer customer)
        {
            if (id != customer.Id)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de cliente no coincide.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                TempData[MessageEnum.Warning.ToString()] = "Datos de entrada no válidos.";
                return View(customer);
            }

            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData[MessageEnum.Success.ToString()] = $"Operación completada: {EntityName} actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Customers.AnyAsync(c => c.Id == customer.Id))
                {
                    TempData[MessageEnum.Warning.ToString()] = $"No se encontró el {EntityName.ToLower()}.";
                    return RedirectToAction("Index");
                }
                TempData[MessageEnum.Error.ToString()] = "Error de concurrencia al actualizar el cliente.";
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al actualizar el {EntityName.ToLower()}: {ex.Message}";
                return View(customer);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            return await GetCustomerOrRedirectAsync(id, "View");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = $"No se encontró el {EntityName.ToLower()}.";
                    return RedirectToAction("Index");
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                TempData[MessageEnum.Success.ToString()] = $"Operación completada: {EntityName} eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al eliminar el {EntityName.ToLower()}: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}