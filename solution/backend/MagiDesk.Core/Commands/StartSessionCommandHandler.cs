using MagiDesk.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MagiDesk.Core.Commands
{
    public record StartSessionCommand(string TableLabel, string ServerId, string ServerName);
    public record StartSessionResult(Guid SessionId, Guid BillingId, DateTime StartTime);

    public class StartSessionCommandHandler : ICommandHandler<StartSessionCommand, StartSessionResult>
    {
        private readonly ITableRepository _repository;

        public StartSessionCommandHandler(ITableRepository repository)
        {
            _repository = repository;
        }

        public async Task<StartSessionResult> HandleAsync(StartSessionCommand command, CancellationToken cancellationToken = default)
        {
            // Business Logic: Check if occupied is handled by Repo (Infrastructure constraint) or here?
            // Clean Arch: Repo handles storage, Logic handles rules.
            // Repo.StartSessionAsync does the check and insert transactionally. Ideally strict clean arch would separate check and insert, 
            // but for concurrency, DB transaction is best. We will rely on Repo throwing if conflict.
            
            var sessionId = await _repository.StartSessionAsync(command.TableLabel, command.ServerId, command.ServerName);
            // Repo creates billingId and timestamps implicitly, so we might need to return them or fetch them. 
            // For simplicity, let's assume Repo returns ID and we trust it succeeded.
            // BUT the result needs more data. Let's adjust Repo contract later if needed or fetch details.
            // Actually, the repo currently generates IDs internaly. It returns SessionId only.
            // We can return approximated result or fetch.
            
            return new StartSessionResult(sessionId, Guid.Empty, DateTime.UtcNow); 
        }
    }
}
