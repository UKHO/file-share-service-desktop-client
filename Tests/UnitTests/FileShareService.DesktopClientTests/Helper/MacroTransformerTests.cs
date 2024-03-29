﻿using FakeItEasy;
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
        [TestCase("$(now.AddDays(7).WeekNumber.Year)", "2023")]
        [TestCase("$(now.AddDays(7).WeekNumber.Year2)", "23")]
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

            RunTest(input, expected);

        }


        [TestCase("$( now )", "02/01/2022 00:00:00")]
        [TestCase("$( now.AddDays( 1 ) )", "02/02/2022 00:00:00")]
        [TestCase("$( now.AddDays(-1) )", "01/31/2022 00:00:00")]

        [TestCase("$(now.Day)", "1")]
        [TestCase("$( now.AddDays(7).Day )", "8")]
        [TestCase("$( now.AddDays( 28).Day )", "1")]
        [TestCase("$(now.AddDays(-23 ).Day)", "9")]

        [TestCase("$(now.Day2)", "01")]
        [TestCase("$( now.AddDays(7).Day2 )", "08")]
        [TestCase("$(now.AddDays( 28 ).Day2)", "01")]
        [TestCase("$(now.AddDays(-23).Day2)", "09")]

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

            RunTest(input, expected);
        }

        [TestCase(1, "$(now.MonthName)", "January")]
        [TestCase(1, "$(now.MonthShortName)", "Jan")]
        [TestCase(1, "$( now.AddDays( +31 ).MonthName )", "February")]
        [TestCase(1, "$( now.AddDays(+31 ).MonthShortName )", "Feb")]
        [TestCase(3, "$( now.MonthName)", "March")]
        [TestCase(3, "$(now.MonthShortName )", "Mar")]
        [TestCase(4, "$(now.MonthName )", "April")]
        [TestCase(4, "$( now.MonthShortName )", "Apr")]
        [TestCase(5, "$(now.MonthName )", "May")]
        [TestCase(5, "$( now.MonthShortName )", "May")]
        [TestCase(6, "$(now.MonthName )", "June")]
        [TestCase(6, "$( now.MonthShortName )", "Jun")]
        [TestCase(7, "$(now.MonthName)", "July")]
        [TestCase(7, "$(now.MonthShortName)", "Jul")]
        [TestCase(8, "$(now.MonthName)", "August")]
        [TestCase(8, "$(now.MonthShortName)", "Aug")]
        [TestCase(9, "$(now.MonthName)", "September")]
        [TestCase(9, "$(now.MonthShortName)", "Sep")]
        [TestCase(10, "$(now.MonthName)", "October")]
        [TestCase(10, "$(now.MonthShortName)", "Oct")]
        [TestCase(11, "$(now.MonthName)", "November")]
        [TestCase(11, "$(now.MonthShortName)", "Nov")]
        [TestCase(12, "$(now.MonthName)", "December")]
        [TestCase(12, "$(now.MonthShortName)", "Dec")]
        public void MonthNameTests(int month, string input, string expected)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(2022, month, 01, 0, 0, 0, DateTimeKind.Utc));

            RunTest(input, expected);
        }

        [TestCase("$(now.Month) $(now.MonthName)", "1 January")]
        [TestCase("$(now.MonthName) $(now.Month)", "January 1")]
        [TestCase("$(now.MonthName) $(now.Month) $(now.MonthName)", "January 1 January")]
        public void MonthMatching(string input, string expected)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc));

            RunTest(input, expected);
        }

        [TestCase(4, "$(now.DayName)", "Monday")]
        [TestCase(4, "$(now.DayShortName)", "Mon")]
        [TestCase(4, "$( now.AddDays( +1).DayName )", "Tuesday")]
        [TestCase(4, "$( now.AddDays(+1 ).DayShortName )", "Tue")]
        [TestCase(4, "$( now.AddDays( 2 ).DayName)", "Wednesday")]
        [TestCase(7, "$(now.AddDays( -1 ).DayShortName )", "Wed")]
        [TestCase(7, "$(now.DayName)", "Thursday")]
        [TestCase(7, "$( now.DayShortName )", "Thu")]
        [TestCase(8, "$(now.DayName )", "Friday")]
        [TestCase(8, "$( now.DayShortName )", "Fri")]
        [TestCase(9, "$(now.DayName )", "Saturday")]
        [TestCase(9, "$( now.DayShortName )", "Sat")]
        [TestCase(10, "$(now.DayName )", "Sunday")]
        [TestCase(10, "$( now.DayShortName )", "Sun")]
        public void DayNameTests(int day, string input, string expected)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(2022, 07, day, 0, 0, 0, DateTimeKind.Utc));

            RunTest(input, expected);
        }

        private void RunTest(string input, string expanded)
        {
            var transformer = new MacroTransformer(fakeCurrentDateTimeProvider);

            Assert.AreEqual(expanded, transformer.ExpandMacros(input));

            var macroBetwix = $"\\\\ukho.business.data\\{input}\\files";
            var expandedBetwix = $"\\\\ukho.business.data\\{expanded}\\files";

            Assert.AreEqual(expandedBetwix, transformer.ExpandMacros(macroBetwix));

            macroBetwix = $"| {input} |";
            expandedBetwix = $"| {expanded} |";

            Assert.AreEqual(expandedBetwix, transformer.ExpandMacros(macroBetwix));
        }
    }
}
