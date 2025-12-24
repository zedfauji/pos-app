using Dapper;
using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MagiDesk.Infrastructure.Repositories
{
    public class BillingRepository : IBillingRepository
    {
        private readonly string _connectionString;

        public BillingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres") 
                ?? configuration["Db:Postgres:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING")
                ?? throw new InvalidOperationException("Missing Postgres Connection String");
        }
        
        private NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task<Guid> CreateBillAsync(BillResult bill)
        {
             // Get table_id from tables table using table_label
             // If table_label doesn't match any table, use a placeholder but log it
             const string getTableIdSql = @"
                 SELECT t.table_id 
                 FROM public.tables t 
                 WHERE t.table_number = @TableLabel 
                 LIMIT 1";
             
             using var conn = CreateConnection();
             await conn.OpenAsync();
             
             var tableId = await conn.ExecuteScalarAsync<Guid?>(getTableIdSql, new { TableLabel = bill.TableLabel });
             
             // If no table found, use placeholder but this should be logged/fixed
             if (tableId == null || tableId == Guid.Empty)
             {
                 tableId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                 // Log warning - this is a data integrity issue
                 // TODO: Consider throwing exception or logging to audit table
             }
             
             const string sql = @"INSERT INTO billing.bills(bill_id, billing_id, session_id, table_id, table_label, server_id, server_name, start_time, end_time, time_minutes, items_total, time_total, subtotal, discounts, tax, total_amount, status, created_at, updated_at)
                                  VALUES(@BillId, @BillId, @SessionId, @TableId, @TableLabel, @ServerId, @ServerName, @StartTime, @EndTime, @TotalTimeMinutes, @ItemsCost, @TimeCost, @ItemsCost + @TimeCost, 0, 0, @TotalAmount, 'AwaitingPayment'::billing.bill_status, now(), now())";
             
             await conn.ExecuteAsync(sql, new 
             {
                 bill.BillId,
                 SessionId = bill.SessionId ?? Guid.Empty, // Use SessionId from BillResult
                 TableId = tableId.Value,
                 bill.TableLabel,
                 bill.ServerId,
                 bill.ServerName,
                 bill.StartTime,
                 bill.EndTime,
                 bill.TotalTimeMinutes,
                 bill.TimeCost,
                 bill.ItemsCost,
                 bill.TotalAmount
             });
             
             return bill.BillId;
        }

        public async Task<MagiDesk.Shared.DTOs.Reports.SalesStatsDto> GetDailySalesStatsAsync()
        {
             const string sql = @"
                SELECT 
                    COALESCE(SUM(total_amount), 0) as TotalSalesToday,
                    COUNT(1) as ClosedSessionsToday
                FROM billing.bills
                WHERE created_at >= CURRENT_DATE";
             
             const string sqlOpen = "SELECT COUNT(1) FROM public.\"TableSessions\" WHERE end_time IS NULL";

             using var conn = CreateConnection();
             
             var stats = await conn.QuerySingleOrDefaultAsync<MagiDesk.Shared.DTOs.Reports.SalesStatsDto>(sql);
             var open = await conn.ExecuteScalarAsync<int>(sqlOpen);
             
             if (stats == null) stats = new MagiDesk.Shared.DTOs.Reports.SalesStatsDto();
             stats.OpenSessionsCount = open;
             
             return stats;
        }
    }
}
