using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunicationsController : ControllerBase
{
    private readonly ICommunicationService _communicationService;
    private readonly ILogger<CommunicationsController> _logger;

    public CommunicationsController(
        ICommunicationService communicationService,
        ILogger<CommunicationsController> logger)
    {
        _communicationService = communicationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to a customer
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _communicationService.SendMessageAsync(request);
            
            return Ok(new SendMessageResponse
            {
                Success = success,
                Message = success ? "Message sent successfully" : "Failed to send message"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid send message request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to customer {CustomerId}", request.CustomerId);
            return StatusCode(500, "Internal server error while sending message");
        }
    }

    /// <summary>
    /// Get communication logs for a customer
    /// </summary>
    [HttpGet("logs/customer/{customerId:guid}")]
    public async Task<ActionResult<List<CommunicationLogDto>>> GetCustomerCommunicationLogs(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await _communicationService.GetCommunicationLogsAsync(customerId, page, pageSize);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving communication logs for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error while retrieving communication logs");
        }
    }

    /// <summary>
    /// Get all communication providers
    /// </summary>
    [HttpGet("providers")]
    public async Task<ActionResult<List<CommunicationProviderDto>>> GetProviders(
        [FromQuery] string? providerType = null)
    {
        try
        {
            Models.CommunicationProvider? type = null;
            if (!string.IsNullOrEmpty(providerType) && Enum.TryParse<Models.CommunicationProvider>(providerType, true, out var parsedType))
            {
                type = parsedType;
            }

            var providers = await _communicationService.GetProvidersAsync(type);
            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving communication providers");
            return StatusCode(500, "Internal server error while retrieving providers");
        }
    }

    /// <summary>
    /// Create a new communication provider
    /// </summary>
    [HttpPost("providers")]
    public async Task<ActionResult<CommunicationProviderDto>> CreateProvider([FromBody] CommunicationProviderDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var provider = await _communicationService.CreateProviderAsync(request);
            return CreatedAtAction(nameof(GetProvider), new { providerId = provider.ProviderId }, provider);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid provider creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating communication provider");
            return StatusCode(500, "Internal server error while creating provider");
        }
    }

    /// <summary>
    /// Get a specific communication provider
    /// </summary>
    [HttpGet("providers/{providerId:guid}")]
    public async Task<ActionResult<CommunicationProviderDto>> GetProvider(Guid providerId)
    {
        try
        {
            var providers = await _communicationService.GetProvidersAsync();
            var provider = providers.FirstOrDefault(p => p.ProviderId == providerId);
            
            if (provider == null)
            {
                return NotFound($"Provider with ID {providerId} not found");
            }

            return Ok(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider {ProviderId}", providerId);
            return StatusCode(500, "Internal server error while retrieving provider");
        }
    }

    /// <summary>
    /// Update a communication provider
    /// </summary>
    [HttpPut("providers/{providerId:guid}")]
    public async Task<ActionResult<CommunicationProviderDto>> UpdateProvider(
        Guid providerId, 
        [FromBody] CommunicationProviderDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var provider = await _communicationService.UpdateProviderAsync(providerId, request);
            if (provider == null)
            {
                return NotFound($"Provider with ID {providerId} not found");
            }

            return Ok(provider);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid provider update request for {ProviderId}", providerId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider {ProviderId}", providerId);
            return StatusCode(500, "Internal server error while updating provider");
        }
    }

    /// <summary>
    /// Delete a communication provider
    /// </summary>
    [HttpDelete("providers/{providerId:guid}")]
    public async Task<IActionResult> DeleteProvider(Guid providerId)
    {
        try
        {
            var success = await _communicationService.DeleteProviderAsync(providerId);
            if (!success)
            {
                return NotFound($"Provider with ID {providerId} not found or cannot be deleted");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provider {ProviderId}", providerId);
            return StatusCode(500, "Internal server error while deleting provider");
        }
    }

    /// <summary>
    /// Get all communication templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<List<CommunicationTemplateDto>>> GetTemplates(
        [FromQuery] string? providerType = null)
    {
        try
        {
            Models.CommunicationProvider? type = null;
            if (!string.IsNullOrEmpty(providerType) && Enum.TryParse<Models.CommunicationProvider>(providerType, true, out var parsedType))
            {
                type = parsedType;
            }

            var templates = await _communicationService.GetTemplatesAsync(type);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving communication templates");
            return StatusCode(500, "Internal server error while retrieving templates");
        }
    }

    /// <summary>
    /// Create a new communication template
    /// </summary>
    [HttpPost("templates")]
    public async Task<ActionResult<CommunicationTemplateDto>> CreateTemplate([FromBody] CommunicationTemplateDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await _communicationService.CreateTemplateAsync(request);
            return CreatedAtAction(nameof(GetTemplate), new { templateId = template.TemplateId }, template);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating communication template");
            return StatusCode(500, "Internal server error while creating template");
        }
    }

    /// <summary>
    /// Get a specific communication template
    /// </summary>
    [HttpGet("templates/{templateId:guid}")]
    public async Task<ActionResult<CommunicationTemplateDto>> GetTemplate(Guid templateId)
    {
        try
        {
            var templates = await _communicationService.GetTemplatesAsync();
            var template = templates.FirstOrDefault(t => t.TemplateId == templateId);
            
            if (template == null)
            {
                return NotFound($"Template with ID {templateId} not found");
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", templateId);
            return StatusCode(500, "Internal server error while retrieving template");
        }
    }

    /// <summary>
    /// Update a communication template
    /// </summary>
    [HttpPut("templates/{templateId:guid}")]
    public async Task<ActionResult<CommunicationTemplateDto>> UpdateTemplate(
        Guid templateId, 
        [FromBody] CommunicationTemplateDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await _communicationService.UpdateTemplateAsync(templateId, request);
            if (template == null)
            {
                return NotFound($"Template with ID {templateId} not found");
            }

            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template update request for {TemplateId}", templateId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
            return StatusCode(500, "Internal server error while updating template");
        }
    }

    /// <summary>
    /// Delete a communication template
    /// </summary>
    [HttpDelete("templates/{templateId:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid templateId)
    {
        try
        {
            var success = await _communicationService.DeleteTemplateAsync(templateId);
            if (!success)
            {
                return NotFound($"Template with ID {templateId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            return StatusCode(500, "Internal server error while deleting template");
        }
    }

    /// <summary>
    /// Retry a failed message
    /// </summary>
    [HttpPost("logs/{logId:guid}/retry")]
    public async Task<ActionResult<RetryMessageResponse>> RetryFailedMessage(Guid logId)
    {
        try
        {
            var success = await _communicationService.RetryFailedMessageAsync(logId);
            
            return Ok(new RetryMessageResponse
            {
                Success = success,
                Message = success ? "Message retry initiated successfully" : "Failed to retry message - may not be eligible for retry"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying message {LogId}", logId);
            return StatusCode(500, "Internal server error while retrying message");
        }
    }

    /// <summary>
    /// Process all failed messages for retry
    /// </summary>
    [HttpPost("process-failed")]
    public async Task<ActionResult<ProcessFailedMessagesResponse>> ProcessFailedMessages()
    {
        try
        {
            await _communicationService.ProcessFailedMessagesAsync();
            
            return Ok(new ProcessFailedMessagesResponse
            {
                Success = true,
                Message = "Failed messages processing initiated"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed messages");
            return StatusCode(500, "Internal server error while processing failed messages");
        }
    }

    /// <summary>
    /// Get communication statistics and analytics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<Dictionary<string, object>>> GetCommunicationStats()
    {
        try
        {
            var stats = await _communicationService.GetCommunicationStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving communication statistics");
            return StatusCode(500, "Internal server error while retrieving communication stats");
        }
    }

    /// <summary>
    /// Get available communication options and configurations
    /// </summary>
    [HttpGet("options")]
    public ActionResult<Dictionary<string, object>> GetCommunicationOptions()
    {
        try
        {
            var options = new Dictionary<string, object>
            {
                ["provider_types"] = new[]
                {
                    new { value = "Email", description = "Email communications via SMTP" },
                    new { value = "SMS", description = "SMS text messages via Twilio" },
                    new { value = "WhatsApp", description = "WhatsApp messages via Business API" }
                },
                ["message_statuses"] = new[]
                {
                    new { value = "Pending", description = "Message queued for sending" },
                    new { value = "Sent", description = "Message sent successfully" },
                    new { value = "Delivered", description = "Message delivered to recipient" },
                    new { value = "Opened", description = "Message opened by recipient" },
                    new { value = "Clicked", description = "Message links clicked by recipient" },
                    new { value = "Failed", description = "Message failed to send" }
                },
                ["template_variables"] = new[]
                {
                    new { variable = "{CustomerName}", description = "Customer first name" },
                    new { variable = "{FullName}", description = "Customer full name" },
                    new { variable = "{Email}", description = "Customer email address" },
                    new { variable = "{Phone}", description = "Customer phone number" },
                    new { variable = "{MembershipLevel}", description = "Customer membership level" },
                    new { variable = "{LoyaltyPoints}", description = "Current loyalty points balance" },
                    new { variable = "{TotalSpent}", description = "Total amount spent by customer" }
                },
                ["template_categories"] = new[]
                {
                    "Welcome", "Promotional", "Transactional", "Reminder", "Survey", "Update", "Alert"
                }
            };

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving communication options");
            return StatusCode(500, "Internal server error while retrieving communication options");
        }
    }
}

public class SendMessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class RetryMessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class ProcessFailedMessagesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
