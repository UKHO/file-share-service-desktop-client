using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Prism.Mvvm;
using UKHO.FileShareClient;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IAuthProvider : INotifyPropertyChanged
    {
        bool IsLoggedIn { get; }
        string? CurrentAccessToken { get; }
        DateTimeOffset? CurrentAccessTokenExpiry { get; }
        Task<string?> Login();

        IEnumerable<string> Roles { get; }       
    }

    public class AuthProvider : BindableBase, IAuthProvider, IAuthTokenProvider
    {
        private readonly IEnvironmentsManager environmentsManager;
        private readonly INavigation navigation;
        private readonly IJwtTokenParser jwtTokenParser;
        private bool isLoggedIn;
        protected AuthenticationResult? authenticationResult;

        public AuthProvider(IEnvironmentsManager environmentsManager, INavigation navigation,
            IJwtTokenParser jwtTokenParser)
        {
            this.environmentsManager = environmentsManager;
            this.navigation = navigation;
            this.jwtTokenParser = jwtTokenParser;
            environmentsManager.PropertyChanged += (sender, args) => IsLoggedIn = false;
        }

        public bool IsLoggedIn
        {
            get => isLoggedIn;
            protected set
            {
                if (isLoggedIn != value)
                {
                    isLoggedIn = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(Roles));
                    RaisePropertyChanged(nameof(CurrentAccessToken));
                    RaisePropertyChanged(nameof(CurrentAccessTokenExpiry));
                    if (!value)
                        navigation.RequestNavigate(NavigationTargets.Authenticate);
                }
            }
        }

        [ExcludeFromCodeCoverage] // Can't unit test the login process as it is calling out to real AAD
        public async Task<string?> Login()
        {
            await GetToken();
            IsLoggedIn = true;
            return CurrentAccessToken;
        }

        public string? CurrentAccessToken => authenticationResult?.AccessToken;
        public DateTimeOffset? CurrentAccessTokenExpiry => authenticationResult?.ExpiresOn;

        public IEnumerable<string> Roles => IsLoggedIn && authenticationResult != null
            ? jwtTokenParser.ParseRoles(authenticationResult.AccessToken)
            : Enumerable.Empty<string>();

        public async  Task<string> GetToken()
        {
            var tenantId = environmentsManager.CurrentEnvironment.TenantId;
            var microsoftOnlineLoginUrl = @$"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
            var authority = $"{microsoftOnlineLoginUrl}{tenantId}";
            var scopes = new[] {$"{environmentsManager.CurrentEnvironment.ClientId}/.default" };

            var publicClientApplication = PublicClientApplicationBuilder
                .Create(environmentsManager.CurrentEnvironment.ClientId)
                .WithAuthority(authority)
                .WithRedirectUri("http://localhost")
                .Build();

            var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            TokenCacheHelper.EnableSerialization(publicClientApplication.UserTokenCache);         
            var accounts = (await publicClientApplication.GetAccountsAsync()).ToList();
            try
            {
                authenticationResult = await publicClientApplication.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync(cancellationSource.Token)
                    .ConfigureAwait(false);
            }
            // There is no access token in the cache, so prompt the user to sign-in.
            catch (MsalUiRequiredException)
            {
                authenticationResult = await publicClientApplication.AcquireTokenInteractive(scopes).ExecuteAsync(cancellationSource.Token);
            }
            RaisePropertyChanged(nameof(CurrentAccessToken));          
            return authenticationResult.AccessToken;

        }
    }
}