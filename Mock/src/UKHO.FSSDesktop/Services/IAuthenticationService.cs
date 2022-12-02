namespace UKHO.FSSDesktop.Services
{
    using System.Security;
    using Security;

    public interface IAuthenticationService
    {
        void Authenticate(string emailAddress, SecureString password, DeploymentEnvironment environment);

        SecurityContext? CurrentContext { get; }
    }
}