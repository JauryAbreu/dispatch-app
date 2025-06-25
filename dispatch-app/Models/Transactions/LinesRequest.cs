namespace dispatch_app.Models.Transactions
{
    public class LinesRequest
    {
        public int Id { get; set; }
        public List<LinesModel> Lines { get; set; }
    }
}
