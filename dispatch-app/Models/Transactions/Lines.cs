using System.ComponentModel.DataAnnotations;

namespace dispatch_app.Models.Transactions
{
    public class Lines
    {
        public int Id { get; set; }
        public int? HeaderId { get; set; } = 0;
        public int Line { get; set; } = 0;
        public Header Header { get; set; }
        [StringLength(50)]
        public string Sku { get; set; } = string.Empty;
        [StringLength(50)]
        public string Barcode { get; set; } = string.Empty;
        [StringLength(200)]
        public string? Description { get; set; } = string.Empty;
        [StringLength(500)]
        public string? Notes { get; set; } = string.Empty;
        [StringLength(25)]
        public string? Bin { get; set; } = string.Empty;
        public double? Quantity { get; set; } = 0;
        public DeliveryStatusEnum? Status { get; set; } = DeliveryStatusEnum.Pendiente;
        public bool CanBeDispatched { get; set; } = false;

        [StringLength(50)]
        public string? UserCode { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; } = DateTime.Now;
    }
}
