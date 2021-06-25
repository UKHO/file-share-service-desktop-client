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

        [Test]
        public void TestEnabledChangedWithAuthWithoutAdminRole()
        {
            Assert.IsFalse(adminButton.Enabled);

            A.CallTo(() => fakeAuthProvider.IsLoggedIn).Returns(true);
            A.CallTo(() => fakeAuthProvider.Roles).Returns(System.Array.Empty<string>());

            adminButton.AssertPropertyChangedNotFired(nameof(adminButton.Enabled), FireAuthProviderPropertyChanged);

            Assert.IsFalse(adminButton.Enabled);
        }


        [Test]
        public void TestEnabledChangedWithAuthAndAdminRole()
        {
            Assert.IsFalse(adminButton.Enabled);

            A.CallTo(() => fakeAuthProvider.IsLoggedIn).Returns(true);
            A.CallTo(() => fakeAuthProvider.Roles).Returns(new[] {"BatchCreate"});

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