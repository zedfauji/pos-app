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
            // Join with active sessions to get SessionId
            const string sql = @"
                SELECT ts.label, ts.type, ts.occupied, ts.server as Server, ts.start_time as StartTime, ses.session_id as CurrentSessionId
                FROM public.table_status ts
                LEFT JOIN public.table_sessions ses ON ts.label = ses.table_label AND ses.status = 'active'
                ORDER BY ts.label";
            
            using var conn = CreateConnection();
            return await conn.QueryAsync<TableStatusDto>(sql);
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
                                  FROM public.table_sessions s
                                  LEFT JOIN (
                                      SELECT o.session_id, 
                                             COUNT(*) as ItemsCount, 
                                             SUM((oi.base_price + oi.price_delta) * oi.quantity) as Total
                                      FROM ord.orders o
                                      JOIN ord.order_items oi ON oi.order_id = o.order_id
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
                                 FROM public.table_sessions s
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
                var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM public.table_sessions WHERE table_label = @Label AND status = 'active'", new { Label = tableLabel }, tx);
                if (exists > 0) throw new InvalidOperationException("Active session already exists.");

                const string insertSql = @"INSERT INTO public.table_sessions(session_id, table_label, server_id, server_name, start_time, status, billing_id)
                                           VALUES(@Sid, @Label, @SrvId, @SrvName, @Start, 'active', @Bid)";
                await conn.ExecuteAsync(insertSql, new { Sid = sessionId, Label = tableLabel, SrvId = serverId, SrvName = serverName, Start = now, Bid = billingId }, tx);

                const string updateStatus = @"INSERT INTO public.table_status(label, type, occupied, start_time, server)
                                              VALUES(@Label, 'billiard', true, @Start, @SrvName)
                                              ON CONFLICT (label) DO UPDATE SET occupied = true, start_time = @Start, server = @SrvName, updated_at = now()";
                 await conn.ExecuteAsync(updateStatus, new { Label = tableLabel, Start = now, SrvName = serverName }, tx);

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
                var label = await conn.ExecuteScalarAsync<string>("SELECT table_label FROM public.table_sessions WHERE session_id = @Sid", new { Sid = sessionId }, tx);
                
                // 2. Close Session
                await conn.ExecuteAsync("UPDATE public.table_sessions SET end_time = @End, status = 'closed' WHERE session_id = @Sid", new { End = endTime, Sid = sessionId }, tx);
                
                // 3. Free Table
                if (!string.IsNullOrEmpty(label))
                {
                    await conn.ExecuteAsync("UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL WHERE label = @Label", new { Label = label }, tx);
                }
                
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
            return await conn.ExecuteScalarAsync<bool>("SELECT occupied FROM public.table_status WHERE label = @Label", new { Label = tableLabel });
        }

        public async Task MoveSessionAsync(Guid sessionId, string fromLabel, string toLabel)
        {
             using var conn = CreateConnection();
             await conn.OpenAsync();
             using var tx = await conn.BeginTransactionAsync();
             
             try 
             {
                 // Update session
                 await conn.ExecuteAsync("UPDATE public.table_sessions SET table_label = @To WHERE session_id = @Sid", new { To = toLabel, Sid = sessionId }, tx);
                 // Audit
                 await conn.ExecuteAsync("INSERT INTO public.table_session_moves(session_id, from_label, to_label, moved_at) VALUES(@Sid, @From, @To, now())", new { Sid = sessionId, From = fromLabel, To = toLabel }, tx);
                 
                 // Fetch session details for restoring status
                 var session = await conn.QuerySingleAsync("SELECT server_name, start_time FROM public.table_sessions WHERE session_id = @Sid", new { Sid = sessionId }, tx);

                 // Free old
                 await conn.ExecuteAsync("UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL WHERE label = @From", new { From = fromLabel }, tx);
                 
                 // Occupy new - Use UPDATE only, as target table must exist
                 var affected = await conn.ExecuteAsync(@"UPDATE public.table_status 
                                           SET occupied = true, start_time = @Start, server = @Srv 
                                           WHERE label = @To", 
                                           new { To = toLabel, Start = session.start_time, Srv = session.server_name }, tx);
                                           
                 if (affected == 0)
                 {
                     // Fallback: If table really doesn't exist (dynamic?), insert with default type 'table'
                      await conn.ExecuteAsync(@"INSERT INTO public.table_status(label, type, occupied, start_time, server) 
                                                VALUES(@To, 'table', true, @Start, @Srv)", 
                                                new { To = toLabel, Start = session.start_time, Srv = session.server_name }, tx);
                 }
                                           
                 await tx.CommitAsync();
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
                                  FROM public.table_sessions s
                                  WHERE s.session_id = @Sid";
             using var conn = CreateConnection();
             return await conn.QueryFirstOrDefaultAsync<SessionOverview>(sql, new { Sid = sessionId });
        }

        public async Task<BillPreviewDto> GetBillPreviewAsync(string tableLabel)
        {
            using var conn = CreateConnection();
            
            // 1. Get Session
            const string sessionSql = @"SELECT session_id, start_time, items FROM public.table_sessions WHERE table_label = @Label AND status = 'active'";
            var session = await conn.QueryFirstOrDefaultAsync<dynamic>(sessionSql, new { Label = tableLabel });

            if (session == null) return new BillPreviewDto();

            Guid sessionId = session.session_id;

            // 2. Fetch Items from SQL (ord schema)
            const string itemsSql = @"
                SELECT 
                    COALESCE(oi.snapshot_name, mi.name, 'Unknown Item') as name,
                    oi.quantity,
                    (oi.base_price + oi.price_delta) as price,
                    oi.menu_item_id::text as itemId
                FROM ord.order_items oi
                JOIN ord.orders o ON oi.order_id = o.order_id
                LEFT JOIN menu.menu_items mi ON oi.menu_item_id = mi.menu_item_id
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
    }
}
