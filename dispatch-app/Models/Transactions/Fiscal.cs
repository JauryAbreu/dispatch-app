using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace dispatch_app.Models.Transactions
{
    public class Fiscal
    {
        public int Id { get; set; }
        [StringLength(25)]
        public string ReceiptId { get; set; } = string.Empty;
        [StringLength(15)]
        public string NCFNumber { get; set; } = string.Empty;
    }
}
