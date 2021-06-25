using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;

namespace FileShareService.DesktopClient.CoreTests
{
    public class JwtTokenParserTests
    {
        [Test]
        public void TestRolesWithARealToken()
        {
            var result = new JwtTokenParser().ParseRoles(
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjIwODk5MzgyLCJuYmYiOjE2MjA4OTkzODIsImV4cCI6MTYyMDkwMzI4MiwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQUI4QlhpbnFobS9mTW4wK0JWOHpUcmpYa2ZWeC9uQkdsTFpvTE8xMmhjeVZSbzZ1b0ZmRHhoK3ZCeTlSeWNSdXBCU2R3bnhncDVPOWxqUE8wZHVma2FqbW9hUzJJcmNDb05FZ3JrY1hsK1R3PSIsImFtciI6WyJwd2QiLCJtZmEiXSwiYXBwaWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IlJvY2stRXZhbnMiLCJnaXZlbl9uYW1lIjoiTWFydGluIiwiaXBhZGRyIjoiNjIuMTcyLjEwOC42IiwibmFtZSI6Ik1hcnRpbiBSb2NrLUV2YW5zIiwib2lkIjoiNTI1NTYzZDYtODEzMy00YzdkLThiZDMtMjhhOGQwYmVlNGRiIiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTU0MjA1MTk1NC02NDYzODY0NjgtMTA1MDg4Nzk3NC00MTUxMCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBREUuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiVlI3cklLRjhURV9iVmtEelg3WFFIVWRRS0hKay1RdElPa2g0MEkyZGtXTSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiYW1hcnRpbkB1a2hvLmdvdi51ayIsInVwbiI6ImFtYXJ0aW5AdWtoby5nb3YudWsiLCJ1dGkiOiIyWVhpZjFTdkZraVpQcFNhYm5WYUFBIiwidmVyIjoiMS4wIn0.R1ARCFATQe-hZz_HCfXqBeAOw64JUhfIB2M5KSzvB6co_DsjD2Z7e7s-hEjM87Fg7KpqZy-ZyiK5v8NoDz0_t1MK2yAIgalMUoS172iLkuP_nx5ACBjzkfOUugrygZDg96a1EBlC3K_HfVsLhdHUfINrKE4-GxdmqtIxS6TD0-EFj71rBZWmjdyN9p6pLz3F5YD2bN8pZ-txTEE8QhUjRXr840P2Fym0Xxkn4nsPx7pgNJ36skanlG3EoSIx9URsqe9dUFF2NyCO7yh3EJfPAASTomNSE9TPV_y3v0L0qs1E8vQDG-evikupFKKt6e1ogj-qTJB4vDCbx4skD0CSsg");

            CollectionAssert.AreEqual(new[] {"BatchCreate"}, result);
        }

        [Test]
        public void TestWithSingleRole()
        {
            var token = GenerateToken("Role1");
            var result = new JwtTokenParser().ParseRoles(token);
            CollectionAssert.AreEqual(new[] {"Role1"}, result);
        }

        [Test]
        public void TestWithMultipleRoles()
        {
            var expectedRoles = new[] {"public", "Role1", "Role2"};
            var token = GenerateToken(expectedRoles);
            var result = new JwtTokenParser().ParseRoles(token);
            CollectionAssert.AreEquivalent(expectedRoles, result);
        }

        public string GenerateToken(params string[] roles)
        {
            var mySecret = "asdv234234^&%&^%&^hjsdfb2%%%";
            var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

            var myIssuer = "http://mysite.com";
            var myAudience = "http://myaudience.com";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new(ClaimTypes.NameIdentifier, "Bob")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            if (roles.Length > 0)
                tokenDescriptor.Claims = new Dictionary<string, object> {{"roles", roles}};

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}