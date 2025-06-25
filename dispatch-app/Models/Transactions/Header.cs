using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dispatch_app.Models.Transactions
{
    public class Header
    {
        public int Id { get; set; }
        [StringLength(25)]
        public string ReceiptId { get; set; } = string.Empty;
        [StringLength(10)]
        public string StoreCode { get; set; } = string.Empty;
        [StringLength(50)]
        public string CustomerCode { get; set; } = string.Empty;
        public double? Quantity { get; set; } = 0;
        public double? QuantityPending { get; set; } = 0;
        public double? QuantityDelivery { get; set; } = 0;
        public double? QuantityDispatched { get; set; } = 0;
        public DeliveryStatusEnum? Status { get; set; } = DeliveryStatusEnum.No_Aplica;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? UserCode { get; set; } = string.Empty;
        public bool? IsAssigned { get; set; } = false;
        public bool? IsRecalculate { get; set; } = false;
        public bool? IsDelivery { get; set; } = false;

        [ValidateNever]
        [NotMapped]
        public Customer customer { get; set; }
        [ValidateNever]
        [NotMapped]
        public Fiscal fiscal { get; set; }
        [ValidateNever]
        public List<Lines> Lines { get; set; }
        [ValidateNever]
        public List<Delivery> Deliveries { get; set; }
    }
}
