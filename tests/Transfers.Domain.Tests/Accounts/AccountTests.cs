using System;
using System.Collections.Generic;
using System.Text;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Domain.Tests.Accounts
{
    public class AccountTests
    {
        private static Account CreateAccount(string holderName = "John Doe", decimal balance = 1000m, decimal overdraftLimit = 500m, AccountStatus status = AccountStatus.Active)
        {
            return new Account(Guid.NewGuid(), holderName, balance, overdraftLimit, status, new TransferLimits(5000m, 5, 1000m, 3));
        }

        [Fact]
        public void Credit_ShouldIncreaseBalance_WhenAccountIsActive()
        {
            // Arrange
            var account = CreateAccount();
            // Act
            account.Credit(200m);
            // Assert
            Assert.Equal(1200m, account.Balance);
        }

        [Fact]
        public void Debit_ShouldDecreaseBalance_WhenAccountIsActiveAndHasSufficientFunds()
        {
            // Arrange
            var account = CreateAccount();
            // Act
            account.Debit(200m);
            // Assert
            Assert.Equal(800m, account.Balance);
        }

        [Fact]
        public void Credit_ShouldThrowDomainException_WhenAccountIsNotActive()
        {
            // Arrange
            var account = CreateAccount(status: AccountStatus.Blocked);
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Credit(200m));
            Assert.Equal(FailureReason.AccountNotActive, exception.Reason);
        }

        [Fact]
        public void Debit_ShouldThrowDomainException_WhenAccountIsNotActive()
        {
            // Arrange
            var account = CreateAccount(status: AccountStatus.Blocked);
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Debit(200m));
            Assert.Equal(FailureReason.AccountNotActive, exception.Reason);
        }

        [Fact]
        public void Debit_ShouldThrowDomainException_WhenAccountHasInsufficientFunds()
        {
            // Arrange
            var account = CreateAccount(balance: 100m, overdraftLimit: 50m);
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Debit(200m));
            Assert.Equal(FailureReason.InsufficientFunds, exception.Reason);
            Assert.Equal(100m, account.Balance); 
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Credit_ShouldThrowDomainException_WhenAmountIsZeroOrNegative(decimal amount)
        {
            // Arrange
            var account = CreateAccount();
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Credit(amount));
            Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
        }

        [Fact]
        public void Debit_ShouldThrowDomainException_WhenAmountIsNegative()
        {
            // Arrange
            var account = CreateAccount();
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Debit(-100m));
            Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
        }

        [Fact]
        public void Debit_ShouldUseOverdraftLimit_WhenBalanceIsInsufficient()
        {
            // Arrange
            var account = CreateAccount(balance: 100m, overdraftLimit: 50m);
            // Act
            account.Debit(120m);
            // Assert
            Assert.Equal(-20m, account.Balance);
        }

        [Fact]
        public void Debit_ShouldThrowDomainException_WhenAmountIsZeroOrNegative()
        {
            // Arrange
            var account = CreateAccount();
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Debit(0m));
            Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
        }

        [Fact]
        public void Debit_ShouldAllow_WhenAmountEqualsBalancePlusOverdraftLimit()
        {
            // Arrange
            var account = CreateAccount(balance: 100m, overdraftLimit: 50m);
            // Act
            account.Debit(150m);
            // Assert
            Assert.Equal(-50m, account.Balance);
        }

        [Fact]
        public void Debit_ShouldThrowInsufficientFunds_WhenAmountExceedsBalancePlusOverdraftLimit()
        {
            // Arrange
            var account = CreateAccount(balance: 100m, overdraftLimit: 50m);
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => account.Debit(151m));
            Assert.Equal(FailureReason.InsufficientFunds, exception.Reason);
        }

        [Fact]
        public void Credit_ShouldCoverOverdraftFirst_WhenBalanceIsNegative()
        {
            // Arrange
            var account = CreateAccount(balance: -50m, overdraftLimit: 100m);
            // Act
            account.Credit(70m);
            // Assert
            Assert.Equal(20m, account.Balance);
        }

        [Fact]
        public void Constructor_ShouldThrowDomainException_WhenOverdraftLimitIsNegative()
        {
            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => CreateAccount(overdraftLimit: -100m));
            Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
        }

        [Fact]
        public void EnsureActive_ShouldNotThrow_WhenAccountIsActive()
        {
            var account = CreateAccount();

            var exception = Record.Exception(account.EnsureActive);

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(AccountStatus.Blocked)]
        [InlineData(AccountStatus.Inactive)]
        public void EnsureActive_ShouldThrowAccountNotActive_WhenAccountIsNotActive(AccountStatus status)
        {
            var account = CreateAccount(status: status);

            var exception = Assert.Throws<DomainException>(account.EnsureActive);

            Assert.Equal(FailureReason.AccountNotActive, exception.Reason);
        }
    }
}
