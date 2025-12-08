using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;

namespace CustomerApi.Controllers.V2;

/// <summary>
/// Version 2 Customers Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
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
    /// Get customer by ID (requires customer:view permission)
    /// </summary>
    [HttpGet("{customerId:guid}")]
    [RequiresPermission(Permissions.CUSTOMER_VIEW)]
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
    /// Get customer by phone number (requires customer:view permission)
    /// </summary>
    [HttpGet("by-phone/{phone}")]
    [RequiresPermission(Permissions.CUSTOMER_VIEW)]
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
    /// Get customer by email (requires customer:view permission)
    /// </summary>
    [HttpGet("by-email/{email}")]
    [RequiresPermission(Permissions.CUSTOMER_VIEW)]
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

    // Note: Additional endpoints from v1 controller should be added here with appropriate permission annotations
    // For brevity, showing the pattern with the main GET endpoints
}

