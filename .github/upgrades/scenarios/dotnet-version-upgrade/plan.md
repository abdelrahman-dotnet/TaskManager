# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade the TaskManagerRefactor solution to target net10.0.
**Scope**: 3 projects (TaskManager.API, TaskManager.Bussiness, TaskManager.Data). Primary focus: change TargetFramework, update NuGet packages, and resolve any breaking changes so the solution builds and tests pass.

## Tasks

### 01-update-project-targetframeworks: Update project TargetFramework to net10.0

Description:
Change each project's TargetFramework to net10.0 (or add multi-targeting if required). Ensure project file properties, nullable settings, LangVersion, and implicit usings are adjusted as warranted.

Affected items:
- TaskManager.API/TaskManager.API.csproj
- TaskManager.Bussiness/TaskManager.Bussiness.csproj
- TaskManager.Data/TaskManager.Data.csproj

Done when:
- All project files target net10.0
- Solution builds without project file parsing errors

---

### 02-upgrade-nuget-packages: Update NuGet package references

Description:
Update packages to versions compatible with net10.0. Prioritize security fixes and packages flagged as deprecated or vulnerable in the assessment. Replace unsupported packages with recommended alternatives when no compatible version exists.

Affected items:
- All packages listed in assessment.md for the three projects

Done when:
- No package has known compatibility or security issues for net10.0
- Projects restore and build successfully

---

### 03-fix-api-breaking-changes: Resolve compile-time and API incompatibilities

Description:
Address source/binary incompatibilities flagged in the assessment. Apply code fixes, adjust APIs, and replace deprecated APIs with modern equivalents. Add compatibility shims only when necessary and documented in progress-details.md.

Done when:
- Solution builds without errors
- Unit/integration tests run (if present) and pass

---

### 04-run-build-and-tests: Validate the solution

Description:
Perform a full restore, build, and run tests. Fix any remaining warnings and treat warnings as errors for modified projects. Ensure no new analyzer warnings are introduced by the upgrade.

Done when:
- Full build succeeds
- All tests pass
- No new warnings introduced in modified projects

---

### 05-finalize-and-commit: Final clean up and create PR

Description:
Clean up temporary files, update scenario-instructions.md with decisions made, run dotnet format (optional), and create a PR from working branch to source branch with a descriptive summary of changes.

Done when:
- PR is created with code changes and test results
- scenario-instructions.md and progress artifacts are updated

---
