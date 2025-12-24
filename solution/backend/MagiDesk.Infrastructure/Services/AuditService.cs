using Dapper;
using MagiDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace MagiDesk.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly string _connectionString;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IConfiguration configuration, ILogger<AuditService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task LogEventAsync<T>(
            string actorId,
            string actionType,
            string entityType,
            string entityId,
            T? beforeState,
            T? afterState,
            string correlationId,
            string source = "API")
        {
            await LogInternalAsync(actorId, actionType, entityType, entityId, 
                JsonSerializer.Serialize(beforeState), 
                JsonSerializer.Serialize(afterState), 
                correlationId, source);
        }

        public async Task LogEventAsync(
            string actorId,
            string actionType,
            string entityType,
            string entityId,
            string correlationId,
            string source = "API")
        {
            await LogInternalAsync(actorId, actionType, entityType, entityId, null, null, correlationId, source);
        }

        private async Task LogInternalAsync(
            string actorId, 
            string actionType, 
            string entityType, 
            string entityId, 
            string? beforeStateJson, 
            string? afterStateJson, 
            string correlationId,
            string source)
        {
            const string sql = @"
                INSERT INTO audit.events 
                (actor_id, action_type, entity_type, entity_id, correlation_id, before_state, after_state, source)
                VALUES 
                (@ActorId, @ActionType, @EntityType, @EntityId, @CorrelationId, @BeforeState::jsonb, @AfterState::jsonb, @Source)";

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.ExecuteAsync(sql, new
                {
                    ActorId = actorId,
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    CorrelationId = correlationId,
                    BeforeState = beforeStateJson,
                    AfterState = afterStateJson,
                    Source = source
                });
            }
            catch (Exception ex)
            {
                // Fallback logging - Audit failure should NOT break the transaction if possible, 
                // but for high integrity systems, maybe it SHOULD. 
                // For now, we log the failure critically so the app continues, 
                // but alerts should fire.
                _logger.LogCritical(ex, "FAILED TO WRITE AUDIT LOG: {ActionType} on {EntityId}", actionType, entityId);
                
                // In a stricter mode, we would re-throw to rollback the comprehensive transaction.
                // throw; 
            }
        }
    }
}
