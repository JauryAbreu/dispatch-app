using dispatch_app.Models;
using dispatch_app.Models.Transactions;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dispatch_app.Controllers
{
    [MinimumRoleAuthorize(RoleLevel.Dispatcher)]
    public class ReportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly AuthUtil _authUtil;
        private readonly TransactionUtil _transactionUtil;
        private readonly ProfileUtil _profileUtil;
        private readonly SmtpSettings _smtpSettings;
        private readonly IConfiguration _configuration;

        public ReportController(
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

        [HttpGet]
        public async Task<IActionResult> DispatchReport(int? id)
        {
            if (!id.HasValue)
            {
                TempData[MessageEnum.Warning.ToString()] = "ID de orden no proporcionado.";
                return RedirectToAction("Orders", "Transactions");
            }

            try
            {
                var header = await _context.Headers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id.Value);

                if (header == null)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Orden no encontrada.";
                    return RedirectToAction("Orders", "Transactions");
                }

                ViewBag.Users = await _userManager.GetUsersInRoleAsync(RoleLevel.Dispatcher.ToString());
                var transaction = await _transactionUtil.GetCurrentTransactionAsync(_context, header, false);
                return View(transaction);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar el reporte de despacho: {ex.Message}";
                return RedirectToAction("Orders", "Transactions");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DispatchItemsReport(string startDate, string endDate)
        {
            var redirect = await CheckAccessAsync("Supervisor", "generar el reporte de ítems despachados");
            if (redirect != null) return redirect;

            try
            {
                DateTime today = DateTime.Now.Date;
                DateTime start = string.IsNullOrEmpty(startDate) ? today : DateTime.Parse(startDate);
                DateTime end = string.IsNullOrEmpty(endDate) ? today : DateTime.Parse(endDate).AddDays(1).AddSeconds(-1);

                if (start > end || end > today.AddDays(1) || (end - start).TotalDays > 32)
                {
                    TempData[MessageEnum.Warning.ToString()] = "Rango de fechas inválido.";
                    return View(new List<TransactionModel>());
                }

                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");

                var transactions = await _context.Headers
                    .AsNoTracking()
                    .Where(h => h.CreatedDate >= start.Date &&
                               h.CreatedDate <= end.Date.AddDays(1).AddSeconds(-1) &&
                               (h.Status != DeliveryStatusEnum.No_Aplica))
                    .OrderByDescending(h => h.CreatedDate)
                    .Select(h => new TransactionModel
                    {
                        Id = h.Id,
                        ReceiptId = h.ReceiptId,
                        Customer = _context.Customers
                            .Where(c => c.CustomerId == h.CustomerCode)
                            .Select(c => string.IsNullOrEmpty(c.Company) ? 
                                $"{c.LastName}, {c.FirstName} ".Trim() :
                                $"{c.VatNumber} - {c.Company}".Trim())
                            .FirstOrDefault() ?? string.Empty,
                        Qty = h.Quantity ?? 0,
                        Status = h.Status == DeliveryStatusEnum.Entrega_Completada ? "Completada" : "Parcial",
                        CreatedDate = Convert.ToDateTime(h.CreatedDate).ToString("dd-MM-yyyy") ?? string.Empty,
                        details = _context.Lines
                            .Where(l => l.HeaderId == h.Id)
                            .GroupBy(l => new { l.Sku, l.Barcode, l.Description })
                            .Select(g => new DetailTransactionModel
                            {
                                Sku = g.Key.Sku,
                                Barcode = g.Key.Barcode,
                                Description = g.Key.Description,
                                Total = (int)g.Sum(x => x.Quantity ?? 0),
                                Transfered = (int)g.Where(x => x.Status != DeliveryStatusEnum.Pendiente && x.Status != DeliveryStatusEnum.No_Aplica)
                                             .Sum(x => x.Quantity ?? 0),
                                Pending = (int)g.Where(x => x.Status == DeliveryStatusEnum.Pendiente)
                                          .Sum(x => x.Quantity ?? 0)
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return View(transactions);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar el reporte de ítems despachados: {ex.Message}";
                return View(new List<TransactionModel>());
            }
        }
    }
}