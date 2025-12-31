namespace Eventiq.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid CheckoutId { get; set; }
    public virtual Checkout Checkout { get; set; }
    
    public Guid UserId { get; set; }
    public Guid EventItemId { get; set; }
    public virtual EventItem EventItem { get; set; }
    
    // VNPAY fields
    public string PaymentId { get; set; } = string.Empty; // vnp_TxnRef
    public string? VnpTransactionNo { get; set; } // vnp_TransactionNo
    public string? VnpResponseCode { get; set; } // vnp_ResponseCode
    public string? VnpSecureHash { get; set; } // vnp_SecureHash
    
    // Amount fields
    public decimal GrossAmount { get; set; } // 
    public decimal PlatformFee { get; set; } 
    public decimal OrgAmount { get; set; } 
    
    // Payment status
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    
    // VNPAY callback data
    public DateTime? PaidAt { get; set; }
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    
    // IPN verification
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
}

public enum PaymentStatus
{
    PENDING,    
    SUCCESS,    
    FAILED,     
    CANCELLED   
}

