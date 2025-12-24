using System.ComponentModel.DataAnnotations;

namespace PaymentApi.Models;

public class VoidPaymentRequestDto
{
    [Required]
    public Guid BillingId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal AmountToVoid { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? ServerId { get; set; }
}
