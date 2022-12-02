namespace UKHO.FSSDesktop.Infrastructure.Services
{
    using System.Security;
    using CommunityToolkit.Mvvm.Messaging;
    using FSSDesktop.Services;
    using Messages.Security;
    using Security;

    internal class AuthenticationService : IAuthenticationService
    {
        private SecurityContext? _currentContext;

        public void Authenticate(string emailAddress, SecureString password, DeploymentEnvironment environment)
        {
            // Simulate a remote call
            Task.Factory.StartNew(() =>
            {
                var claims = new List<SecurityClaim>();
                var displayName = string.Empty;

                switch (emailAddress)
                {
                    case "dan@ukho":
                        claims.Add(new SecurityClaim(WellKnownClaim.Access));
                        displayName = "Dan Beasley";
                        break;
                    case "john@ukho":
                        claims.Add(new SecurityClaim(WellKnownClaim.Access));
                        claims.Add(new SecurityClaim(WellKnownClaim.AdministerImport));
                        displayName = "John Rippington";
                        break;
                    case "jivitesh@ukho":
                        claims.Add(new SecurityClaim(WellKnownClaim.Access));
                        claims.Add(new SecurityClaim(WellKnownClaim.AdministerImport));
                        claims.Add(new SecurityClaim(WellKnownClaim.ExpireBatches));
                        displayName = "Jivitesh Trivedi";
                        break;
                    case "martyn@ukho":
                        claims.Add(new SecurityClaim(WellKnownClaim.Access));
                        claims.Add(new SecurityClaim(WellKnownClaim.AdministerImport));
                        claims.Add(new SecurityClaim(WellKnownClaim.ExpireBatches));
                        claims.Add(new SecurityClaim(WellKnownClaim.AdministerSecurity));
                        displayName = "Martyn Fewtrell";
                        break;
                }

                var context = claims.Any() ? new SecurityContext(emailAddress, displayName, environment, claims) : null;
                _currentContext = context;

                //Thread.Sleep(2000);

                WeakReferenceMessenger.Default.Send(new AuthenticationMessage(context));
            });
        }

        public SecurityContext? CurrentContext => _currentContext;
    }
}