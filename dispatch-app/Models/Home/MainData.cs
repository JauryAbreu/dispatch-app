using dispatch_app.Models.Transactions;

namespace dispatch_app.Models.Home
{
    public class MainData
    {
        public string UserId { get; set; }
        public int OrderLast30Days { get; set; }
        public int OrderLast7Days { get; set; }
        public int OrderLast1Days { get; set; }
        public List<TimeToDispatched> TimeToDispatcheds { get; set; }
        public List<TransactionModel> TransactionModels { get; set; }
        public List<ChartData> ChartData { get; set; }
    }

    public class ChartData
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
    public class TimeToDispatched
    {
        public string Date { get; set; }
        public string Description { get; set; }
    }
}
