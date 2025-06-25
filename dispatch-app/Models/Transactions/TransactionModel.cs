namespace dispatch_app.Models.Transactions
{
    public class TransactionModel
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public string ReceiptId { get; set; }
        public double Qty { get; set; }
        public string CreatedDate { get; set; }
        public string Status { get; set; }
        public List<DetailTransactionModel> details { get; set; }
    }

    public class DetailTransactionModel
    {
        public string Sku { get; set; }
        public string Barcode { get; set; }
        public string Description { get; set; }
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Transfered { get; set; }
    }
}
