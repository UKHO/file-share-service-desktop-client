using System;
using System.ComponentModel;
using FakeItEasy;
using FileShareService.DesktopClientTests;
using Microsoft.Identity.Client;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;

namespace FileShareService.DesktopClient.CoreTests
{
    internal class AuthProviderTests
    {
        private AuthProviderForTesting authProvider = null!;
        private IEnvironmentsManager fakeEnvironmentsManager = null!;
        private INavigation fakeNavigation = null!;
        private IJwtTokenParser fakeJwtTokenParser = null!;

        [SetUp]
        public void Setup()
        {
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            fakeNavigation = A.Fake<INavigation>();
            fakeJwtTokenParser = A.Fake<IJwtTokenParser>();
            authProvider = new AuthProviderForTesting(fakeEnvironmentsManager, fakeNavigation, fakeJwtTokenParser);
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

            CollectionAssert.IsEmpty(authProvider.Roles);
        }

        [Test]
        public void TestWhenEnvironmentChangesLoggedInReset()
        {
            authProvider.SetAuthenticationResult(CreateAuthenticationResult(""));

            authProvider.AssertPropertiesChanged(() =>
                    fakeEnvironmentsManager.PropertyChanged += Raise.FreeForm<PropertyChangedEventHandler>.With(
                        fakeEnvironmentsManager,
                        new PropertyChangedEventArgs(nameof(fakeEnvironmentsManager.CurrentEnvironment))),
                nameof(authProvider.IsLoggedIn),
                nameof(authProvider.CurrentAccessToken),
                nameof(authProvider.CurrentAccessTokenExpiry));

            A.CallTo(() => fakeNavigation.RequestNavigate(NavigationTargets.Authenticate)).MustHaveHappened();
        }


        [Test]
        public void TestGetRolesAfterAuth()
        {
            var expectedToken = "aSecureToken";


            A.CallTo(() => fakeJwtTokenParser.ParseRoles(A<string>.That.Matches(s => s == expectedToken)))
                .Returns(new[] {"Role1"});

            authProvider.SetAuthenticationResult(CreateAuthenticationResult(expectedToken));


            CollectionAssert.AreEqual(new [] {"Role1"}, authProvider.Roles);
        }

        private AuthenticationResult CreateAuthenticationResult(string accessToken)
        {
            return new(accessToken, false, "",
                DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(1), "", null, "",
                new[] {"tenantId/.default"}, Guid.NewGuid(),
                new AuthenticationResultMetadata(TokenSource.IdentityProvider));
        }
    }

    internal class AuthProviderForTesting : AuthProvider
    {
        public AuthProviderForTesting(IEnvironmentsManager environmentsManager, INavigation navigation,
            IJwtTokenParser jwtTokenParser) : base(
            environmentsManager, navigation, jwtTokenParser)
        {
        }

        public void SetAuthenticationResult(AuthenticationResult? authResult)
        {
            authenticationResult = authResult;
            IsLoggedIn = authResult != null;
        }
    }
}