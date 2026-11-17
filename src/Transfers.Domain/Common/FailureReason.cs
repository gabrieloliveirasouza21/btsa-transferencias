using System;
using System.Collections.Generic;
using System.Text;

namespace Transfers.Domain.Common
{
    public enum FailureReason
    {
        InvalidAmount,
        SameAccount,
        AccountNotActive,
        InsufficientFunds,
        HourlyLimitExceeded,
        HourlyAttemptsExceeded,
        InvalidScheduledDate,
        InvalidStatusTransition,
    }
}
