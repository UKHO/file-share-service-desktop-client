using System;
using System.ComponentModel;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Auth;

namespace FileShareService.DesktopClientTests.Modules.Auth
{
    public class AuthenticateViewModelTests
    {
        private AuthenticateViewModel authenticateViewModel = null!;
        private IEnvironmentsManager fakeEnvironmentsManager = null!;
        private IAuthProvider fakeAuthProvider = null!;
        private INavigation fakeNavigation = null!;

        [SetUp]
        public void Setup()
        {
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            fakeAuthProvider = A.Fake<IAuthProvider>();
            fakeNavigation = A.Fake<INavigation>();
            authenticateViewModel = new AuthenticateViewModel(fakeAuthProvider, fakeEnvironmentsManager,
                fakeNavigation);
        }

        [Test]
        public void TestCurrentEnvironment()
        {
            var environmentConfig1 = new EnvironmentConfig();
            var environmentConfig2 = new EnvironmentConfig();
            fakeEnvironmentsManager.CurrentEnvironment = environmentConfig1;

            Assert.AreSame(environmentConfig1, authenticateViewModel.CurrentEnvironment);

            authenticateViewModel.AssertPropertyChanged(nameof(authenticateViewModel.CurrentEnvironment),
                () => authenticateViewModel.CurrentEnvironment = environmentConfig2);
            authenticateViewModel.AssertPropertyChangedNotFired(nameof(authenticateViewModel.CurrentEnvironment),
                () => authenticateViewModel.CurrentEnvironment = environmentConfig2);
            Assert.AreSame(environmentConfig2, authenticateViewModel.CurrentEnvironment);

            authenticateViewModel.AssertPropertyChanged(nameof(authenticateViewModel.CurrentEnvironment),
                () => authenticateViewModel.CurrentEnvironment = environmentConfig1);
            Assert.AreSame(environmentConfig1, authenticateViewModel.CurrentEnvironment);
        }

        [Test]
        public void TestFirePropertyChangedEventsWhenEnvironmentChanges()
        {
            authenticateViewModel.AssertPropertiesChanged(() =>
            {
                fakeAuthProvider.PropertyChanged +=
                    Raise.FreeForm<PropertyChangedEventHandler>.With(fakeAuthProvider,
                        new PropertyChangedEventArgs(nameof(fakeAuthProvider.IsLoggedIn)));
            }, nameof(authenticateViewModel.IsAuthenticated));
        }

        [Test]
        public void TestLogin()
        {
            var authTaskTcs = new TaskCompletionSource<string?>();

            A.CallTo(() => fakeAuthProvider.Login()).Returns(authTaskTcs.Task);
            A.CallTo(() => fakeEnvironmentsManager.CurrentEnvironment)
                .Returns(new EnvironmentConfig {Name = "UnitTestEnv"});

            authenticateViewModel.LoginCommand.Execute();


            Assert.IsTrue(authenticateViewModel.IsAuthenticating);
            Assert.IsFalse(authenticateViewModel.IsAuthenticated);

            A.CallTo(() => fakeAuthProvider.CurrentAccessTokenExpiry).Returns(DateTimeOffset.Now);

            authTaskTcs.SetResult("Token");

            Assert.IsFalse(authenticateViewModel.IsAuthenticating);
            Assert.IsFalse(authenticateViewModel.IsAuthenticated);
            A.CallTo(() => fakeNavigation.RequestNavigate(NavigationTargets.Search)).MustHaveHappened();
        }
    }
}