namespace UKHO.FSSDesktop.Security
{
    using System.Collections.Generic;

    public class SecurityContext
    {
        private readonly string _userName;
        private readonly string _displayName;
        private readonly DeploymentEnvironment _environment;
        private readonly IEnumerable<SecurityClaim> _claims;

        public SecurityContext(string userName, string displayName, DeploymentEnvironment environment, IEnumerable<SecurityClaim> claims)
        {
            _userName = userName;
            _displayName = displayName;
            _environment = environment;
            _claims = claims;
        }


        public string UserName => _userName;

        public DeploymentEnvironment Environment1 => _environment;

        public IEnumerable<SecurityClaim> Claims => _claims;

        public string DisplayName => _displayName;
    }
}