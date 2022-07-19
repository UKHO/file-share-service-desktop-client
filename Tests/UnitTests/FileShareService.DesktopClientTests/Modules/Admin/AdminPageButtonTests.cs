using System.ComponentModel;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    public class AdminPageButtonTests
    {
        private AdminPageButton adminButton = null!;
        private IAuthProvider fakeAuthProvider = null!;

        [SetUp]
        public void Setup()
        {
            fakeAuthProvider = A.Fake<IAuthProvider>();
            adminButton = new AdminPageButton(fakeAuthProvider);
        }

        [Test]
        public void TestAdminPageButtonPropertyValues()
        {
            Assert.AreEqual("Admin", adminButton.DisplayName);
            Assert.AreEqual(NavigationTargets.Admin, adminButton.NavigationTarget);
        }

        [TestCase("BATCH CREATE")]
        [TestCase("BatchCreateMaritimeSafetyInformation")]
        [TestCase("BatchCreate_")]
        [TestCase(" BatchCreate_")]
        public void TestEnabledChangedWithAuthWithoutAdminRole(string adminRole)
        {
            Assert.IsFalse(adminButton.Enabled);

            A.CallTo(() => fakeAuthProvider.IsLoggedIn).Returns(true);
            A.CallTo(() => fakeAuthProvider.Roles).Returns(new [] { "IgnoreMe", adminRole });

            adminButton.AssertPropertyChangedNotFired(nameof(adminButton.Enabled), FireAuthProviderPropertyChanged);

            Assert.IsFalse(adminButton.Enabled);
        }


        [TestCase("BatchCreate")]
        [TestCase("Batchcreate")]
        [TestCase("batchCreate")]
        [TestCase(" batchcreate ")]
        [TestCase("BATCHCREATE")]
        [TestCase("BatchCreate_MaritimeSafetyInformation")]
        [TestCase(" batchcreate_MaritimeSafetyInformation ")]
        [TestCase("batchCreate_MaritimeSafetyInformation")]
        [TestCase("BATCHCREATE_MaritimeSafetyInformation")]
        public void TestEnabledChangedWithAuthAndAdminRole(string adminRole)
        {
            Assert.IsFalse(adminButton.Enabled);

            A.CallTo(() => fakeAuthProvider.IsLoggedIn).Returns(true);
            A.CallTo(() => fakeAuthProvider.Roles).Returns(new[] { "IgnoreMe", adminRole});

            adminButton.AssertPropertyChanged(nameof(adminButton.Enabled), FireAuthProviderPropertyChanged);

            Assert.IsTrue(adminButton.Enabled);
        }

        private void FireAuthProviderPropertyChanged()
        {
            fakeAuthProvider.PropertyChanged +=
                Raise.FreeForm<PropertyChangedEventHandler>.With(fakeAuthProvider,
                    new PropertyChangedEventArgs(nameof(fakeAuthProvider.IsLoggedIn)));
        }
    }
}