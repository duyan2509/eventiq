using System.Net;
using System.Security.Cryptography;
using System.Text;
using Eventiq.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Services;

public class VnPayService : IVnPayService
{
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _paymentUrl;
    private readonly string _version;
    private readonly string _command;
    private readonly string _currCode;
    private readonly string _locale;
    private readonly string _hashType;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(IConfiguration configuration, ILogger<VnPayService> logger)
    {
        _tmnCode = configuration["VnPay:TmnCode"] ?? throw new ArgumentNullException("VnPay:TmnCode");
        _hashSecret = configuration["VnPay:HashSecret"] ?? throw new ArgumentNullException("VnPay:HashSecret");
        _paymentUrl = configuration["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        _version = configuration["VnPay:Version"] ?? "2.1.0";
        _command = configuration["VnPay:Command"] ?? "pay";
        _currCode = configuration["VnPay:CurrCode"] ?? "VND";
        _locale = configuration["VnPay:Locale"] ?? "vn";
        _hashType = configuration["VnPay:HashType"] ?? "MD5"; // Default to MD5 for Sandbox
        _logger = logger;
        _logger.LogInformation("VnPayService initialized - TmnCode: {TmnCode}, HashSecret: {HashSecret}, HashType: {HashType}, PaymentUrl: {PaymentUrl}", _tmnCode, _hashSecret, _hashType, _paymentUrl);
    }

    public string CreatePaymentUrl(string paymentId, decimal amount, string orderInfo, string returnUrl, string ipnUrl)
    {
        var vnpayData = new Dictionary<string, string>
        {
            { "vnp_Version", _version },
            { "vnp_Command", _command },
            { "vnp_TmnCode", _tmnCode },
            { "vnp_Amount", ((long)(amount*100)).ToString() }, // Convert to cents
            { "vnp_CurrCode", _currCode },
            { "vnp_TxnRef", paymentId },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", _locale },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_IpAddr", "127.0.0.1" }, // Should get from request in production
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
        };

        // Sort and build query string WITH URL encoding (as per VNPay example)
        var sortedData = vnpayData.OrderBy(x => x.Key).ToList();
        
        // Build query string with URL encoding (like VNPay example does)
        var queryStringBuilder = new StringBuilder();
        foreach (var kv in sortedData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                queryStringBuilder.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }
        
        var queryString = queryStringBuilder.ToString();
        
        // Remove last '&' for hash calculation
        string signData = queryString;
        if (signData.Length > 0)
        {
            signData = signData.Remove(signData.Length - 1, 1);
        }
        
        _logger.LogInformation("VNPay query string for hash (with URL encoding): {QueryString}", signData);

        // Calculate hash from URL-encoded string (as per VNPay example)
        string secureHash = CreateSecureHashHMACSHA512(signData);
        _logger.LogInformation("VNPay HMACSHA512 hash: {Hash}", secureHash);
        
        // Add secure hash to query string
        queryString += $"vnp_SecureHash={secureHash}";


        var paymentUrl = $"{_paymentUrl}?{queryString}";
        _logger.LogInformation("Created VNPAY payment URL for payment {PaymentId}: {PaymentUrl}", paymentId, paymentUrl);
        
        return paymentUrl;
    }

    public bool VerifyIpnCallback(Dictionary<string, string> vnpayData, string secureHash)
    {
        try
        {
            // Remove vnp_SecureHash and vnp_SecureHashType from data (as per VNPay example)
            var dataToVerify = vnpayData
                .Where(x => x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);

            // Build query string WITH URL encoding (as per VNPay example GetResponseData method)
            var queryStringBuilder = new StringBuilder();
            foreach (var kv in dataToVerify)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    queryStringBuilder.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            
            // Remove last '&'
            var queryStringForHash = queryStringBuilder.ToString();
            if (queryStringForHash.Length > 0)
            {
                queryStringForHash = queryStringForHash.Remove(queryStringForHash.Length - 1, 1);
            }
            
            _logger.LogInformation("VNPay IPN verification - QueryString (with URL encoding): {QueryString}", queryStringForHash);

            // Calculate hash from URL-encoded string (as per VNPay example)
            string calculatedHash = CreateSecureHashHMACSHA512(queryStringForHash);

            _logger.LogInformation("VNPay IPN verification - Calculated: {Calculated}, Received: {Received}", calculatedHash, secureHash);

            var isValid = calculatedHash.Equals(secureHash, StringComparison.InvariantCultureIgnoreCase);
            
            _logger.LogInformation("VNPAY IPN verification result: {IsValid}", isValid);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying VNPAY IPN callback");
            return false;
        }
    }

    public VnPayIpnResult ParseIpnCallback(Dictionary<string, string> queryParams)
    {
        var result = new VnPayIpnResult();

        if (queryParams.TryGetValue("vnp_TxnRef", out var txnRef))
            result.PaymentId = txnRef;

        if (queryParams.TryGetValue("vnp_TransactionNo", out var transactionNo))
            result.TransactionNo = transactionNo;

        if (queryParams.TryGetValue("vnp_ResponseCode", out var responseCode))
            result.ResponseCode = responseCode;

        if (queryParams.TryGetValue("vnp_Amount", out var amountStr) && long.TryParse(amountStr, out var amount))
            result.Amount = amount / 100m; // Convert from cents

        if (queryParams.TryGetValue("vnp_SecureHash", out var secureHash))
            result.SecureHash = secureHash;

        if (queryParams.TryGetValue("vnp_BankCode", out var bankCode))
            result.BankCode = bankCode;

        if (queryParams.TryGetValue("vnp_CardType", out var cardType))
            result.CardType = cardType;

        if (queryParams.TryGetValue("vnp_PayDate", out var payDateStr) && 
            DateTime.TryParseExact(payDateStr, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var payDate))
            result.PayDate = payDate;

        return result;
    }

    private string CreateSecureHashHMACSHA512(string data)
    {
        // VNPay uses HMACSHA512 for secure hash (as per VNPay example)
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(_hashSecret);
        byte[] inputBytes = Encoding.UTF8.GetBytes(data);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}

