using System;
using System.Collections.Generic;
using System.Text;

namespace Transfers.Domain.Transfers
{
    public enum TransferStatus
    {
        Scheduled,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}
