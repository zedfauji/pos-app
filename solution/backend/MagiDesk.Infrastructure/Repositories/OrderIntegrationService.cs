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
                // 1. Create an Order in 'ord.orders' if not exists for this session? Or create new order for every batch?
                // The legacy code often appended items to table_session.items JSON directly OR used OrderApi.
                // The Refit client calls PostOrderAsync.
                // We will create a NEW order record for this batch of items to track distinct "sends".
                

                // Simple Order Record
                const string insertOrderSql = @"
                    INSERT INTO ord.orders(order_id, session_id, table_label, created_at, status, is_deleted)
                    VALUES(@OrderId, @SessionId, @TableLabel, @CreatedAt, 'submitted', false)";
                
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

                // 2. Insert Items into 'ord.order_items'
                const string insertItemSql = @"
                    INSERT INTO ord.order_items(order_item_id, order_id, menu_item_id, quantity, base_price, price_delta, is_deleted, created_at, delivered_quantity, status)
                    VALUES(@OrderItemId, @OrderId, @MenuItemId, @Quantity, @Price, 0, false, @CreatedAt, @Quantity, 'pending')";
                
                foreach (var item in items)
                {
                    // ItemId from DTO is string (Menu Item ID).
                    // MenuApi uses bigserial (long).
                    if (!long.TryParse(item.ItemId, out var menuItemId))
                    {
                         // If not long, handle gracefully or default?
                         menuItemId = 0; 
                    }

                    await conn.ExecuteAsync(insertItemSql, new 
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = orderId,
                        MenuItemId = menuItemId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        CreatedAt = now
                    }, tx);
                }
                
                // 3. Optional: Update table_sessions items JSON just for legacy read compatibility?
                // The refactored StopSession reads from ord.order_items now (as per my fix in legacy Program.cs earlier).
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
             // 1. Fetch SQL Items (ord.order_items)
             // NOTE: Cannot join menu.menu_items because menu_item_id types differ (bigint vs uuid).
             // Using snapshot_name as the fallback for item names.
             const string sql = @"
                SELECT 
                    COALESCE(oi.snapshot_name, 'Item #' || oi.menu_item_id::text) as name,
                    oi.quantity,
                    (oi.base_price + oi.price_delta) as price,
                    oi.menu_item_id::text as itemId
                FROM ord.order_items oi
                JOIN ord.orders o ON oi.order_id = o.order_id
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
