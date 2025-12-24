using Dapper;
using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Infrastructure.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly string _connectionString;

        public TableRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres") 
                ?? configuration["Db:Postgres:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING")
                ?? throw new InvalidOperationException("Missing Postgres Connection String");
        }

        private NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<TableStatusDto>> GetAllAsync()
        {
            const string sql = @"
                SELECT 
                    t.table_name as Label, 
                    COALESCE(tt.name, 'Bar') as TypeName,
                    COALESCE(tt.id, 0) as TypeId,
                    CASE WHEN t.table_type = 1 THEN 'billiard' ELSE 'table' END as Type,
                    CASE WHEN ses.session_id IS NOT NULL THEN true ELSE false END as Occupied, 
                    ses.server_name as Server, 
                    ses.start_time as StartTime, 
                    ses.session_id as CurrentSessionId,
                    -- Split for Config
                    COALESCE(tt.has_timer, false) as HasTimer,
                    COALESCE(tt.hourly_rate, 0) as HourlyRate,
                    COALESCE(tt.allow_orders, true) as AllowOrders,
                    COALESCE(tt.requires_server, true) as RequiresServer
                FROM public.tables t
                LEFT JOIN public.table_types tt ON t.type_id = tt.id
                LEFT JOIN public.""TableSessions"" ses ON t.table_name = ses.table_label AND ses.status = 'active'
                ORDER BY t.table_name";
            
            using var conn = CreateConnection();
            return await conn.QueryAsync<TableStatusDto, TableConfigDto, TableStatusDto>(
                sql, 
                (status, config) => {
                    status.Config = config;
                    return status;
                },
                splitOn: "HasTimer");
        }

        public async Task<IEnumerable<SessionOverview>> GetActiveSessionsAsync()
        {
             const string sql = @"SELECT 
                                      s.session_id as SessionId, 
                                      s.billing_id as BillingId, 
                                      s.table_label as TableId, 
                                      s.server_name as ServerName, 
                                      s.start_time as StartTime, 
                                      s.status as Status,
                                      COALESCE(item_stats.ItemsCount, 0) as ItemsCount,
                                      COALESCE(item_stats.Total, 0) as Total
                                  FROM public.""TableSessions"" s
                                  LEFT JOIN (
                                      SELECT o.session_id, 
                                             COUNT(*) as ItemsCount, 
                                             SUM((oi.base_price + oi.price_delta) * oi.quantity) as Total
                                      FROM orders.orders o
                                      JOIN orders.order_items oi ON oi.order_id = o.order_id
                                      WHERE o.is_deleted = false AND oi.is_deleted = false
                                      GROUP BY o.session_id
                                  ) item_stats ON item_stats.session_id = s.session_id
                                  WHERE (s.status ILIKE 'active' OR (s.status IS NULL AND s.end_time IS NULL))
                                  ORDER BY s.start_time";
             using var conn = CreateConnection();
             return await conn.QueryAsync<SessionOverview>(sql);
        }

        public async Task<SessionOverview?> GetActiveSessionByTableAsync(string tableLabel)
        {
            // Adjusted SQL to map to SessionOverview properties
            const string sql = @"SELECT s.session_id as SessionId, s.billing_id as BillingId, s.table_label as TableId, s.server_name as ServerName, s.start_time as StartTime, s.status as Status, s.items as ItemsJson
                                 FROM public.""TableSessions"" s
                                 WHERE s.table_label = @Label AND s.status = 'active'
                                 ORDER BY s.start_time DESC LIMIT 1";
            using var conn = CreateConnection();
            // Note: SessionOverview doesn't have ItemsJson? It has ItemsCount. 
            // We might need to deserialize or just ignore items for 'Overview'.
            // The original logic read json. Let's see if we can adapt.
            // Dapper will ignore extra columns.
            
            return await conn.QueryFirstOrDefaultAsync<SessionOverview>(sql, new { Label = tableLabel });
        }
        


        public async Task<Guid> StartSessionAsync(string tableLabel, string serverId, string serverName)
        {
            var sessionId = Guid.NewGuid();
            var billingId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Check if active
                var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM public.\"TableSessions\" WHERE table_label = @Label AND status = 'active'", new { Label = tableLabel }, tx);
                if (exists > 0) throw new InvalidOperationException("Active session already exists.");

                const string insertSql = @"INSERT INTO public.""TableSessions""(session_id, table_label, server_id, server_name, start_time, status, billing_id)
                                           VALUES(@Sid, @Label, @SrvId, @SrvName, @Start, 'active', @Bid)";
                await conn.ExecuteAsync(insertSql, new { Sid = sessionId, Label = tableLabel, SrvId = serverId, SrvName = serverName, Start = now, Bid = billingId }, tx);

                await tx.CommitAsync();
                return sessionId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task StopSessionAsync(Guid sessionId, DateTime endTime)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try 
            {
                // 1. Get Table Label for this session
                var label = await conn.ExecuteScalarAsync<string>("SELECT table_label FROM public.\"TableSessions\" WHERE session_id = @Sid", new { Sid = sessionId }, tx);
                
                // 2. Close Session
                await conn.ExecuteAsync("UPDATE public.\"TableSessions\" SET end_time = @End, status = 'closed' WHERE session_id = @Sid", new { End = endTime, Sid = sessionId }, tx);
                
                // 3. Free Table (No-op, derived from session status)
                
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        
        public async Task<bool> IsTableOccupiedAsync(string tableLabel)
        {
            using var conn = CreateConnection();
            const string sql = "SELECT COUNT(1) FROM public.\"TableSessions\" WHERE table_label = @Label AND status = 'active'";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Label = tableLabel });
            return count > 0;
        }

        public async Task<MoveSessionResult> MoveSessionAsync(Guid sessionId, string fromLabel, string toLabel, bool force)
        {
             using var conn = CreateConnection();
             await conn.OpenAsync();
             using var tx = await conn.BeginTransactionAsync();
             
             try 
             {
                 // 1. Get Session & Lock
                 var session = await conn.QuerySingleOrDefaultAsync<dynamic>(
                     "SELECT * FROM public.\"TableSessions\" WHERE session_id = @Sid FOR UPDATE", 
                     new { Sid = sessionId }, tx);

                 if (session == null) throw new ArgumentException("Session not found");
                 if (session.status != "active") throw new InvalidOperationException("Session is not active");

                 // 2. Get Configs (Source & Target)
                 const string configSql = @"
                    SELECT t.table_name, tt.has_timer, tt.hourly_rate, tt.name as type_name 
                    FROM public.tables t 
                    JOIN public.table_types tt ON t.type_id = tt.id 
                    WHERE t.table_name = @Label";

                 var fromConfig = await conn.QuerySingleAsync<dynamic>(configSql, new { Label = fromLabel }, tx);
                 var toConfig = await conn.QuerySingleAsync<dynamic>(configSql, new { Label = toLabel }, tx);

                 // 3. Check Target Occupancy
                 var occupied = await conn.ExecuteScalarAsync<int>(
                     "SELECT COUNT(1) FROM public.\"TableSessions\" WHERE table_label = @Label AND status = 'active'", 
                     new { Label = toLabel }, tx);
                 
                 if (occupied > 0) throw new InvalidOperationException($"Target table {toLabel} is occupied.");

                 // 4. Calculate Logic
                 bool fromTimer = fromConfig.has_timer;
                 bool toTimer = toConfig.has_timer;
                 decimal fromRate = fromConfig.hourly_rate;
                 decimal toRate = toConfig.hourly_rate;
                 
                 DateTime startTime = session.start_time;
                 DateTime now = DateTime.UtcNow;
                 decimal costToAdd = 0;
                 string action = "Moved";
                 bool confirmNeeded = false;
                 string confirmMsg = "";

                 // Scenario A: Timer Stopped or Rate Changed -> Finalize current segment
                 if (fromTimer)
                 {
                     if (!toTimer)
                     {
                         confirmNeeded = true;
                         confirmMsg = $"Moving to {toConfig.type_name} will STOP the timer. Current time cost will be added to bill.";
                         action = "TimerStopped";
                     }
                     else if (fromRate != toRate)
                     {
                         confirmNeeded = true;
                         confirmMsg = $"Rate change detected (${fromRate}/hr -> ${toRate}/hr). Current segment will be billed.";
                         action = "RateChanged";
                     }
                 }

                 // Check Start
                 if (!fromTimer && toTimer)
                 {
                     action = "TimerStarted";
                 }

                 // 5. Handle Confirmation
                 if (confirmNeeded && !force)
                 {
                     return new MoveSessionResult 
                     { 
                         Success = false, 
                         ConfirmationNeeded = true, 
                         Message = confirmMsg,
                         FromTable = fromLabel,
                         ToTable = toLabel
                     };
                 }

                 // 6. Execute Logic
                 if (fromTimer && (action == "TimerStopped" || action == "RateChanged"))
                 {
                     // Calculate Cost
                     var duration = now - startTime;
                     if (duration.TotalMinutes < 0) duration = TimeSpan.Zero;
                     
                     decimal hours = (decimal)duration.TotalHours;
                     costToAdd = Math.Round(hours * fromRate, 2);

                     if (costToAdd > 0)
                     {
                         // Insert Order Item
                         // Find active order or create new one
                        var orderId = await conn.ExecuteScalarAsync<Guid?>("SELECT order_id FROM orders.orders WHERE session_id = @Sid AND is_deleted = false LIMIT 1", new { Sid = sessionId }, tx);
                        
                        if (orderId == null)
                        {
                            orderId = Guid.NewGuid();
                            await conn.ExecuteAsync("INSERT INTO orders.orders(order_id, session_id, table_id, server_id, created_at, status, delivery_status, subtotal, discount, tax, tip, total, profit_total, is_deleted) VALUES(@Oid, @Sid, @Tid, @SrvId, @Now, 'open', 'pending', 0, 0, 0, 0, 0, 0, false)",
                                new { Oid = orderId, Sid = sessionId, Tid = sessionId, SrvId = "SYSTEM", Now = now }, tx);
                        }

                        var menuItemId = Guid.Empty; // System Item
                        string itemName = $"Table Time ({fromLabel}): {duration.Hours}h {duration.Minutes}m";

                        await conn.ExecuteAsync(@"
                            INSERT INTO orders.order_items(order_item_id, order_id, menu_item_id, menu_item_version, name, quantity, base_price, price_delta, vendor_price, line_total, profit, delivered_quantity, delivery_status, created_at, updated_at, modifiers, snapshot_name, line_discount, is_deleted)
                            VALUES(@ItemId, @Oid, @MenuId, 1, @Name, 1, @Price, 0, 0, @Price, 0, 0, 'pending', @Now, @Now, '[]'::jsonb, @Name, 0, false)",
                            new { ItemId = Guid.NewGuid(), Oid = orderId, MenuId = menuItemId, Price = costToAdd, Now = now, Name = itemName }, tx);
                     }
                     
                     // Reset Start Time if Timer Stopped or Rate Changed (New segment starts now)
                     // If Timer Stopped, start_time is irrelevant? Or set to NULL? 
                     // DB Schema `start_time` is NOT NULL? Let's check. 
                     // Assuming NOT NULL. We set it to NOW so tracking continues but from 0 if restarted.
                     await conn.ExecuteAsync("UPDATE public.\"TableSessions\" SET start_time = @Now WHERE session_id = @Sid", new { Now = now, Sid = sessionId }, tx);
                 }
                 else if (!fromTimer && toTimer)
                 {
                     // Start Timer
                     await conn.ExecuteAsync("UPDATE public.\"TableSessions\" SET start_time = @Now WHERE session_id = @Sid", new { Now = now, Sid = sessionId }, tx);
                 }

                 // 7. Move Table
                 await conn.ExecuteAsync("UPDATE public.\"TableSessions\" SET table_label = @To WHERE session_id = @Sid", new { To = toLabel, Sid = sessionId }, tx);
                 
                 // Audit
                 await conn.ExecuteAsync("INSERT INTO public.table_session_moves(session_id, from_label, to_label, moved_at) VALUES(@Sid, @From, @To, now())", new { Sid = sessionId, From = fromLabel, To = toLabel }, tx);
                                           
                 await tx.CommitAsync();

                 return new MoveSessionResult 
                 { 
                     Success = true, 
                     Message = $"Moved to {toLabel}. {action}." + (costToAdd > 0 ? $" Added ${costToAdd}." : ""), 
                     CostFinalized = costToAdd,
                     ActionTaken = action,
                     FromTable = fromLabel,
                     ToTable = toLabel 
                 };
             }
             catch
             {
                 await tx.RollbackAsync();
                 throw;
             }
        }

        public async Task<SessionOverview?> GetSessionByIdAsync(Guid sessionId)
        {
             const string sql = @"SELECT s.session_id as SessionId, s.billing_id as BillingId, s.table_label as TableId, s.server_name as ServerName, s.start_time as StartTime, s.status as Status
                                  FROM public.""TableSessions"" s
                                  WHERE s.session_id = @Sid";
             using var conn = CreateConnection();
             return await conn.QueryFirstOrDefaultAsync<SessionOverview>(sql, new { Sid = sessionId });
        }

        public async Task<BillPreviewDto> GetBillPreviewAsync(string tableLabel)
        {
            using var conn = CreateConnection();
            
            // 1. Get Session
            const string sessionSql = @"SELECT session_id, start_time, items FROM public.""TableSessions"" WHERE table_label = @Label AND status = 'active'";
            var session = await conn.QueryFirstOrDefaultAsync<dynamic>(sessionSql, new { Label = tableLabel });

            if (session == null) return new BillPreviewDto();

            Guid sessionId = session.session_id;

            // 2. Fetch Items from SQL (ord schema)
            // NOTE: Cannot join menu.menu_items because menu_item_id types differ (bigint vs uuid).
            const string itemsSql = @"
                SELECT 
                    COALESCE(oi.snapshot_name, 'Item #' || oi.menu_item_id::text) as name,
                    oi.quantity,
                    (oi.base_price + oi.price_delta) as price,
                    oi.menu_item_id::text as itemId
                FROM orders.order_items oi
                JOIN orders.orders o ON oi.order_id = o.order_id
                WHERE o.session_id = @Sid AND oi.is_deleted = false AND o.is_deleted = false";

            var rawItems = await conn.QueryAsync(itemsSql, new { Sid = sessionId });

            // 2b. Fetch Legacy JSON Items
            string json = session.items?.ToString() ?? "[]";
            var legacyItems = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new List<ItemLine>();

            // 3. Merge & Process
            var allItems = new List<ItemLine>();
            
            // From SQL
            allItems.AddRange(rawItems.Select(x => new ItemLine 
            {
                itemId = x.itemId,
                name = x.name,
                price = (decimal)x.price,
                quantity = (int)x.quantity
            }));
            
            // From JSON
            allItems.AddRange(legacyItems);

             var items = allItems.GroupBy(x => x.itemId)
                                 .Select(g => new ItemLine 
                                 {
                                     itemId = g.Key,
                                     name = g.First().name,
                                     price = g.First().price,
                                     quantity = g.Sum(x => x.quantity)
                                 }).ToList();

            // 4. Calculate
            decimal subtotal = items.Sum(x => x.price * x.quantity);
            
            // Time Cost (Placeholder)
            decimal timeCost = 0; 

            // Tax (10%)
            decimal taxRate = 0.10m; 
            decimal taxCtx = (subtotal + timeCost) * taxRate;

            return new BillPreviewDto
            {
                Items = items,
                Subtotal = subtotal + timeCost,
                TaxAmount = taxCtx,
                DiscountAmount = 0,
                TotalAmount = (subtotal + timeCost) + taxCtx,
                Currency = "USD"
            };
        }

        public async Task EndSessionAsync(Guid sessionId)
        {
            var now = DateTime.UtcNow;
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Get Session Details & Total, and get table_id from tables table
                const string sessionSql = @"
                    SELECT 
                        s.table_label, 
                        s.billing_id, 
                        s.status,
                        s.server_name,
                        COALESCE(t.table_id, '00000000-0000-0000-0000-000000000001'::uuid) AS table_id
                    FROM public.""TableSessions"" s
                    LEFT JOIN public.tables t ON t.table_number = s.table_label
                    WHERE s.session_id = @Sid";
                
                var session = await conn.QueryFirstOrDefaultAsync<dynamic>(sessionSql, new { Sid = sessionId }, tx);
                
                if (session == null) throw new ArgumentException("Session not found");
                if (session.status != "active") throw new InvalidOperationException("Session is not active");

                Guid billingId = session.billing_id;
                string label = session.table_label;
                Guid tableId = session.table_id;

                // 2. Calculate Total (Simplified vs GetBillPreview)
                const string totalSql = @"
                    SELECT SUM((oi.base_price + oi.price_delta) * oi.quantity)
                    FROM orders.orders o
                    JOIN orders.order_items oi ON oi.order_id = o.order_id
                    WHERE o.session_id = @Sid AND o.is_deleted = false AND oi.is_deleted = false";
                
                var subtotal = await conn.ExecuteScalarAsync<decimal?>(totalSql, new { Sid = sessionId }, tx) ?? 0;
                
                // Constants for MVP
                decimal taxRate = 0.10m;
                decimal taxAmount = subtotal * taxRate;
                decimal totalAmount = subtotal + taxAmount;

                // Use billing.bills table which has proper UUID session_id
                // Include table_label and server_name for Payment Hub display
                // Use actual table_id from tables table, not session_id
                const string insertBill = @"
                    INSERT INTO billing.bills (
                        bill_id, billing_id, session_id, table_id,
                        table_label, server_name,
                        items_total, time_total, subtotal, discounts, tax, total_amount,
                        time_minutes, status, created_at, updated_at
                    ) VALUES (
                        @Bid, @Bid, @Sid, @TableId,
                        @TableLabel, @ServerName,
                        @Subtotal, 0, @Subtotal, 0, @Tax, @Total,
                        0, 'AwaitingPayment', @Now, @Now
                    ) ON CONFLICT (bill_id) DO NOTHING"; // Idempotency safety

                await conn.ExecuteAsync(insertBill, new { 
                    Bid = billingId, 
                    Sid = sessionId,
                    TableId = tableId,
                    TableLabel = label,
                    ServerName = session.server_name ?? "Unknown",
                    Subtotal = subtotal,
                    Tax = taxAmount,
                    Total = totalAmount, 
                    Now = now 
                }, tx);

                // 4. End Session (Status -> ended)
                const string updateSession = @"
                    UPDATE public.""TableSessions"" 
                    SET status = 'ended', end_time = @Now 
                    WHERE session_id = @Sid";
                await conn.ExecuteAsync(updateSession, new { Sid = sessionId, Now = now }, tx);

                // 5. Free Table (No-op)

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<IEnumerable<TableTypeDto>> GetTableTypesAsync()
        {
            const string sql = @"
                SELECT 
                    id, 
                    name, 
                    has_timer as HasTimer, 
                    hourly_rate as HourlyRate, 
                    allow_orders as AllowOrders, 
                    requires_server as RequiresServer
                FROM public.table_types
                ORDER BY id";
            
            using var conn = CreateConnection();
            return await conn.QueryAsync<TableTypeDto>(sql);
        }
        public async Task<TableStatusDto> AddTableAsync(CreateTableRequest request)
        {
            const string sql = @"
                INSERT INTO public.tables (table_id, table_name, type_id, capacity, is_active)
                VALUES (@Id, @Name, @TypeId, @Capacity, true)
                RETURNING table_id";
            
            var id = Guid.NewGuid();
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id, request.Name, request.TypeId, request.Capacity });
            
            // Return full DTO
            var all = await GetAllAsync(); 
            return all.FirstOrDefault(t => t.Label == request.Name) ?? new TableStatusDto();
        }

        public async Task<TableStatusDto> UpdateTableAsync(Guid tableId, UpdateTableRequest request)
        {
             using var conn = CreateConnection();
             
             // Check Occupancy if deactivating
             if (!request.IsActive)
             {
                 var occupied = await conn.ExecuteScalarAsync<int>(
                     @"SELECT COUNT(1) FROM public.""TableSessions"" s 
                       JOIN public.tables t ON s.table_label = t.table_name 
                       WHERE t.table_id = @Id AND s.status = 'active'", 
                     new { Id = tableId });
                 if (occupied > 0) throw new InvalidOperationException("Cannot deactivate occupied table.");
             }

             const string sql = @"
                UPDATE public.tables
                SET table_name = @Name, type_id = @TypeId, capacity = @Capacity, is_active = @IsActive, updated_at = now()
                WHERE table_id = @Id";
             
             await conn.ExecuteAsync(sql, new { Id = tableId, request.Name, request.TypeId, request.Capacity, request.IsActive });
             
             // Note: Returning via GetAllAsync is inefficient but ensures consistent DTO mapping
             var all = await GetAllAsync();
             return all.FirstOrDefault(t => t.Label == request.Name) ?? new TableStatusDto();
        }

        public async Task DeleteTableAsync(Guid tableId)
        {
             using var conn = CreateConnection();
             // Check Occupancy
             var occupied = await conn.ExecuteScalarAsync<int>(
                 @"SELECT COUNT(1) FROM public.""TableSessions"" s 
                   JOIN public.tables t ON s.table_label = t.table_name 
                   WHERE t.table_id = @Id AND s.status = 'active'", 
                 new { Id = tableId });
             if (occupied > 0) throw new InvalidOperationException("Cannot delete occupied table.");

             // Soft Delete
             await conn.ExecuteAsync("UPDATE public.tables SET is_active = false WHERE table_id = @Id", new { Id = tableId });
        }

        public async Task<TableTypeDto> UpdateTableTypeAsync(int typeId, UpdateTableTypeRequest request)
        {
            const string sql = @"
                UPDATE public.table_types
                SET name = @Name, hourly_rate = @Rate, has_timer = @Timer, requires_server = @ReqServer, allow_orders = @Orders
                WHERE id = @Id
                RETURNING 
                    id, 
                    name, 
                    has_timer as HasTimer, 
                    hourly_rate as HourlyRate, 
                    allow_orders as AllowOrders, 
                    requires_server as RequiresServer";
            
            using var conn = CreateConnection();
            return await conn.QuerySingleAsync<TableTypeDto>(sql, new { 
                Id = typeId, 
                request.Name, 
                Rate = request.HourlyRate, 
                Timer = request.HasTimer, 
                ReqServer = request.RequiresServer, 
                Orders = request.AllowOrders 
            });
        }
    }
}
