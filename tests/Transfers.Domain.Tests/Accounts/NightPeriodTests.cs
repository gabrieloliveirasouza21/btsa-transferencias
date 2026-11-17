using System;
using System.Collections.Generic;
using System.Text;
using Transfers.Domain.Accounts;

namespace Transfers.Domain.Tests.Accounts
{
    public class NightPeriodTests
    {
        [Theory]
        [InlineData("00:00:00", true)]
        [InlineData("05:59:59", true)]
        [InlineData("06:00:00", false)]
        [InlineData("23:59:59", true)]
        [InlineData("20:00:00", true)]
        [InlineData("19:59:59", false)]
        [InlineData("12:00:00", false)]
        public void IsNight_ShouldReturnExpectedResult(string timeString, bool expectedResult)
        {
            // Arrange
            var nightPeriod = new NightPeriod(new TimeOnly(20, 0), new TimeOnly(6, 0));
            var time = TimeOnly.Parse(timeString);
            // Act
            var result = nightPeriod.IsNight(time);
            // Assert
            Assert.Equal(expectedResult, result);
        }

    }
}
