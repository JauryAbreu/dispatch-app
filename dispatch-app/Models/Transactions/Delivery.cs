using System.ComponentModel.DataAnnotations;

namespace dispatch_app.Models.Transactions
{
    public class Delivery
    {
        public int Id { get; set; }
        public int? StoreId { get; set; } = 1;
        public Store Store { get; set; } = new Store();
        public int? CustomerId { get; set; } = 1;
        public Customer Customer { get; set; } = new Customer();

        public int? HeaderId { get; set; } = 1;
        public Header Header { get; set; } = new Header();

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;
        [StringLength(50)]
        public string Email { get; set; } = string.Empty;
        [StringLength(50)]
        public string Address { get; set; } = string.Empty;
        [StringLength(50)]
        public string Address2 { get; set; } = string.Empty;
        [StringLength(50)]
        public string City { get; set; } = string.Empty;
    }
}
