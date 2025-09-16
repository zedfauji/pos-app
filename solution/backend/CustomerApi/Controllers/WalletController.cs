using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService walletService, ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Get wallet by customer ID
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<WalletDto>> GetWalletByCustomerId(Guid customerId)
    {
        try
        {
            var wallet = await _walletService.GetWalletByCustomerIdAsync(customerId);
            if (wallet == null)
            {
                return NotFound($"Wallet for customer {customerId} not found");
            }

            return Ok(wallet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wallet for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving the wallet");
        }
    }

    /// <summary>
    /// Create wallet for customer
    /// </summary>
    [HttpPost("customer/{customerId:guid}")]
    public async Task<ActionResult<WalletDto>> CreateWallet(Guid customerId)
    {
        try
        {
            var wallet = await _walletService.CreateWalletAsync(customerId);
            return CreatedAtAction(nameof(GetWalletByCustomerId), new { customerId }, wallet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while creating the wallet");
        }
    }

    /// <summary>
    /// Add funds to customer's wallet
    /// </summary>
    [HttpPost("customer/{customerId:guid}/add-funds")]
    public async Task<ActionResult> AddFunds(Guid customerId, [FromBody] AddWalletFundsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _walletService.AddFundsAsync(customerId, request);
            if (!success)
            {
                return BadRequest("Unable to add funds to wallet. Wallet may not exist or be inactive, or the amount may exceed maximum balance limits.");
            }

            return Ok(new { Message = "Funds added successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding funds to wallet for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while adding funds to the wallet");
        }
    }

    /// <summary>
    /// Deduct funds from customer's wallet
    /// </summary>
    [HttpPost("customer/{customerId:guid}/deduct-funds")]
    public async Task<ActionResult> DeductFunds(Guid customerId, [FromBody] DeductWalletFundsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _walletService.DeductFundsAsync(
                customerId, 
                request.Amount, 
                request.Description, 
                request.ReferenceId, 
                request.OrderId);

            if (!success)
            {
                return BadRequest("Unable to deduct funds from wallet. Insufficient balance or wallet may not exist.");
            }

            return Ok(new { Message = "Funds deducted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting funds from wallet for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while deducting funds from the wallet");
        }
    }

    /// <summary>
    /// Check if customer can deduct specified amount from wallet
    /// </summary>
    [HttpGet("customer/{customerId:guid}/can-deduct/{amount:decimal}")]
    public async Task<ActionResult<bool>> CanDeduct(Guid customerId, decimal amount)
    {
        try
        {
            var canDeduct = await _walletService.CanDeductAsync(customerId, amount);
            return Ok(canDeduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wallet balance for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while checking wallet balance");
        }
    }

    /// <summary>
    /// Get wallet transactions for customer with pagination
    /// </summary>
    [HttpGet("customer/{customerId:guid}/transactions")]
    public async Task<ActionResult<WalletTransactionResponse>> GetWalletTransactions(
        Guid customerId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (transactions, totalCount) = await _walletService.GetWalletTransactionsAsync(customerId, page, pageSize);

            var response = new WalletTransactionResponse
            {
                Transactions = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wallet transactions for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving wallet transactions");
        }
    }
}
