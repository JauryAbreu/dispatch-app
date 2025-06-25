using dispatch_app.Models;
using dispatch_app.Models.Transactions;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dispatch_app.Controllers
{
    [MinimumRoleAuthorize(RoleLevel.Dispatcher)]
    public class TransactionsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly AuthUtil _authUtil;
        private readonly TransactionUtil _transactionUtil;
        private readonly ProfileUtil _profileUtil;
        private readonly SmtpSettings _smtpSettings;
        private readonly IConfiguration _configuration;

        public TransactionsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            AuthUtil authUtil,
            TransactionUtil transactionUtil,
            ProfileUtil profileUtil,
            IOptions<SmtpSettings> smtpSettings,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _authUtil = authUtil;
            _transactionUtil = transactionUtil;
            _profileUtil = profileUtil;
            _smtpSettings = smtpSettings.Value;
            _configuration = configuration;
        }

        private async Task<IActionResult> CheckAccessAsync(string role, string actionDescription)
        {
            if (!await _profileUtil.HasAccessAsync(HttpContext, role))
            {
                TempData[MessageEnum.Warning.ToString()] = $"No tiene permisos para {actionDescription}.";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        private async Task<Header?> GetHeaderAsync(int? id, string? userId)
        {
            if (id.HasValue)
                return await _context.Headers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id.Value);

            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.Headers
                .AsNoTracking()
                .Where(h => h.CreatedDate >= DateTime.Now.Date)
                .OrderBy(h => h.IsAssigned == true ? 0 : 1)
                .ThenBy(h => h.CreatedDate)
                .FirstOrDefaultAsync(h =>
                    (h.IsAssigned == true && h.UserCode == userId) ||
                    (h.IsAssigned == true && string.IsNullOrEmpty(h.UserCode)) ||
                    true);
        }

        private async Task UpdateHeaderLinesAsync(Header header, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lines = await _context.Lines
                    .Where(l => l.HeaderId == header.Id && l.Status == DeliveryStatusEnum.Pendiente)
                    .ToListAsync();

                header.Quantity = lines.Sum(l => l.Quantity ?? 0);
                header.IsRecalculate = true;
                _context.Update(header);

                var groupedLines = lines
                    .GroupBy(l => new { l.Sku, l.Barcode, l.Description, l.Notes })
                    .Select((g, index) => new Lines
                    {
                        HeaderId = header.Id,
                        Line = index + 1,
                        Sku = g.Key.Sku,
                        Barcode = g.Key.Barcode,
                        Description = g.Key.Description,
                        Notes = g.Key.Notes,
                        Quantity = g.Sum(l => l.Quantity ?? 0),
                        Status = DeliveryStatusEnum.Pendiente,
                        CanBeDispatched = true,
                        UserCode = userId,
                        CreatedDate = g.First().CreatedDate,
                        UpdatedDate = DateTime.Now
                    })
                    .ToList();

                _context.Lines.RemoveRange(lines);
                await _context.Lines.AddRangeAsync(groupedLines);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error al actualizar las líneas del encabezado: {ex.Message}", ex);
            }
        }

        private async Task EnrichHeadersAsync(IEnumerable<Header> headers)
        {
            var customerIds = headers.Select(h => h.CustomerCode).Distinct().ToList();
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToDictionaryAsync(c => c.CustomerId, c => c);

            foreach (var header in headers)
            {
                header.customer = customers.GetValueOrDefault(header.CustomerCode, new Customer());
                if (header.IsRecalculate == false)
                {
                    var userId = await _context.Users
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync();
                    await UpdateHeaderLinesAsync(header, userId ?? string.Empty);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var redirect = await CheckAccessAsync("Dispatcher", "ver las órdenes pendientes");
            if (redirect != null) return redirect;

            try
            {
                var headers = await _context.Headers
                    .AsNoTracking()
                    .Where(h => h.CreatedDate >= DateTime.Now.Date && h.Status == DeliveryStatusEnum.Pendiente)
                    .OrderBy(h => h.IsAssigned)
                    .ThenBy(h => h.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                await EnrichHeadersAsync(headers);
                return View(headers);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar las órdenes: {ex.Message}";
                return View(new List<Header>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            var redirect = await CheckAccessAsync("Dispatcher", "despachar una orden");
            if (redirect != null) return redirect;

            try
            {
                var userId = _authUtil.GetIdFromToken(HttpContext.Session.GetString("JWToken") ?? string.Empty);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData[MessageEnum.Warning.ToString()] = "Sesión inválida.";
                    return RedirectToAction("Index");
                }

                var header = await GetHeaderAsync(id, userId);
                if (header == null)
                {
                    TempData[MessageEnum.Information.ToString()] = "No existen órdenes pendientes.";
                    return RedirectToAction("Index");
                }

                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header);
                return View(transaction);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar la orden: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LinesRequest request)
        {
            var redirect = await CheckAccessAsync("Dispatcher", "despachar una orden");
            if (redirect != null) return redirect;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var header = await _context.Headers.FindAsync(request.Id);
                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Create", new { id = request.Id });
                }

                var userId = _authUtil.GetIdFromToken(HttpContext.Session.GetString("JWToken") ?? string.Empty);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData[MessageEnum.Warning.ToString()] = "Sesión inválida.";
                    return RedirectToAction("Create", new { id = request.Id });
                }

                var currentTransaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header);
                var newLines = new List<Lines>();
                int lineNumber = 0;

                foreach (var item in request.Lines)
                {
                    var currentItem = currentTransaction.details
                        .FirstOrDefault(d => d.Sku == item.Sku && d.Barcode == item.Barcode && d.Description == item.Description);

                    if (currentItem == null || currentItem.Pending < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        TempData[MessageEnum.Warning.ToString()] = $"Cantidad inválida para Sku: {item.Sku}, Descripción: {item.Description}";
                        return RedirectToAction("Create", new { id = request.Id });
                    }

                    newLines.Add(new Lines
                    {
                        Line = ++lineNumber,
                        HeaderId = request.Id,
                        Sku = item.Sku,
                        Barcode = item.Barcode,
                        Quantity = item.Quantity,
                        Description = item.Description,
                        Status = item.Status,
                        UserCode = userId,
                        Notes = string.Empty,
                        CanBeDispatched = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    });

                    header.QuantityDispatched += item.Quantity;
                }

                header.QuantityPending = (header.Quantity ?? 0) - header.QuantityDispatched;
                header.Status = header.QuantityPending <= 0 ? DeliveryStatusEnum.Entrega_Completada : DeliveryStatusEnum.Entrega_Parcial;
                header.UpdatedDate = DateTime.Now;

                _context.Lines.AddRange(newLines);
                _context.Headers.Update(header);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData[MessageEnum.Success.ToString()] = "Orden procesada correctamente.";
                return RedirectToAction("Create", new { id = request.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData[MessageEnum.Error.ToString()] = $"Error al procesar la orden: {ex.Message}";
                return RedirectToAction("Create", new { id = request.Id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Orders(DateTime? startDate, DateTime? endDate)
        {
            return RedirectToAction("Orders", new
            {
                startDate = startDate?.ToString("yyyy-MM-dd"),
                endDate = endDate?.ToString("yyyy-MM-dd")
            });
        }

        [HttpGet]
        public async Task<IActionResult> Orders(string startDate, string endDate)
        {
            var redirect = await CheckAccessAsync("Supervisor", "ver el historial de órdenes");
            if (redirect != null) return redirect;

            try
            {
                DateTime today = DateTime.Now.Date;
                DateTime start = string.IsNullOrEmpty(startDate) ? today.AddDays(-7) : DateTime.Parse(startDate);
                DateTime end = string.IsNullOrEmpty(endDate) ? today : DateTime.Parse(endDate).AddDays(1).AddSeconds(-1);

                if (start > end || end > today.AddDays(1) || (end - start).TotalDays > 32)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Rango de fechas inválido.";
                    return View(new List<Header>());
                }

                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");

                var headers = await _context.Headers
                    .AsNoTracking()
                    .Where(h => h.CreatedDate >= start && h.CreatedDate <= end && h.Status != DeliveryStatusEnum.No_Aplica)
                    .OrderBy(h => h.CreatedDate)
                    .ToListAsync();

                var customerIds = headers.Select(h => h.CustomerCode).Distinct().ToList();
                var receiptIds = headers.Select(h => h.ReceiptId).Distinct().ToList();

                var customers = await _context.Customers
                    .AsNoTracking()
                    .Where(c => customerIds.Contains(c.CustomerId))
                    .ToDictionaryAsync(c => c.CustomerId, c => c);

                var fiscals = await _context.Fiscal
                    .AsNoTracking()
                    .Where(f => receiptIds.Contains(f.ReceiptId))
                    .ToDictionaryAsync(f => f.ReceiptId, f => f);

                foreach (var header in headers)
                {
                    header.customer = customers.GetValueOrDefault(header.CustomerCode, new Customer());
                    header.fiscal = fiscals.GetValueOrDefault(header.ReceiptId, new Fiscal());
                }

                return View(headers);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar el historial de órdenes: {ex.Message}";
                return View(new List<Header>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserOrders()
        {
            var redirect = await CheckAccessAsync("Dispatcher", "ver el listado de órdenes");
            if (redirect != null) return redirect;

            try
            {
                DateTime start = DateTime.Now.Date;
                DateTime end = start.AddDays(1).AddSeconds(-1);

                var headers = await _context.Headers
                    .AsNoTracking()
                    .Where(h => h.CreatedDate >= start &&
                               h.CreatedDate <= end &&
                               (h.Status == DeliveryStatusEnum.En_Proceso || h.Status == DeliveryStatusEnum.Entrega_Parcial))
                    .OrderBy(h => h.CreatedDate)
                    .ToListAsync();

                var customerIds = headers.Select(h => h.CustomerCode).Distinct().ToList();
                var receiptIds = headers.Select(h => h.ReceiptId).Distinct().ToList();

                var customers = await _context.Customers
                    .AsNoTracking()
                    .Where(c => customerIds.Contains(c.CustomerId))
                    .ToDictionaryAsync(c => c.CustomerId, c => c);

                var fiscals = await _context.Fiscal
                    .AsNoTracking()
                    .Where(f => receiptIds.Contains(f.ReceiptId))
                    .ToDictionaryAsync(f => f.ReceiptId, f => f);

                foreach (var header in headers)
                {
                    header.customer = customers.GetValueOrDefault(header.CustomerCode, new Customer());
                    header.fiscal = fiscals.GetValueOrDefault(header.ReceiptId, new Fiscal());
                }

                return View(headers);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar las órdenes del usuario: {ex.Message}";
                return View(new List<Header>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int? id)
        {
            var redirect = await CheckAccessAsync("Supervisor", "ver detalles de la orden");
            if (redirect != null) return redirect;

            if (!id.HasValue)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de orden no proporcionado.";
                return RedirectToAction("Orders");
            }

            try
            {
                var header = await _context.Headers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id.Value);

                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Orders");
                }

                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header, false);
                return View(transaction);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar detalles de la orden: {ex.Message}";
                return RedirectToAction("Orders");
            }
        }

        [HttpGet]
        public async Task<IActionResult> StatusComplete(int? id)
        {
            var redirect = await CheckAccessAsync("Supervisor", "cambiar el estado a completado");
            if (redirect != null) return redirect;

            if (!id.HasValue)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de orden no proporcionado.";
                return RedirectToAction("Orders");
            }

            try
            {
                var header = await _context.Headers.FirstOrDefaultAsync(h => h.Id == id.Value);
                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Orders");
                }

                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header, false);
                int qtyPending = transaction.details.Sum(d => d.Pending);

                if (header.Status != DeliveryStatusEnum.Entrega_Completada && qtyPending <= 0)
                {
                    header.Status = DeliveryStatusEnum.Entrega_Completada;
                    header.UserCode = _authUtil.GetIdFromToken(HttpContext.Session.GetString("JWToken") ?? string.Empty);
                    _context.Headers.Update(header);
                    await _context.SaveChangesAsync();
                    TempData[MessageEnum.Success.ToString()] = $"Estado de la orden {header.ReceiptId} actualizado a Completada.";
                }
                else
                {
                    TempData[MessageEnum.Warning.ToString()] = $"No se pudo completar la orden {header.ReceiptId}. Cantidad pendiente: {qtyPending:N0}.";
                }

                return RedirectToAction("Orders", new
                {
                    startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
                    endDate = DateTime.Now.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al actualizar el estado de la orden: {ex.Message}";
                return RedirectToAction("Orders");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCreatedDate(int? id)
        {
            var redirect = await CheckAccessAsync("Supervisor", "actualizar la fecha de la orden");
            if (redirect != null) return redirect;

            if (!id.HasValue)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de orden no proporcionado.";
                return RedirectToAction("Orders");
            }

            try
            {
                var header = await _context.Headers.FirstOrDefaultAsync(h => h.Id == id.Value);
                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Orders");
                }

                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header, false);
                int qtyPending = transaction.details.Sum(d => d.Pending);

                if (header.Status != DeliveryStatusEnum.Entrega_Completada && qtyPending > 0)
                {
                    header.Status = DeliveryStatusEnum.Pendiente;
                    header.CreatedDate = DateTime.Now;
                    header.UpdatedDate = DateTime.Now;
                    _context.Headers.Update(header);
                    await _context.SaveChangesAsync();
                    TempData[MessageEnum.Success.ToString()] = $"Fecha de la orden {header.ReceiptId} actualizada correctamente.";
                }
                else
                {
                    TempData[MessageEnum.Warning.ToString()] = $"No se pudo actualizar la fecha de la orden {header.ReceiptId}. Cantidad pendiente: {qtyPending:N0}.";
                }

                return RedirectToAction("Orders", new
                {
                    startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
                    endDate = DateTime.Now.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al actualizar la fecha de la orden: {ex.Message}";
                return RedirectToAction("Orders");
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderAssignUser(int id, string? user)
        {
            var redirect = await CheckAccessAsync("Supervisor", "asignar un despachador a una orden");
            if (redirect != null) return redirect;

            if (id == 0)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de orden no proporcionado.";
                return RedirectToAction("Orders");
            }

            try
            {
                var header = await _context.Headers.FirstOrDefaultAsync(h => h.Id == id);
                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Orders");
                }

                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header, false);
                int qtyPending = transaction.details.Sum(d => d.Pending);

                if (header.Status != DeliveryStatusEnum.Entrega_Completada && qtyPending > 0)
                {
                    header.UserCode = user ?? string.Empty;
                    header.IsAssigned = true;
                    header.Status = DeliveryStatusEnum.Pendiente;
                    _context.Headers.Update(header);
                    await _context.SaveChangesAsync();
                    TempData[MessageEnum.Success.ToString()] = $"Despachador asignado a la orden {header.ReceiptId}.";
                }
                else
                {
                    TempData[MessageEnum.Warning.ToString()] = $"No se puede asignar un despachador a la orden {header.ReceiptId} completada.";
                }

                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al asignar despachador: {ex.Message}";
                return RedirectToAction("Orders");
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderAssign(int? id)
        {
            var redirect = await CheckAccessAsync("Supervisor", "asignar una orden");
            if (redirect != null) return redirect;

            if (!id.HasValue)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de orden no proporcionado.";
                return RedirectToAction("Orders");
            }

            try
            {
                var header = await _context.Headers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id.Value);

                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Orders");
                }

                ViewBag.Users = await _userManager.GetUsersInRoleAsync(RoleLevel.Dispatcher.ToString());
                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header, false);
                return View(transaction);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar la orden: {ex.Message}";
                return RedirectToAction("Orders");
            }
        }
    }
}