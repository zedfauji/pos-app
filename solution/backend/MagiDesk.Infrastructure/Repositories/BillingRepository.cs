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
             const string sql = @"INSERT INTO public.bills(bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount, status, is_settled)
                                  VALUES(@BillId, @TableLabel, @ServerId, @ServerName, @StartTime, @EndTime, @TotalTimeMinutes, @ItemsJson::jsonb, @TimeCost, @ItemsCost, @TotalAmount, 'awaiting_payment', false)";
             
             // Serialize items
             var itemsJson = JsonSerializer.Serialize(bill.Items);
             
             using var conn = CreateConnection();
             await conn.ExecuteAsync(sql, new 
             {
                 bill.BillId,
                 bill.TableLabel,
                 bill.ServerId,
                 bill.ServerName,
                 bill.StartTime,
                 bill.EndTime,
                 bill.TotalTimeMinutes,
                 ItemsJson = itemsJson,
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
                FROM public.bills
                WHERE created_at >= CURRENT_DATE OR end_time >= CURRENT_DATE";
             
             const string sqlOpen = "SELECT COUNT(1) FROM public.table_sessions WHERE status = 'active'";

             using var conn = CreateConnection();
             
             var stats = await conn.QuerySingleOrDefaultAsync<MagiDesk.Shared.DTOs.Reports.SalesStatsDto>(sql);
             var open = await conn.ExecuteScalarAsync<int>(sqlOpen);
             
             if (stats == null) stats = new MagiDesk.Shared.DTOs.Reports.SalesStatsDto();
             stats.OpenSessionsCount = open;
             
             return stats;
        }
    }
}
