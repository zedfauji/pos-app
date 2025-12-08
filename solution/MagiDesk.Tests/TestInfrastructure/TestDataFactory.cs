using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Tables;
using OrderApi.Models;
using PaymentApi.Models;

namespace MagiDesk.Tests.TestInfrastructure;

public static class TestDataFactory
{
    public static CreateOrderRequestDto CreateTestCreateOrderRequest()
    {
        return new CreateOrderRequestDto(
            SessionId: Guid.NewGuid(),
            BillingId: Guid.NewGuid(),
            TableId: "Table-1",
            ServerId: "test-server",
            ServerName: "Test Server",
            Items: new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto(
                    MenuItemId: 1,
                    ComboId: null,
                    Quantity: 2,
                    Modifiers: new List<ModifierSelectionDto>()
                )
            }
        );
    }

    public static RegisterPaymentRequestDto CreateTestPaymentRequest()
    {
        return new RegisterPaymentRequestDto(
            SessionId: Guid.NewGuid(),
            BillingId: Guid.NewGuid(),
            TotalDue: 25.50m,
            Lines: new List<RegisterPaymentLineDto>
            {
                new RegisterPaymentLineDto(
                    AmountPaid: 25.50m,
                    PaymentMethod: "Cash",
                    DiscountAmount: 0m,
                    DiscountReason: null,
                    TipAmount: 2.55m,
                    ExternalRef: null,
                    Meta: null
                )
            },
            ServerId: "test-server"
        );
    }

    public static LoginRequest CreateTestLoginRequest()
    {
        return new LoginRequest
        {
            Username = "admin",
            Password = "1234"
        };
    }

    public static StartSessionRequest CreateTestStartSessionRequest()
    {
        return new StartSessionRequest("test-server", "Test Server");
    }

    public static IEnumerable<CreateOrderRequestDto> CreateMultipleTestOrders(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return CreateTestCreateOrderRequest();
        }
    }

    public static IEnumerable<RegisterPaymentRequestDto> CreateMultipleTestPayments(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return CreateTestPaymentRequest();
        }
    }

    public static string CreateInvalidJson()
    {
        return "{ invalid json }";
    }

    public static string CreateEmptyJson()
    {
        return "{}";
    }

    public static string CreateNullJson()
    {
        return "null";
    }

    // Additional test data for crash prevention tests
    public static object CreateLargePayload()
    {
        return new
        {
            data = new string('A', 1024 * 1024), // 1MB of data
            items = Enumerable.Range(1, 10000).Select(i => new { id = i, name = $"Item {i}" }).ToArray()
        };
    }

    public static object CreateInvalidUnicodePayload()
    {
        return new
        {
            name = "\uD800\uDFFF", // Invalid surrogate pair
            description = "Test with invalid Unicode: \uFFFE\uFFFF"
        };
    }

    public static object CreateDeeplyNestedPayload()
    {
        var deepObject = new Dictionary<string, object>();
        var current = deepObject;
        
        for (int i = 0; i < 100; i++)
        {
            current["level"] = i;
            current["next"] = new Dictionary<string, object>();
            current = (Dictionary<string, object>)current["next"];
        }

        return deepObject;
    }

    public static string[] CreateSQLInjectionAttempts()
    {
        return new[]
        {
            "'; DROP TABLE orders; --",
            "' OR '1'='1",
            "'; INSERT INTO users VALUES ('hacker', 'password'); --",
            "' UNION SELECT * FROM users --",
            "'; DELETE FROM payments; --"
        };
    }

    public static string[] CreateXSSAttempts()
    {
        return new[]
        {
            "<script>alert('xss')</script>",
            "<img src=x onerror=alert('xss')>",
            "javascript:alert('xss')",
            "<iframe src='javascript:alert(\"xss\")'></iframe>",
            "<svg onload=alert('xss')>"
        };
    }

    public static string[] CreateMalformedJsonArray()
    {
        return new[]
        {
            "{ invalid json }",
            "{ \"missing\": \"quote }",
            "{ \"trailing\": \"comma\", }",
            "{ \"unclosed\": \"string",
            "{ \"null\": null, \"undefined\": }",
            "not json at all",
            ""
        };
    }

    public static object CreateLargeNumberPayload()
    {
        return new
        {
            quantity = 999999999m,
            price = 999999999m,
            total = 1999999998m
        };
    }

    public static string[] CreateInvalidContentTypes()
    {
        return new[]
        {
            "text/plain",
            "application/xml",
            "application/octet-stream",
            "multipart/form-data",
            "image/jpeg"
        };
    }
}