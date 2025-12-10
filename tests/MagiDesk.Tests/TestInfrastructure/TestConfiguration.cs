using Microsoft.Extensions.Configuration;

namespace MagiDesk.Tests.TestInfrastructure;

public static class TestConfiguration
{
    public static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string GetTestDatabaseConnectionString()
    {
        return Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") 
               ?? "Host=34.51.82.201;Port=5432;Database=postgres;Username=posapp;Password=Campus_66;SSL Mode=Require;Trust Server Certificate=true";
    }

    public static string GetTestApiBaseUrl()
    {
        return Environment.GetEnvironmentVariable("TEST_API_BASE_URL") 
               ?? "https://magidesk-backend-904541739138.us-central1.run.app";
    }

    public static Dictionary<string, string> GetTestApiUrls()
    {
        return new Dictionary<string, string>
        {
            ["OrderApi"] = Environment.GetEnvironmentVariable("TEST_ORDER_API_URL") ?? "https://magidesk-order-904541739138.northamerica-south1.run.app",
            ["PaymentApi"] = Environment.GetEnvironmentVariable("TEST_PAYMENT_API_URL") ?? "https://magidesk-payment-904541739138.northamerica-south1.run.app",
            ["MenuApi"] = Environment.GetEnvironmentVariable("TEST_MENU_API_URL") ?? "https://magidesk-menu-904541739138.northamerica-south1.run.app",
            ["InventoryApi"] = Environment.GetEnvironmentVariable("TEST_INVENTORY_API_URL") ?? "https://magidesk-inventory-904541739138.northamerica-south1.run.app",
            ["SettingsApi"] = Environment.GetEnvironmentVariable("TEST_SETTINGS_API_URL") ?? "https://magidesk-settings-904541739138.northamerica-south1.run.app",
            ["TablesApi"] = Environment.GetEnvironmentVariable("TEST_TABLES_API_URL") ?? "https://magidesk-tables-904541739138.northamerica-south1.run.app",
            ["Backend"] = Environment.GetEnvironmentVariable("TEST_BACKEND_URL") ?? "https://magidesk-backend-904541739138.us-central1.run.app"
        };
    }
}
