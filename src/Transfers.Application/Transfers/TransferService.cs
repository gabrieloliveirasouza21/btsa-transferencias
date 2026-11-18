using Microsoft.Extensions.Options;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers
{
    public class TransferService
    {
        private readonly IAccountRepository _accounts;
        private readonly ITransferRepository _transfers;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly TransferRulesOptions _rules;
        private readonly TimeZoneInfo _timeZone;

        public TransferService(
            IAccountRepository accounts,
            ITransferRepository transfers,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider,
            IOptions<TransferRulesOptions> rules)
        {
            _accounts = accounts;
            _transfers = transfers;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _rules = rules.Value;
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_rules.TimeZoneId);
        }

        public async Task<TransferResponse> CreateAsync(CreateTransferRequest request, CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow();
            var transfer = Transfer.CreateImmediate(request.SourceAccountId, request.DestinationAccountId, request.Amount, now);

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await ProcessAsync(transfer, now, token);
                _transfers.Add(transfer);
                return transfer;
            }, ct);

            return TransferResponse.From(transfer);
        }

        public async Task<TransferResponse> ScheduleAsync(ScheduleTransferRequest request, CancellationToken ct)
        {
            var transfer = Transfer.CreateScheduled(
                request.SourceAccountId,
                request.DestinationAccountId,
                request.Amount,
                request.ScheduledFor,
                _timeProvider.GetUtcNow());

            await EnsureAccountExistsAsync(transfer.SourceAccountId, ct);
            await EnsureAccountExistsAsync(transfer.DestinationAccountId, ct);

            _transfers.Add(transfer);
            await _unitOfWork.SaveChangesAsync(ct);

            return TransferResponse.From(transfer);
        }

        public async Task<TransferResponse> CancelAsync(Guid id, CancellationToken ct)
        {
            var transfer = await _transfers.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Transfer", id);

            transfer.Cancel(_timeProvider.GetUtcNow());
            await _unitOfWork.SaveChangesAsync(ct);

            return TransferResponse.From(transfer);
        }

        public async Task<ScheduledBatchResult> ProcessDueScheduledAsync(int batchSize, CancellationToken ct)
        {
            var dueIds = await _transfers.ListDueScheduledIdsAsync(_timeProvider.GetUtcNow(), batchSize, ct);
            var processed = 0;
            var skipped = 0;

            foreach (var id in dueIds)
            {
                try
                {
                    var ran = await _unitOfWork.ExecuteInTransactionAsync(token => ProcessScheduledAsync(id, token), ct);
                    if (ran) processed++; else skipped++;
                }
                catch (ConcurrencyConflictException)
                {
                    skipped++;
                }
            }

            return new ScheduledBatchResult(processed, skipped);
        }

        public async Task<TransferResponse> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var transfer = await _transfers.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Transfer", id);

            return TransferResponse.From(transfer);
        }

        private async Task ProcessAsync(Transfer transfer, DateTimeOffset now, CancellationToken ct)
        {
            var (source, destination) = await LockAccountsAsync(transfer.SourceAccountId, transfer.DestinationAccountId, ct);

            try
            {
                source.EnsureActive();
                destination.EnsureActive();

                var usage = await _transfers.GetUsageSinceAsync(source.Id, now - _rules.WindowLength, ct);
                source.Limits.EnsureAllows(IsNight(now), usage.Amount, usage.Attempts, transfer.Amount);

                source.Debit(transfer.Amount);
                destination.Credit(transfer.Amount);
                transfer.Complete(now);
            }
            catch (DomainException ex)
            {
                transfer.Fail(ex.Reason, now);
            }
        }

        private async Task<bool> ProcessScheduledAsync(Guid id, CancellationToken ct)
        {
            var transfer = await _transfers.GetByIdAsync(id, ct);
            if (transfer is null || transfer.Status != TransferStatus.Scheduled)
            {
                return false;
            }

            var now = _timeProvider.GetUtcNow();
            transfer.StartProcessing(now);
            await ProcessAsync(transfer, now, ct);

            return true;
        }

        private async Task EnsureAccountExistsAsync(Guid id, CancellationToken ct)
        {
            if (await _accounts.GetByIdAsync(id, ct) is null)
            {
                throw new NotFoundException("Account", id);
            }
        }

        private async Task<(Account Source, Account Destination)> LockAccountsAsync(Guid sourceId, Guid destinationId, CancellationToken ct)
        {
            var locked = new Dictionary<Guid, Account>();

            foreach (var id in new[] { sourceId, destinationId }.Order())
            {
                locked[id] = await _accounts.GetForUpdateAsync(id, ct)
                    ?? throw new NotFoundException("Account", id);
            }

            return (locked[sourceId], locked[destinationId]);
        }

        private bool IsNight(DateTimeOffset utcNow)
        {
            var local = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
            return _rules.NightPeriod.IsNight(TimeOnly.FromTimeSpan(local.TimeOfDay));
        }
    }
}
