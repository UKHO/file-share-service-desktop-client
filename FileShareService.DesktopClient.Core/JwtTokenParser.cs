using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IJwtTokenParser
    {
        IEnumerable<string> ParseRoles(string jwtToken);
    }

    [ExcludeFromCodeCoverage]
    public class JwtTokenParser : IJwtTokenParser
    {
        public IEnumerable<string> ParseRoles(string jwtToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
            var rolesClaims = jsonToken?.Claims.Where(c => c.Type == "roles") ?? Enumerable.Empty<Claim>();
            return rolesClaims.Select(c => c.Value).ToArray();
        }
    }
}