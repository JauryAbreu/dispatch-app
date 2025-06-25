using Microsoft.AspNetCore.Mvc;

namespace dispatch_app.Models.Transactions
{
    public class DispatchItemsReportModel
    {
        public TransactionModel Transaction { get; set; }
        public DateTime GeneratedDate { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
