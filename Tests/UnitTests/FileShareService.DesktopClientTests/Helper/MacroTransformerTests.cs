using FakeItEasy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Helper;

namespace FileShareService.DesktopClientTests.Helper
{
    [TestFixture]
    public class MacroTransformerTests
    {
        private ICurrentDateTimeProvider fakeCurrentDateTimeProvider = null!;

        [SetUp]
        public void Setup()
        {
            fakeCurrentDateTimeProvider = A.Fake<ICurrentDateTimeProvider>();
        }

        [TestCase("$(now.Year)", "2022")]
        [TestCase("$(now.Year2)", "22")]
        [TestCase("$(now.Year   )", "2022")]
        [TestCase("$(now.Year2   )", "22")]
        [TestCase("$(   now.Year)", "2022")]
        [TestCase("$(   now.Year2)", "22")]
        [TestCase("$(   now.Year   )", "2022")]
        [TestCase("$(   now.Year2   )", "22")]
        [TestCase("$(   now.AddDays(30).Year   )", "2023")]
        [TestCase("$(   now.AddDays(30).Year2   )", "23")]
        
        [TestCase("$(now.WeekNumber)", "52")]
        [TestCase("$(now.AddDays(7).WeekNumber)", "1")]
        [TestCase("$(now.AddDays(21).WeekNumber)", "3")]
        [TestCase("$(now.AddDays(-14).WeekNumber)", "50")]
        [TestCase("$(now.WeekNumber   )", "52")]
        [TestCase("$(   now.WeekNumber)", "52")]
        [TestCase("$(   now.WeekNumber   )", "52")]
        [TestCase("$(now.WeekNumber+1)", "1")]
        [TestCase("$(now.WeekNumber +1)", "1")]
        [TestCase("$(now.WeekNumber + 1)", "1")]
        [TestCase("$(now.WeekNumber+ 1)", "1")]
        [TestCase("$(now.WeekNumber +10)", "10")]
        [TestCase("$(now.WeekNumber   -1)", "51")]
        [TestCase("$(now.WeekNumber-1)", "51")]
        [TestCase("$(now.WeekNumber -  1)", "51")]
        [TestCase("$(now.WeekNumber   -10)", "42")]

        [TestCase("$(now.WeekNumber2)", "52")]
        [TestCase("$(now.AddDays(7).WeekNumber2)", "01")]
        [TestCase("$(now.AddDays(21).WeekNumber2)", "03")]
        [TestCase("$(now.AddDays(-14).WeekNumber2)", "50")]
        [TestCase("$(now.WeekNumber2   )", "52")]
        [TestCase("$(   now.WeekNumber2)", "52")]
        [TestCase("$(   now.WeekNumber2   )", "52")]
        [TestCase("$(now.WeekNumber2+1)", "01")]
        [TestCase("$(now.WeekNumber2 +1)", "01")]
        [TestCase("$(now.WeekNumber2 + 1)", "01")]
        [TestCase("$(now.WeekNumber2+ 1)", "01")]
        [TestCase("$(now.WeekNumber2 +10)", "10")]
        [TestCase("$(now.WeekNumber2   -1)", "51")]
        [TestCase("$(now.WeekNumber2-1)", "51")]
        [TestCase("$(now.WeekNumber2 -  1)", "51")]
        [TestCase("$(now.WeekNumber2   -10)", "42")]

        [TestCase("$(now.WeekNumber.Year)", "2022")]
        [TestCase("$(now.WeekNumber.Year2)", "22")]
        [TestCase("$(now.AddDays(7).WeekNumber.Year)", "1")]
        [TestCase("$(now.AddDays(7).WeekNumber.Year2)", "1")]
        [TestCase("$(now.WeekNumber +10.Year)", "2023")]
        [TestCase("$(now.WeekNumber +10.Year2)", "23")]

        [TestCase("$(now)", "12/31/2022 00:00:00")]
        [TestCase("$(now.AddDays(0))", "12/31/2022 00:00:00")]
        [TestCase("$(now.AddDays(1))", "01/01/2023 00:00:00")]
        [TestCase("$(now.AddDays(+1))", "01/01/2023 00:00:00")]
        [TestCase("$(now.AddDays( 1 ))", "01/01/2023 00:00:00")]
        [TestCase("$(now.AddDays( + 1 ))", "01/01/2023 00:00:00")]
        [TestCase("$(now.AddDays(7))", "01/07/2023 00:00:00")]
        [TestCase("$(now.AddDays(31))", "01/31/2023 00:00:00")]
        [TestCase("$(now.AddDays(365))", "12/31/2023 00:00:00")]
        [TestCase("$(now.AddDays(-1))", "12/30/2022 00:00:00")]
        [TestCase("$(now.AddDays(-14))", "12/17/2022 00:00:00")]
        [TestCase("$(now.AddDays( - 1 ))", "12/30/2022 00:00:00")]

        
        public void DecemberTests(string input, string expected)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc));

            var transformer = new MacroTransformer(fakeCurrentDateTimeProvider);

            Assert.AreEqual(expected, transformer.ExpandMacros(input));
            
        }


        [TestCase("$( now )", "02/01/2022 00:00:00")]
        [TestCase("$( now.AddDays( 1 ) )", "02/02/2022 00:00:00")]
        [TestCase("$( now.AddDays(-1) )", "01/31/2022 00:00:00")]

        [TestCase("$(now.Day)", "1")]
        [TestCase("$( now.AddDays(7).Day )", "8")]
        [TestCase("$( now.AddDays( 28).Day )", "1")]
        [TestCase("$(now.AddDays(-22 ).Day)", "9")]

        [TestCase("$(now.Day2)", "01")]
        [TestCase("$( now.AddDays(7).Day2 )", "08")]
        [TestCase("$(now.AddDays( 28 ).Day2)", "01")]
        [TestCase("$(now.AddDays(-22).Day2)", "09")]

        [TestCase("$(now.Month)", "2")]
        [TestCase("$(now.AddDays(28).Month)", "3")]
        [TestCase("$(now.AddDays(-1).Month)", "1")]

        [TestCase("$(now.Month2)", "02")]
        [TestCase("$( now.AddDays( 28).Month2 )", "03")]
        [TestCase("$(now.AddDays(-1).Month2)", "01")]

        public void FebruaryTests(string input, string expected)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(2022, 02, 01, 0, 0, 0, DateTimeKind.Utc));

            var transformer = new MacroTransformer(fakeCurrentDateTimeProvider);

            Assert.AreEqual(expected, transformer.ExpandMacros(input));

        }

    }
}
