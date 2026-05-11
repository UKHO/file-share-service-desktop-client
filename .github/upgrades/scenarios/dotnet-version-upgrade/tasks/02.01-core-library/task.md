# 02.01-core-library: Upgrade Core library (foundation, no dependencies)

## Objective
Upgrade FileShareService.DesktopClient.Core to .NET 8 as the foundation library with no project dependencies.

## Scope
**Project**: FileShareService.DesktopClient.Core
- Current TFM: net6.0
- Target TFM: net8.0

**Package Updates** (for this project):
- Microsoft.Extensions.Configuration.Json: 7.0.0 → 8.0.1
- Microsoft.Extensions.Http.Polly: 7.0.10 → 8.0.26
- Microsoft.Extensions.Logging: 7.0.0 → 8.0.1
- System.Security.Cryptography.ProtectedData: 7.0.1 → 8.0.0

## Steps
1. Update TargetFramework to net8.0 in FileShareService.DesktopClient.Core.csproj
2. Update package references to .NET 8 versions
3. Build the project and fix any compilation errors
4. Address any source incompatibility issues in this project

## Validation
- Project builds successfully with 0 errors
- No warnings related to API incompatibilities

**Done when**: Core library targets net8.0, packages updated, builds successfully
