using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
}

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment for amount {Amount}", request.Amount);

            // Simulate payment processing delay
            await Task.Delay(1000);

            // Simulate payment validation
            if (!ValidatePaymentRequest(request))
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Invalid payment information",
                    TransactionId = null
                };
            }

            // Simulate payment gateway response (90% success rate)
            var random = new Random();
            var isSuccessful = random.Next(100) < 90;

            if (isSuccessful)
            {
                var transactionId = GenerateTransactionId();
                
                _logger.LogInformation("Payment processed successfully. Transaction ID: {TransactionId}", transactionId);
                
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Amount = request.Amount,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            else
            {
                _logger.LogWarning("Payment failed for amount {Amount}", request.Amount);
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment was declined by the bank",
                    TransactionId = null
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "Payment processing failed due to a technical error",
                TransactionId = null
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            _logger.LogInformation("Processing refund for transaction {TransactionId}, amount {Amount}", transactionId, amount);

            // Simulate refund processing delay
            await Task.Delay(500);

            // Simulate refund validation
            if (string.IsNullOrEmpty(transactionId) || amount <= 0)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Invalid refund request",
                    TransactionId = null
                };
            }

            // Simulate refund gateway response (95% success rate)
            var random = new Random();
            var isSuccessful = random.Next(100) < 95;

            if (isSuccessful)
            {
                _logger.LogInformation("Refund processed successfully for transaction {TransactionId}", transactionId);
                
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Amount = amount,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            else
            {
                _logger.LogWarning("Refund failed for transaction {TransactionId}", transactionId);
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Refund was declined",
                    TransactionId = null
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "Refund processing failed due to a technical error",
                TransactionId = null
            };
        }
    }

    private bool ValidatePaymentRequest(PaymentRequest request)
    {
        // Basic validation
        if (request.Amount <= 0)
            return false;

        if (string.IsNullOrEmpty(request.CardNumber) || request.CardNumber.Length < 13)
            return false;

        if (string.IsNullOrEmpty(request.CardHolderName))
            return false;

        if (request.ExpiryMonth < 1 || request.ExpiryMonth > 12)
            return false;

        if (request.ExpiryYear < DateTime.Now.Year)
            return false;

        if (string.IsNullOrEmpty(request.Cvv) || request.Cvv.Length < 3)
            return false;

        return true;
    }

    private string GenerateTransactionId()
    {
        var random = new Random();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomPart = random.Next(1000, 9999);
        return $"TXN_{timestamp}_{randomPart}";
    }
}

// DTOs
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Cvv { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
