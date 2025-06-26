using dispatch_app.Models;
using dispatch_app.Models.Transactions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace dispatch_app.Utils
{
    public class TransactionUtil
    {
        public async Task<TransactionModel> GetCurrentTransactionAsync(ApplicationDbContext context, Header header, bool updateData = true)
        {
            header.customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == header.CustomerCode) ?? new Customer();

            header.Lines = await context.Lines
                .AsNoTracking()
                .Where(l => l.HeaderId == header.Id)
                .ToListAsync();

            if (header.IsRecalculate == false && updateData && header.Status != DeliveryStatusEnum.Entrega_Completada)
            {
                header.Status = DeliveryStatusEnum.En_Proceso;
                header.UpdatedDate = DateTime.Now;
                context.Update(header);
                await context.SaveChangesAsync();
            }

            var groupedDetails = header.Lines
                .GroupBy(l => new { l.Sku, l.Barcode, l.Description })
                .Select(g => new DetailTransactionModel
                {
                    Sku = g.Key.Sku,
                    Barcode = g.Key.Barcode,
                    Description = g.Key.Description,
                    Total = (int)g.Where(x => x.Status == DeliveryStatusEnum.Pendiente).Sum(x => x.Quantity ?? 0),
                    Transfered = (int)g.Where(x => x.Status == DeliveryStatusEnum.Entrega_Completada)
                                 .Sum(x => x.Quantity ?? 0)
                })
                .ToList();
            foreach (var item in groupedDetails)
                item.Pending = item.Total - item.Transfered;

            return new TransactionModel
            {
                Id = header.Id,
                Qty = header.Quantity ?? 0,
                ReceiptId = header.ReceiptId,
                Customer = string.IsNullOrEmpty(header.customer.Company)
                    ? $"{header.customer.FirstName} {header.customer.LastName}".Trim()
                    : header.customer.Company,
                CreatedDate = header.CreatedDate?.ToString("dd-MM-yyyy hh:mm tt") ?? "Sin fecha",
                Status = header.Status.ToString().Replace("_", " "),
                details = groupedDetails
            };
        }
    }
}