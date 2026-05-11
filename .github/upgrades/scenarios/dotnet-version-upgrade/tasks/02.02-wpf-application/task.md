# 02.02-wpf-application: Upgrade WPF application (depends on Core)

## Objective
Upgrade FileShareService.DesktopClient WPF application to .NET 8.

## Scope
**Project**: FileShareService.DesktopClient
- Current TFM: net6.0-windows
- Target TFM: net8.0-windows
- **Depends on**: FileShareService.DesktopClient.Core (Level 1)

**Package Updates** (for this project):
- Microsoft.Identity.Client: 4.55.0 → 4.84.0 (security fix)
- System.IdentityModel.Tokens.Jwt: 6.32.1 → 8.18.0 (security fix)
- Newtonsoft.Json: 13.0.3 → 13.0.4
- Review deprecated System.IdentityModel.Tokens.Jwt 6.34.0 usage

## Steps
1. Update TargetFramework to net8.0-windows in FileShareService.DesktopClient.csproj
2. Update package references including security vulnerability fixes
3. Build the project and fix WPF-related compilation errors
4. Address any WPF API changes for .NET 8

## Key Concerns
- WPF APIs may have behavioral changes (most are binary-only)
- Identity/JWT packages have major version jumps — may require code changes

## Validation
- Project builds successfully with 0 errors
- Application launches without runtime errors

**Done when**: WPF app targets net8.0-windows, all packages updated including security fixes, builds and launches successfully
