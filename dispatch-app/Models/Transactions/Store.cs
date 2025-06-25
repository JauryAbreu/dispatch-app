using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace dispatch_app.Models.Transactions
{
    public class Store
    {
        public int Id { get; set; }
        [StringLength(25)]
        public string StoreId { get; set; } = string.Empty;
        [StringLength(100)]
        public string Description { get; set; } = string.Empty;
        [ValidateNever]
        public List<Delivery> Deliveries { get; set; }
    }
}
