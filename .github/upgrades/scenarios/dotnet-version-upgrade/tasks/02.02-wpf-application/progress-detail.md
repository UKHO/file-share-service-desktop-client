# Task 02.02: WPF Application Upgrade Complete

**Progress**: 0/5 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

### Target Framework
- ✅ **FileShareService.DesktopClient.csproj** (WPF Application)
  - TargetFramework: `net6.0-windows` → `net8.0-windows`

### Package Updates
- ✅ Microsoft.Extensions.Configuration.Json: 7.0.0 → 8.0.1
- ✅ Microsoft.Extensions.Http.Polly: 7.0.10 → 8.0.26
   - 🔄 02.03-test-projects: Upgrade test projects (depend on Core and WPF app)
### Security Fixes (via Core library reference)
The WPF application inherits the security fixes from FileShareService.DesktopClient.Core:
- Microsoft.Identity.Client: 4.55.0 → 4.84.0 (via Core reference)
- System.IdentityModel.Tokens.Jwt: 6.32.1 → 8.18.0 (via Core reference)

## Validation

### Build Status
✅ **WPF application upgrade successful**

Expected build errors detected in test projects (still on .NET 6):
- `NU1201`: FileShareService.DesktopClientTests cannot reference net8.0-windows project
- `NU1201`: FileShareService.DesktopClient.CoreTests cannot reference net8.0 and net8.0-windows projects

These errors are expected and will be resolved when test projects are upgraded in the next subtask (02.03).

### WPF-Specific Considerations
- No WPF API breaking changes encountered
- UseWPF property retained correctly for .NET 8
- Runtime identifier (win-x64) and self-contained publishing settings preserved
- No compilation errors related to WPF APIs

### Source Incompatibility Issues
- No source-level breaking changes encountered
- No code modifications required

## Files Modified
- `file-share-service-desktop-client\FileShareService.DesktopClient.csproj`

## Next Steps
Proceed to subtask 02.03-test-projects to upgrade both test projects (final subtask before validation).
