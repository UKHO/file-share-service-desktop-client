using FakeItEasy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Helper;

namespace FileShareService.DesktopClientTests.Helper
{
    [TestFixture]
    public class DateTimeValidatorTests
    {
        private IMacroTransformer macroTransformer = null!;
        private ICurrentDateTimeProvider fakeCurrentDateTimeProvider = null!;
        private IDateTimeValidator dateTimeValidator = null!;
        private List<string> list = null!;

        private readonly string[] validFormats = new string[] { "yyyy-MM-ddTHH:mm:ss" };


        [SetUp]
        public void Setup()
        {
            fakeCurrentDateTimeProvider = A.Fake<ICurrentDateTimeProvider>();
            macroTransformer = new MacroTransformer(fakeCurrentDateTimeProvider);
            dateTimeValidator = new DateTimeValidator(macroTransformer);
            list = new List<string>();

            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(DateTime.UtcNow);
        }

        [Test]
        public void TestForValidFormat()
        {
            list.Clear();

            DateTime? expectedDate = dateTimeValidator.ValidateExpiryDate(true, validFormats, "2022-02-14T15:30:10", list);

            Assert.IsTrue(expectedDate.HasValue);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(new DateTime(2022, 02, 14), expectedDate!.Value.Date);
        }

        [Test]
        public void TestForValidMacro()
        {
            list.Clear();

            DateTime? expectedDate = dateTimeValidator.ValidateExpiryDate(true, validFormats, "$(now.AddDays(5))", list);

            Assert.IsTrue(expectedDate.HasValue);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(DateTime.Now.AddDays(5).Date, expectedDate!.Value.Date);
        }

        [Test]
        public void TestForInvalidFormat()
        {
            list.Clear();

            DateTime? expectedDate = dateTimeValidator.ValidateExpiryDate(true, validFormats, "12/10/2022", list);

            Assert.IsNull(expectedDate);
            Assert.AreEqual(1, list.Count);
            StringAssert.StartsWith("The expiry date format is invalid", list[0]);
        }
    }
}
