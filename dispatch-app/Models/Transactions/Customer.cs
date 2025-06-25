using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace dispatch_app.Models.Transactions
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string? LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? VatNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Company { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; } = string.Empty;

        [StringLength(50)]
        public string? State { get; set; } = string.Empty;

        [StringLength(50)]
        public string? City { get; set; } = string.Empty;

        [ValidateNever]
        public List<Delivery> Deliveries { get; set; } = new();
    }
}

