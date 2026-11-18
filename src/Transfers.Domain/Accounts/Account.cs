using System;
using System.Collections.Generic;
using System.Text;
using Transfers.Domain.Common;

namespace Transfers.Domain.Accounts
{
    public class Account
    {
        public Guid Id { get; private set; }
        public string HolderName { get; private set; } = null!;
        public decimal Balance { get; private set; } 
        public decimal OverdraftLimit { get; private set; }
        public AccountStatus Status { get; private set; }
        public TransferLimits Limits { get; private set; } = null!;
        public decimal AvailableAmount => Balance + OverdraftLimit;

        public Account(Guid id, string holderName, decimal balance, decimal overdraftLimit, AccountStatus status, TransferLimits limits)
        {
            if (overdraftLimit < 0)
            {
                throw new DomainException(FailureReason.InvalidAmount, "Overdraft limit cannot be negative.");
            }

            this.Id = id;
            this.HolderName = holderName;
            this.Balance = balance;
            this.OverdraftLimit = overdraftLimit;
            this.Status = status;
            this.Limits = limits;
        }

        private Account()
        {
        }

        public void EnsureActive()
        {
            if (Status != AccountStatus.Active)
            {
                throw new DomainException(FailureReason.AccountNotActive, "Account is not active.");
            }
        }

        public void Credit(decimal amount)
        {
            if (amount <= 0)
            {
                throw new DomainException(FailureReason.InvalidAmount, "Credit amount must be greater than zero.");
            }
            EnsureActive();
            Balance += amount;
        }

        public void Debit(decimal amount)
        {
            if (amount <= 0)
            {
                throw new DomainException(FailureReason.InvalidAmount, "Debit amount must be greater than zero.");
            }
            EnsureActive();
            if (amount > AvailableAmount)
            {
                throw new DomainException(FailureReason.InsufficientFunds, "Insufficient funds for the debit operation.");
            }
            Balance -= amount;
        }

    }
}
