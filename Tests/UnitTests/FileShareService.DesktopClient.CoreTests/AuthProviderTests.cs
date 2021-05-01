using FakeItEasy;
using FileShareService.DesktopClientTests;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;

namespace FileShareService.DesktopClient.CoreTests
{
    internal class AuthProviderTests
    {
        private AuthProvider authProvider = null!;
        private IEnvironmentsManager fakeEnvironmentsManager = null!;
        private INavigation fakeNavigation = null!;

        [SetUp]
        public void Setup()
        {
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            fakeNavigation = A.Fake<INavigation>();
            authProvider = new AuthProvider(fakeEnvironmentsManager, fakeNavigation);
        }

        [Test]
        public void TestIsLoggedInPropertyDefaultsToFalse()
        {
            Assert.IsFalse(authProvider.IsLoggedIn);
        }

        [Test]
        public void TestAuthPropertiesDefaultToNull()
        {
            Assert.IsNull(authProvider.CurrentAccessTokenExpiry);
            Assert.IsNull(authProvider.CurrentAccessToken);
        }
    }
}