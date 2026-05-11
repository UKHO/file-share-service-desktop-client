# Scenario Instructions: dotnet-version-upgrade

**Status**: Planning Complete
**Description**: Upgrade WPF solution from .NET 6 to .NET 8.0 (LTS)

## Workflow Stages

- [x] Assessment
- [x] Planning
- [ ] Execution

## Strategy
**Selected**: All-At-Once
**Rationale**: 5 projects, all on .NET 6, clear dependency structure. Despite 785 issues, 760 are binary incompatibilities requiring recompilation only. Minimal source-level breaking changes (10 issues) and standard WPF upgrade path make atomic upgrade most efficient.

### Execution Constraints
- Single atomic upgrade — all projects updated together
- Validate full solution build after upgrade
- Fix all compilation errors in one bounded pass (not iterative)
- Testing occurs after atomic upgrade completes successfully

## Preferences

### Flow Mode
**Guided** — Pause after each stage (assessment, plan, breakdowns) for user review

### Source Control
- Source branch: main
- Working branch: Feature/Abzu-dot-net-upgrade-8.0
- Target framework: .NET 8.0 (LTS)

### Commit Strategy
**After Each Task** — Commit after each task completes successfully

### Technical Preferences
- **Target Framework**: .NET 8.0 (LTS)
- **Solution Type**: Windows WPF application
- **Security Vulnerabilities**: Include in upgrade (Microsoft.Identity.Client, System.IdentityModel.Tokens.Jwt)

## Decisions
- All-At-Once strategy selected based on low code change risk and small solution size
- Security vulnerability fixes included by default (2 packages)
