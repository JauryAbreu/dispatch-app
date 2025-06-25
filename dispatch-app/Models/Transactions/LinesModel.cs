namespace dispatch_app.Models.Transactions
{
    public class LinesModel
    {
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public string Bin { get; set; } = "";
        public DeliveryStatusEnum Status { get; set; } = DeliveryStatusEnum.Pendiente;
    }
}
