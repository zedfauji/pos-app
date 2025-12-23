using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MagiDesk.Shared.DTOs.Users;

namespace JsonTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://magidesk-users-23sbzjsxaq-pv.a.run.app/");
                
                Console.WriteLine("Testing JSON deserialization with frontend options...");
                
                var response = await httpClient.GetAsync("api/users?page=1&pageSize=10");
                var jsonContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Raw JSON Response:");
                Console.WriteLine(jsonContent);
                Console.WriteLine();
                
                // Test with frontend options
                var frontendOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                Console.WriteLine("Testing deserialization with frontend options...");
                var pagedResult = JsonSerializer.Deserialize<PagedResult<UserDto>>(jsonContent, frontendOptions);
                
                if (pagedResult != null)
                {
                    Console.WriteLine($"✅ Deserialization successful!");
                    Console.WriteLine($"Total Count: {pagedResult.TotalCount}");
                    Console.WriteLine($"Page: {pagedResult.Page}");
                    Console.WriteLine($"Page Size: {pagedResult.PageSize}");
                    Console.WriteLine($"Items Count: {pagedResult.Items.Count}");
                }
                else
                {
                    Console.WriteLine("❌ Deserialization returned null");
                }
                
                // Test with default options
                Console.WriteLine("\nTesting deserialization with default options...");
                var defaultResult = JsonSerializer.Deserialize<PagedResult<UserDto>>(jsonContent);
                
                if (defaultResult != null)
                {
                    Console.WriteLine($"✅ Default deserialization successful!");
                }
                else
                {
                    Console.WriteLine("❌ Default deserialization returned null");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}