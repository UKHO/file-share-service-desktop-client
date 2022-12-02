namespace UKHO.FSSDesktop.Security
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Runtime.InteropServices.ComTypes;

    public class SecurityToken
    {
        private readonly List<SecurityClaim> _claims;

        public SecurityToken(IEnumerable<SecurityClaim> claims)
        {
            _claims = new List<SecurityClaim>(claims);
        }

        public IEnumerable<SecurityClaim> Claims => _claims;

        public bool HasClaim(string name)
        {
            return _claims.Any(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}