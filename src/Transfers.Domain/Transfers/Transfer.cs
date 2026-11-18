using System;
using System.Collections.Generic;
using System.Text;
using Transfers.Domain.Common;

namespace Transfers.Domain.Transfers
{
    public class Transfer
    {
        public Guid Id { get; private set; }
        public Guid SourceAccountId { get; private set; }
        public Guid DestinationAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public TransferStatus Status { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? ScheduledFor { get; private set; }
        public DateTimeOffset? ProcessedAt { get; private set; }
        public FailureReason? FailureReason { get; private set; }

        private Transfer()
        {

        }

        public static Transfer CreateImmediate(Guid source, Guid destination, decimal amount, DateTimeOffset now)
        {
            if(amount <= 0)
            {
                throw new DomainException(Common.FailureReason.InvalidAmount , "Transfer amount must be greater than zero.");
            }
            if(source == destination)
            {
                throw new DomainException(Common.FailureReason.SameAccount, "Source and destination accounts must be different.");
            }
            return new Transfer
            {
                Id = Guid.NewGuid(),
                SourceAccountId = source,
                DestinationAccountId = destination,
                Amount = amount,
                Status = TransferStatus.Processing,
                CreatedAt = now
            };
        }

        public static Transfer CreateScheduled(Guid source, Guid destination, decimal amount, DateTimeOffset scheduledFor, DateTimeOffset now)
        {
            if (amount <= 0)
            {
                throw new DomainException(Common.FailureReason.InvalidAmount, "Transfer amount must be greater than zero.");
            }
            if (source == destination)
            {
                throw new DomainException(Common.FailureReason.SameAccount, "Source and destination accounts must be different.");
            }
            if (scheduledFor <= now)
            {
                throw new DomainException(Common.FailureReason.InvalidScheduledDate, "Scheduled date must be in the future.");
            }
            return new Transfer
            {
                Id = Guid.NewGuid(),
                SourceAccountId = source,
                DestinationAccountId = destination,
                Amount = amount,
                Status = TransferStatus.Scheduled,
                CreatedAt = now,
                ScheduledFor = scheduledFor
            };
        }

        public void Complete(DateTimeOffset processedAt)
        {
            if (Status != TransferStatus.Processing)
            {
                throw new DomainException(Common.FailureReason.InvalidStatusTransition, "Transfer can only be marked as processed if it is in Processing status.");
            }
            Status = TransferStatus.Completed;
            ProcessedAt = processedAt;
        }

        public void Fail(FailureReason reason, DateTimeOffset processedAt)
        {
            if (Status != TransferStatus.Processing)
            {
                throw new DomainException(Common.FailureReason.InvalidStatusTransition, "Transfer can only be marked as failed if it is in Processing status.");
            }
            Status = TransferStatus.Failed;
            FailureReason = reason;
            ProcessedAt = processedAt;
        }

        public void StartProcessing(DateTimeOffset processedAt)
        {
            if (Status != TransferStatus.Scheduled)
            {
                throw new DomainException(Common.FailureReason.InvalidStatusTransition, "Transfer can only be marked as processing if it is in Scheduled status.");
            }
            Status = TransferStatus.Processing;
            ProcessedAt = processedAt;
        }

        public void Cancel(DateTimeOffset processedAt)
        {
            if (Status != TransferStatus.Scheduled)
            {
                throw new DomainException(Common.FailureReason.InvalidStatusTransition, "Transfer can only be marked as canceled if it is in Scheduled status.");
            }
            Status = TransferStatus.Cancelled;
            ProcessedAt = processedAt;
        }
    }
}
