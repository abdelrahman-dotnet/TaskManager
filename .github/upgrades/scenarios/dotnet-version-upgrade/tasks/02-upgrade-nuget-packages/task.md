# 02-upgrade-nuget-packages: Update NuGet package references

Description:
Update packages to versions compatible with net10.0. Prioritize security fixes and packages flagged as deprecated or vulnerable in the assessment. Replace unsupported packages with recommended alternatives when no compatible version exists.

Affected items:
- All packages listed in assessment.md for the three projects

Done when:
- No package has known compatibility or security issues for net10.0
- Projects restore and build successfully
