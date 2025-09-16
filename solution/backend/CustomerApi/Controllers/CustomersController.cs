using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid customerId)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                return NotFound($"Customer with ID {customerId} not found");
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving the customer");
        }
    }

    /// <summary>
    /// Get customer by phone number
    /// </summary>
    [HttpGet("by-phone/{phone}")]
    public async Task<ActionResult<CustomerDto>> GetCustomerByPhone(string phone)
    {
        try
        {
            var customer = await _customerService.GetCustomerByPhoneAsync(phone);
            if (customer == null)
            {
                return NotFound($"Customer with phone {phone} not found");
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer by phone {Phone}", phone);
            return StatusCode(500, "An error occurred while retrieving the customer");
        }
    }

    /// <summary>
    /// Get customer by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<CustomerDto>> GetCustomerByEmail(string email)
    {
        try
        {
            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer == null)
            {
                return NotFound($"Customer with email {email} not found");
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer by email {Email}", email);
            return StatusCode(500, "An error occurred while retrieving the customer");
        }
    }

    /// <summary>
    /// Search customers with filters and pagination
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<CustomerSearchResponse>> SearchCustomers([FromBody] CustomerSearchRequest request)
    {
        try
        {
            var (customers, totalCount) = await _customerService.SearchCustomersAsync(request);

            var response = new CustomerSearchResponse
            {
                Customers = customers,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return StatusCode(500, "An error occurred while searching customers");
        }
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerService.CreateCustomerAsync(request);
            return CreatedAtAction(nameof(GetCustomer), new { customerId = customer.CustomerId }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, "An error occurred while creating the customer");
        }
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    [HttpPut("{customerId:guid}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(Guid customerId, [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerService.UpdateCustomerAsync(customerId, request);
            if (customer == null)
            {
                return NotFound($"Customer with ID {customerId} not found");
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while updating the customer");
        }
    }

    /// <summary>
    /// Delete a customer (soft delete)
    /// </summary>
    [HttpDelete("{customerId:guid}")]
    public async Task<ActionResult> DeleteCustomer(Guid customerId)
    {
        try
        {
            var success = await _customerService.DeleteCustomerAsync(customerId);
            if (!success)
            {
                return NotFound($"Customer with ID {customerId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while deleting the customer");
        }
    }

    /// <summary>
    /// Get customer statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<CustomerStatsDto>> GetCustomerStats()
    {
        try
        {
            var stats = await _customerService.GetCustomerStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statistics");
            return StatusCode(500, "An error occurred while retrieving customer statistics");
        }
    }

    /// <summary>
    /// Process an order for a customer (updates spending, loyalty points, etc.)
    /// </summary>
    [HttpPost("{customerId:guid}/process-order")]
    public async Task<ActionResult> ProcessOrder(Guid customerId, [FromBody] ProcessOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _customerService.ProcessOrderAsync(customerId, request);
            if (!success)
            {
                return NotFound($"Customer with ID {customerId} not found");
            }

            return Ok(new { Message = "Order processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while processing the order");
        }
    }

    /// <summary>
    /// Update customer's membership level
    /// </summary>
    [HttpPut("{customerId:guid}/membership")]
    public async Task<ActionResult> UpdateMembershipLevel(Guid customerId, [FromBody] UpdateMembershipRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _customerService.UpdateMembershipLevelAsync(customerId, request.MembershipLevelId);
            if (!success)
            {
                return NotFound($"Customer with ID {customerId} not found or invalid membership level");
            }

            return Ok(new { Message = "Membership level updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership level for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while updating the membership level");
        }
    }

    /// <summary>
    /// Get customers with expiring memberships
    /// </summary>
    [HttpGet("expiring-memberships")]
    public async Task<ActionResult<List<CustomerDto>>> GetExpiringMemberships([FromQuery] int daysAhead = 30)
    {
        try
        {
            var customers = await _customerService.GetExpiringMembershipsAsync(daysAhead);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers with expiring memberships");
            return StatusCode(500, "An error occurred while retrieving expiring memberships");
        }
    }

    /// <summary>
    /// Process expired memberships (background job endpoint)
    /// </summary>
    [HttpPost("process-expired-memberships")]
    public async Task<ActionResult> ProcessExpiredMemberships()
    {
        try
        {
            var processedCount = await _customerService.ProcessMembershipExpiriesAsync();
            return Ok(new { ProcessedCount = processedCount, Message = $"Processed {processedCount} expired memberships" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired memberships");
            return StatusCode(500, "An error occurred while processing expired memberships");
        }
    }
}
