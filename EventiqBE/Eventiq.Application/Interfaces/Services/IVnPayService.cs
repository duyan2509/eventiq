namespace Eventiq.Application.Interfaces.Services;

public interface IVnPayService
{
    /// <summary>
    /// Create VNPAY payment URL for checkout
    /// </summary>
    string CreatePaymentUrl(string paymentId, decimal amount, string orderInfo, string returnUrl, string ipnUrl);
    
    /// <summary>
    /// Verify IPN callback from VNPAY
    /// </summary>
    bool VerifyIpnCallback(Dictionary<string, string> vnpayData, string secureHash);
    
    /// <summary>
    /// Parse IPN callback data
    /// </summary>
    VnPayIpnResult ParseIpnCallback(Dictionary<string, string> queryParams);
}

public class VnPayIpnResult
{
    public string PaymentId { get; set; } = string.Empty; // vnp_TxnRef
    public string? TransactionNo { get; set; } // vnp_TransactionNo
    public string? ResponseCode { get; set; } // vnp_ResponseCode
    public decimal Amount { get; set; } // vnp_Amount (divided by 100)
    public string? SecureHash { get; set; } // vnp_SecureHash
    public string? BankCode { get; set; } // vnp_BankCode
    public string? CardType { get; set; } // vnp_CardType
    public DateTime? PayDate { get; set; } // vnp_PayDate
    public bool IsSuccess => ResponseCode == "00";
}

