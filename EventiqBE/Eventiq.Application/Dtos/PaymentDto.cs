namespace Eventiq.Application.Dtos;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid CheckoutId { get; set; }
    public Guid UserId { get; set; }
    public Guid EventItemId { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string? VnpTransactionNo { get; set; }
    public string? VnpResponseCode { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal OrgAmount { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime? PaidAt { get; set; }
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentUrlRequest
{
    public Guid CheckoutId { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
}

public class PaymentUrlResponse
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
}

