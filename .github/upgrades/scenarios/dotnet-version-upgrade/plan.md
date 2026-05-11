# .NET 8 Upgrade Plan

## Overview

**Target**: Upgrade WPF solution from .NET 6 to .NET 8.0 (LTS)
**Scope**: 5 projects (1 core library, 1 WPF application, 2 test projects, 1 WiX installer)

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 5 projects, all on .NET 6, clear dependency structure. Despite 785 issues, 760 are binary incompatibilities requiring recompilation only, not code changes. Minimal source-level breaking changes (10 issues) and standard WPF upgrade path make this a straightforward upgrade.

## Tasks

### 01-prerequisites: Validate Prerequisites

Verify that the development environment is ready for .NET 8:
- Confirm .NET 8 SDK is installed on the machine
- Validate any global.json files are compatible with .NET 8 SDK
- Check for any environment-specific constraints

**Done when**: .NET 8 SDK installation confirmed, no global.json blocking issues found

---

### 02-atomic-upgrade: Upgrade All Projects to .NET 8

Update all .NET projects simultaneously to target .NET 8:

**Target framework updates:**
- FileShareService.DesktopClient.Core: net6.0 → net8.0
- FileShareService.DesktopClient (WPF): net6.0-windows → net8.0-windows
- FileShareService.DesktopClient.CoreTests: net6.0-windows → net8.0-windows
- FileShareService.DesktopClientTests: net6.0-windows → net8.0-windows

**Package updates** (apply across all projects):
- Microsoft.Identity.Client: 4.55.0 → 4.84.0 (security vulnerability fix)
- System.IdentityModel.Tokens.Jwt: 6.32.1 → 8.18.0 (security vulnerability fix)
- Microsoft.Extensions.Configuration.Json: 7.0.0 → 8.0.1
- Microsoft.Extensions.Http.Polly: 7.0.10 → 8.0.26
- Microsoft.Extensions.Logging: 7.0.0 → 8.0.1
- Newtonsoft.Json: 13.0.3 → 13.0.4
- System.Security.Cryptography.ProtectedData: 7.0.1 → 8.0.0

**Deprecated package handling:**
- Review System.IdentityModel.Tokens.Jwt version 6.34.0 (deprecated) usage

**Code fixes:**
- Address 10 source incompatibility issues (Api.0002)
- Fix compilation errors after framework and package updates
- The 760 binary incompatibility issues (Api.0001) will be resolved by recompilation

**Note**: The WiX installer project (FileShareService.DesktopClient.Installer.wixproj) is not a .NET project and does not require framework changes.

**Done when**: All 4 .NET projects target .NET 8, all package updates applied, solution builds with 0 errors

---

### 03-validation: Validate Upgrade

Run comprehensive validation to ensure the upgrade is successful:
- Build entire solution from clean state
- Run all unit tests (both test projects)
- Verify WPF application launches successfully
- Confirm no runtime errors or warnings

**Done when**: Solution builds successfully, all tests pass, WPF application starts without errors

