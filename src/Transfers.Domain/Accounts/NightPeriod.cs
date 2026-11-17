using System;
using System.Collections.Generic;
using System.Text;

namespace Transfers.Domain.Accounts
{
    public sealed record NightPeriod(TimeOnly Start, TimeOnly End)
    {
        public bool IsNight(TimeOnly time)
        {
            if (Start <= End)
            {
                return time >= Start && time < End;
            }
            else
            {
                return time >= Start || time < End;
            }
        }
    }
}
