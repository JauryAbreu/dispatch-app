using dispatch_app.Models;
using dispatch_app.Models.Home;
using dispatch_app.Models.Transactions;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dispatch_app.Controllers
{
    [MinimumRoleAuthorize(RoleLevel.Dispatcher)]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly AuthUtil _authUtil;

        public HomeController(
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _authUtil = new AuthUtil(configuration, userManager);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? code)
        {
            try
            {
                string? userId = User.IsInRole(RoleLevel.Dispatcher.ToString())
                    ? _authUtil.GetIdFromToken(HttpContext.Session.GetString("JWToken") ?? string.Empty)
                    : code;

                if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(code))
                {
                    TempData[MessageEnum.Warning.ToString()] = "Código de usuario inválido.";
                    return View(new MainData());
                }

                var mainData = await GetMainDataAsync(userId);
                return View(mainData);
            }
            catch (Exception ex)
            {
                TempData[MessageEnum.Error.ToString()] = $"Error al cargar el dashboard: {ex.Message}";
                return View(new MainData());
            }
        }

        public async Task<MainData> GetMainDataAsync(string? userId)
        {
            var today = DateTime.Now.Date;
            var headersQuery = _context.Headers
                .AsNoTracking()
                .Where(h => h.IsRecalculate == true);

            if (!string.IsNullOrEmpty(userId))
                headersQuery = headersQuery.Where(h => h.UserCode == userId);

            var headers = await headersQuery
                .OrderByDescending(h => h.CreatedDate)
                .ToListAsync();

            var customerIds = headers.Select(h => h.CustomerCode).Distinct().ToList();
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .Select(c => new { c.CustomerId, c.Company })
                .ToDictionaryAsync(c => c.CustomerId, c => c.Company);

            return new MainData
            {
                UserId = userId,
                OrderLast1Days = headers.Count(h => h.CreatedDate >= today),
                OrderLast7Days = headers.Count(h => h.CreatedDate >= today.AddDays(-7)),
                OrderLast30Days = headers.Count(h => h.CreatedDate >= today.AddMonths(-1)),
                TimeToDispatcheds = headers
                    .Where(h => h.QuantityDispatched == h.Quantity || h.Status == DeliveryStatusEnum.Entrega_Completada)
                    .OrderByDescending(h => h.CreatedDate)
                    .Take(10)
                    .Select(h => new TimeToDispatched
                    {
                        Date = h.CreatedDate?.ToString("hh:mm tt") ?? "Sin fecha",
                        Description = $"Recibo: {h.ReceiptId}, Cantidad: {h.Quantity}"
                    })
                    .ToList(),
                TransactionModels = headers
                    .Take(10)
                    .Select(h => new TransactionModel
                    {
                        Id = h.Id,
                        ReceiptId = h.ReceiptId,
                        Customer = customers.GetValueOrDefault(h.CustomerCode, "Sin cliente"),
                        Qty = h.Quantity ?? 0,
                        Status = h.Status == DeliveryStatusEnum.Entrega_Completada ? "Completada" : "Pendiente",
                        CreatedDate = h.CreatedDate?.ToString("dd-MM-yyyy hh:mm tt") ?? "Sin fecha"
                    })
                    .OrderByDescending(t => t.Status)
                    .ThenBy(t => t.ReceiptId)
                    .ToList(),
                ChartData = Enumerable.Range(0, 10)
                    .Select(offset =>
                    {
                        var startDate = today.AddDays(-((offset + 1) * 3 - 1));
                        var endDate = today.AddDays(-offset * 3);
                        return new ChartData
                        {
                            Description = $"{startDate:dd-MMM}",
                            Amount = headers.Count(h => h.CreatedDate.HasValue &&
                                                       h.CreatedDate.Value.Date >= startDate &&
                                                       h.CreatedDate.Value.Date <= endDate),
                            Date = startDate
                        };
                    })
                    .OrderByDescending(cd => cd.Date)
                    .ToList()
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(model);
        }
    }
}