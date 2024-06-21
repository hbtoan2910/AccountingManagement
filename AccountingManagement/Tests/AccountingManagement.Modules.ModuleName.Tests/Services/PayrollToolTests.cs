using System;
using System.Linq;
using Moq;
using Xunit;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.ModuleName.Tests.Services
{
    public class PayrollToolTests
    {
        private PayrollTool _classUnderTest;

        public PayrollToolTests()
        {
            _classUnderTest = new PayrollTool();
        }

        [Fact]
        public void ShouldReturnNextPayrollPeriods()
        {
            var actuals = _classUnderTest.GetNextPayrollPeriods("2021-05", 12);

            Assert.True(actuals.Count() == 12);
            Assert.True(actuals.First().Equals("2021-06"));
            Assert.True(actuals.Last().Equals("2022-05"));
        }

        [Fact]
        public void ShouldReturnPreviousPayrollPeriods()
        {
            var actuals = _classUnderTest.GetPreviousPayrollPeriods("2021-05", 12);

            Assert.True(actuals.Count() == 12);
            Assert.True(actuals.First().Equals("2021-04"));
            Assert.True(actuals.Last().Equals("2020-05"));
        }
    }
}
