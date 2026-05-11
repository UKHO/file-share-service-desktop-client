# 02.03-test-projects: Upgrade test projects (depend on Core and WPF app)

## Objective
Upgrade both test projects to .NET 8.

## Scope
**Projects**:
1. FileShareService.DesktopClientTests (depends on FileShareService.DesktopClient)
2. FileShareService.DesktopClient.CoreTests (depends on both test projects and Core)

- Current TFM: net6.0-windows
- Target TFM: net8.0-windows

## Steps
1. Update TargetFramework to net8.0-windows in both test project files
2. Update any test-specific package references if needed
3. Build both test projects and fix compilation errors
4. Address any test framework API changes

## Validation
- Both test projects build successfully with 0 errors
- All unit tests pass

**Done when**: Both test projects target net8.0-windows, build successfully, all tests pass
