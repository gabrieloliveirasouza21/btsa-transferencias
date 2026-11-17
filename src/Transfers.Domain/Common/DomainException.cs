using System;
using System.Collections.Generic;
using System.Text;

namespace Transfers.Domain.Common
{
    public class DomainException : Exception
    {
        public FailureReason Reason { get; }

        public DomainException(FailureReason reason, string message) : base(message)
        {
            Reason = reason;
        }
    }
}
