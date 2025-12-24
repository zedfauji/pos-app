using Dapper;
using Microsoft.Extensions.Logging;
using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Infrastructure.Repositories
{
    public class OrderIntegrationService : IOrderIntegrationService
    {
        private readonly string _connectionString;
        private readonly Microsoft.Extensions.Logging.ILogger<OrderIntegrationService> _logger;

        public OrderIntegrationService(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger<OrderIntegrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("Postgres") 
                ?? configuration["Db:Postgres:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING")
                ?? throw new InvalidOperationException("Missing Postgres Connection String");
            _logger = logger;
        }

        private NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task PostOrderAsync(string tableLabel, string sessionId, List<OrderItemDto> items)
        {
            _logger.LogInformation("PostOrderAsync called for Table {TableLabel}, Session {SessionId}, Items: {Count}", tableLabel, sessionId, items?.Count ?? 0);
            if (items == null || !items.Any()) return;
            
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            var orderId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            try 
            {
                // Legacy Logic Replication:
                // 1. Create an Order in 'orders.orders' if not exists for this session? Or create new order for every batch?
                // The legacy code often appended items to table_session.items JSON directly OR used OrderApi.
                // The Refit client calls PostOrderAsync.
                // We will create a NEW order record for this batch of items to track distinct "sends".
                

                // Simple Order Record - Using canonical orders schema
                const string insertOrderSql = @"
                    INSERT INTO orders.orders(order_id, session_id, table_id, billing_id, status, delivery_status, subtotal, discount, tax, tip, total, profit_total, created_at, updated_at, is_deleted)
                    VALUES(@OrderId, @SessionId, @TableLabel, gen_random_uuid(), 'open'::orders.order_status, 'pending', 0, 0, 0, 0, 0, 0, @CreatedAt, @CreatedAt, false)";
                
                // Note: Guid extraction from string sessionId
                if (!Guid.TryParse(sessionId, out var sessionGuid))
                {
                     // Fallback if session is somehow not a guid, but DTO says string? 
                     // Legacy DB uses UUID for session_id.
                     throw new ArgumentException("Invalid Session ID format");
                }

                await conn.ExecuteAsync(insertOrderSql, new 
                { 
                    OrderId = orderId, 
                    SessionId = sessionGuid, 
                    TableLabel = tableLabel, 
                    CreatedAt = now 
                }, tx);
                
                _logger.LogInformation("Order {OrderId} created for session {SessionId}", orderId, sessionGuid);

                // 2. Insert Items into 'orders.order_items' - Using canonical orders schema
                // Note: menu_item_id is now uuid in orders.order_items, but we may need to convert
                const string insertItemSql = @"
                    INSERT INTO orders.order_items(order_item_id, order_id, menu_item_id, menu_item_version, name, quantity, base_price, price_delta, vendor_price, line_total, profit, delivered_quantity, delivery_status, created_at, updated_at, modifiers, line_discount, is_deleted)
                    VALUES(@OrderItemId, @OrderId, @MenuItemId, 1, @ItemName, @Quantity, @Price, 0, 0, @Price * @Quantity, 0, @Quantity, 'pending'::orders.delivery_status, @CreatedAt, @CreatedAt, '[]'::jsonb, 0, false)";
                
                foreach (var item in items)
                {
                    // ItemId from DTO is string (Menu Item ID).
                    // orders.order_items.menu_item_id is uuid, so we need to parse as Guid
                    Guid menuItemId;
                    if (!Guid.TryParse(item.ItemId, out menuItemId))
                    {
                         // If not UUID, generate one (fallback)
                         menuItemId = Guid.NewGuid();
                    }

                    await conn.ExecuteAsync(insertItemSql, new
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = orderId,
                        MenuItemId = menuItemId,
                        ItemName = item.ItemName ?? "Unknown Item",
                        Quantity = item.Quantity,
                        Price = item.Price,
                        CreatedAt = now
                    }, tx);
                }
                
                // 3. Optional: Update table_sessions items JSON just for legacy read compatibility?
                // The refactored StopSession reads from orders.order_items now (canonical schema).
                // So we do NOT need to update the JSON blob. Clean separation!
                
                
                await tx.CommitAsync();
                _logger.LogInformation("PostOrderAsync transaction committed successfully for {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PostOrderAsync for Table {TableLabel}", tableLabel);
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ItemLine>> GetOrderItemsForSessionAsync(Guid sessionId)
        {
             _logger.LogInformation("GetOrderItemsForSessionAsync called for Session {SessionId}", sessionId);
             using var conn = CreateConnection();
             // 1. Fetch SQL Items (orders.order_items) - Using canonical orders schema
             // NOTE: Can now join menu.menu_items because menu_item_id types match (uuid).
             const string sql = @"
                SELECT 
                    COALESCE(oi.name, oi.snapshot_name, 'Item #' || oi.menu_item_id::text) as name,
                    oi.quantity,
                    oi.line_total as price,
                    oi.menu_item_id::text as itemId
                FROM orders.order_items oi
                JOIN orders.orders o ON oi.order_id = o.order_id
                WHERE o.session_id = @Sid AND oi.is_deleted = false AND o.is_deleted = false
                ORDER BY oi.created_at";

             var rawSqlItems = await conn.QueryAsync(sql, new { Sid = sessionId });
             _logger.LogInformation("Fetched {Count} items from SQL for Session {SessionId}", rawSqlItems.Count(), sessionId);

             // 2. Fetch Legacy JSON Items
             const string jsonSql = "SELECT items FROM public.\"TableSessions\" WHERE session_id = @Sid";
             var jsonString = await conn.ExecuteScalarAsync<string>(jsonSql, new { Sid = sessionId });
             
             var legacyItems = new List<ItemLine>();
             if (!string.IsNullOrEmpty(jsonString))
             {
                 try 
                 {
                     legacyItems = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(jsonString) ?? new List<ItemLine>();
                 }
                 catch { /* Ignore malformed JSON */ }
                 
                 _logger.LogInformation("Fetched {Count} legacy items from JSON for Session {SessionId}", legacyItems.Count, sessionId);
             }

             // 3. Merge & Group
             var combined = new List<ItemLine>();
             
             // Map SQL items to DTO
             foreach(var row in rawSqlItems)
             {
                 combined.Add(new ItemLine 
                 {
                     itemId = row.itemId,
                     name = row.name,
                     price = (decimal)row.price,
                     quantity = (int)row.quantity
                 });
             }
             
             // Add Legacy
             combined.AddRange(legacyItems);

             // Group by ItemId/Name/Price to clean up
             var result = combined.GroupBy(x => new { x.itemId, x.name, x.price })
                            .Select(g => new ItemLine 
                            {
                                itemId = g.Key.itemId,
                                name = g.Key.name,
                                price = g.Key.price,
                                quantity = g.Sum(x => x.quantity)
                            }).ToList();

             _logger.LogInformation("Returning {Count} merged unique items for Session {SessionId}", result.Count, sessionId);
             return result;
        }
    }
}
