# Building from source

ADACT keeps shared MSBuild defaults in the repo root and limits CLI-specific versioning to the two distributable CLI projects.

## Shared build settings

`Directory.Build.props` applies these defaults repo-wide:

- `Nullable=enable`
- `ImplicitUsings=enable`
- `TreatWarningsAsErrors=true`

This makes new projects inherit the same language and warning policy without repeating the properties in each `.csproj`.

## CLI-specific versioning

`build/Adact.Cli.Versioning.props` is imported only by:

- `src/Adact.Cli/Adact.Cli.csproj`
- `src/Adact.Cli.Client/Adact.Cli.Client.csproj`

It centralizes:

- `_AdactCliVersioningEnabled=true`
- `AdactCliVersion` (which feeds `Version`)

Importing this props file is the rule that opts a project into CLI version stamping. `_AdactCliVersioningEnabled` is the opt-in marker, while `AdactCliVersion` remains the version override value. Other projects do not opt into this behavior.

## Gitless build behavior

For projects that import `build/Adact.Cli.Versioning.props`, `Directory.Build.targets` resolves `InformationalVersion` as follows:

1. Use the first 7 characters of `SourceRevisionId` when available.
2. Otherwise try `git rev-parse --short=7 HEAD`.
3. If Git metadata is unavailable, continue the build without failing.
4. If no short SHA is resolved, keep `InformationalVersion` equal to `Version`.

Expected result in a Git-less environment: the build still succeeds, and the CLI assembly `InformationalVersion` remains the plain `Version` value.

This is a permanent rule for `Adact.Cli` and `Adact.Cli.Client`; there is no separate enable/disable flag for short SHA stamping.

## Out of scope for this setup

`ContinuousIntegrationBuild` and `Deterministic` are recognized as related build settings, but they are not required by the current build configuration cleanup.
