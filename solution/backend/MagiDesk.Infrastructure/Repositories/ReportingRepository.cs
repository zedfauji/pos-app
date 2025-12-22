using Dapper;
using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Reporting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace MagiDesk.Infrastructure.Repositories
{
    public class ReportingRepository : IReportingRepository
    {
        private readonly string _connectionString;

        public ReportingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres") 
                ?? configuration["Db:Postgres:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING")
                ?? throw new InvalidOperationException("Missing Postgres Connection String");
        }

        private NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task<ZReportDto> GetZReportAsync(DateTime dateUtc)
        {
            // We want payments made on this "Operational Day".
            // Assuming Day is 00:00 to 23:59 UTC for now.
            // TODO: Configurable Shift times.
            
            var startOfDay = dateUtc.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            const string sql = @"
                SELECT 
                    COALESCE(SUM(amount_paid), 0)
                FROM pay.payments
                WHERE created_at >= @Start AND created_at <= @End AND payment_method = @Method";

            const string sqlTotal = @"
                SELECT 
                     count(DISTINCT billing_id)
                FROM pay.payments
                WHERE created_at >= @Start AND created_at <= @End";

            using var conn = CreateConnection();
            
            var cash = await conn.ExecuteScalarAsync<decimal>(sql, new { Start = startOfDay, End = endOfDay, Method = "Cash" });
            var card = await conn.ExecuteScalarAsync<decimal>(sql, new { Start = startOfDay, End = endOfDay, Method = "Card" });
            var orders = await conn.ExecuteScalarAsync<int>(sqlTotal, new { Start = startOfDay, End = endOfDay });

            return new ZReportDto
            {
                StartTime = startOfDay,
                EndTime = endOfDay,
                TotalCash = cash,
                TotalCard = card,
                TotalSales = cash + card,
                TotalOrders = orders,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System" 
            };
        }
    }
}
