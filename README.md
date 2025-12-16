# ADO Pipelines Local Runner

[![CI - Build and Test](https://github.com/rodmtl/ado-pipelines-local-runner/actions/workflows/ci-build-test.yml/badge.svg)](https://github.com/rodmtl/ado-pipelines-local-runner/actions/workflows/ci-build-test.yml)
[![Code Coverage](https://github.com/rodmtl/ado-pipelines-local-runner/actions/workflows/coverage.yml/badge.svg)](https://github.com/rodmtl/ado-pipelines-local-runner/actions/workflows/coverage.yml)
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/rodmtl/1c398fffaec63391fb3f602849382e7b/raw/coverage-badge.json)](https://github.com/rodmtl/ado-pipelines-local-runner/actions/workflows/coverage.yml)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A command-line tool for validating Azure DevOps YAML pipelines locally, enabling fast feedback without pushing to the cloud.

## Features

- ✅ **YAML Syntax Validation** - Detect malformed YAML with precise error locations
- ✅ **Schema Validation** - Validate against Azure DevOps pipeline schema
- ✅ **Template Resolution** - Resolve and expand local template references
- ✅ **Variable Processing** - Support for `$(var)` and `${{ variables.var }}` syntax
- ✅ **Circular Reference Detection** - Prevent infinite template loops
- ✅ **Multiple Output Formats** - Text, JSON, SARIF, and Markdown reports
- ✅ **Strict Mode** - Treat warnings as errors for stricter validation
- ✅ **Cross-Platform** - Runs on Windows, macOS, and Linux

## Installation

### Download Pre-built Binary (Recommended)

Download the latest release for your platform from the [Releases](https://github.com/rodmtl/ado-pipelines-local-runner/releases) page:

**Windows:**

```powershell
# Download azp-local-win-x64.zip and extract
Expand-Archive -Path azp-local-win-x64.zip -DestinationPath azp-local
cd azp-local
.\azp-local.exe --help
```

**Linux:**

```bash
# Download and extract azp-local-linux-x64.tar.gz
tar -xzf azp-local-linux-x64.tar.gz
chmod +x azp-local
./azp-local --help
```

**macOS:**

```bash
# Download and extract azp-local-osx-x64.tar.gz
tar -xzf azp-local-osx-x64.tar.gz
chmod +x azp-local
./azp-local --help
```

> 💡 **Tip:** Add the extracted directory to your PATH for easier access.

### Build from Source

If you prefer to build from source:

**Prerequisites:**

- .NET 8.0 SDK or later

**Steps:**

```bash
# Clone the repository
git clone https://github.com/rodmtl/ado-pipelines-local-runner.git
cd ado-pipelines-local-runner

# Build the project
dotnet build

# Run tests
dotnet test

# Publish a self-contained executable
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Publish then run

```bash
dotnet publish -c Release
.\src\bin\Release\net8.0\publish\azp-local.exe validate --pipeline azure-pipelines.yml
```

### Basic Validation

```bash
azp-local validate --pipeline azure-pipelines.yml
```

### With Variables

```bash
# Using variable files
azp-local validate --pipeline build.yml --vars vars/common.yml --vars vars/prod.yml

# Using inline variables
azp-local validate --pipeline build.yml --var buildConfig=Release --var environment=prod
```

### With Templates

```bash
azp-local validate --pipeline ci.yml --base-path ./
```

### Output Formats

```bash
# JSON output
azp-local validate --pipeline build.yml --output json

# SARIF format (for tool integration)
azp-local validate --pipeline build.yml --output sarif

# Markdown report
azp-local validate --pipeline build.yml --output markdown

# Save to file
azp-local validate --pipeline build.yml --output json --log-file validation-report.json
```

### Strict Mode

```bash
# Treat warnings as errors
azp-local validate --pipeline build.yml --strict
```

### Allow Unresolved Variables

```bash
# Report undefined variables as warnings instead of errors
azp-local validate --pipeline build.yml --allow-unresolved
```

## CLI Options

| Option | Description | Default |
|--------|-------------|---------|
| `--pipeline` | Path to the pipeline YAML file to validate | **Required** |
| `--base-path` | Base directory for resolving template references | Current directory |
| `--vars` | Variable files (YAML format, can specify multiple) | None |
| `--var` | Inline variable in key=value format (can specify multiple) | None |
| `--schema-version` | Azure DevOps schema version to validate against | Latest |
| `--output` | Output format: text\|json\|sarif\|markdown | text |
| `--strict` | Treat warnings as errors | false |
| `--allow-unresolved` | Allow undefined variables (reported as warnings) | false |
| `--verbosity` | Logging level: quiet\|minimal\|normal\|detailed | normal |
| `--log-file` | Optional file path to save validation report | None |

## Exit Codes

- **0** - Success (no errors)
- **1** - Validation errors found
- **3** - Configuration or runtime errors

## Examples

### Example 1: Simple Pipeline Validation

```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - script: echo Hello World
    displayName: 'Say Hello'
```

```bash
azp-local validate --pipeline azure-pipelines.yml
# Output: ✅ Validation successful
```

### Example 2: Pipeline with Templates

```yaml
# azure-pipelines.yml
trigger:
  - main

extends:
  template: templates/build-template.yml
```

```bash
azp-local validate --pipeline azure-pipelines.yml --base-path ./
```

### Example 3: Pipeline with Variables

```yaml
# build.yml
trigger:
  - main

variables:
  - name: buildConfiguration
    value: $(BuildConfig)

steps:
  - script: dotnet build -c $(buildConfiguration)
```

```bash
azp-local validate --pipeline build.yml --var BuildConfig=Release
```

### Example 4: Multi-Stage Pipeline

```yaml
# ci-cd.yml
trigger:
  - main

stages:
  - stage: Build
    jobs:
      - job: BuildJob
        steps:
          - script: dotnet build
  
  - stage: Deploy
    jobs:
      - job: DeployJob
        steps:
          - script: dotnet publish
```

```bash
azp-local validate --pipeline ci-cd.yml --output json --strict
```

## Development

### Project Structure

```text
ado-pipelines-local-runner/
├── src/                        # Source code
│   ├── Contracts/              # Interfaces and models
│   ├── Core/                   # Core implementation
│   │   ├── Orchestration/      # Validation orchestrator
│   │   ├── Parsing/            # YAML parser
│   │   ├── Validators/         # Syntax validators
│   │   ├── Schema/             # Schema manager
│   │   ├── Templates/          # Template resolver
│   │   ├── Variables/          # Variable processor
│   │   └── Reporting/          # Error reporter
│   └── Program.cs              # CLI entry point
├── tests/                      # Unit tests
│   └── Unit/                   # Unit test suites
├── docs/                       # Documentation
└── demos/                      # Example pipelines
```

### Architecture

The project follows **SOLID principles** with a clean architecture:

- **Presentation Layer**: CLI (Program.cs)
- **Orchestration Layer**: ValidationOrchestrator coordinates all components
- **Domain Layer**: Parsers, validators, resolvers, processors
- **Infrastructure Layer**: File system, caching, logging

**Key Design Patterns**:

- Strategy Pattern (validators, resolvers)
- Dependency Injection (all components wired via DI)
- Builder Pattern (validation result aggregation)
- Adapter Pattern (file system abstraction)

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run specific test category
dotnet test --filter "FullyQualifiedName~YamlParser"

# Generate code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Code Coverage

```bash
# Generate OpenCover report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/

# Generate HTML report (requires ReportGenerator)
dotnet tool install -g reportgenerator
reportgenerator -reports:"./coverage/coverage.opencover.xml" -targetdir:"./coverage/report" -reporttypes:Html
```

**Target**: 80%+ line coverage

### Contributing

1. Follow existing code style and patterns
2. Write unit tests for new features (TDD preferred)
3. Ensure all tests pass before submitting
4. Update documentation for user-facing changes

## Performance

- Syntax validation of typical pipelines (< 500 lines) completes in **< 1 second**
- Startup time: **< 5 seconds** (cold start)

## Roadmap

### Phase 1 (Current - MVP)

- ✅ YAML syntax validation
- ✅ Schema validation
- ✅ Local template resolution
- ✅ Basic variable processing
- ✅ Error reporting with multiple formats

### Phase 2 (Planned)

- 🔲 Remote template fetching (HTTP/Git)
- 🔲 Advanced variable scoping
- 🔲 Execution simulation
- 🔲 Linting and best practices checks
- 🔲 IDE integration (VS Code extension)

## Troubleshooting

### Common Issues

**Error: File not found**

```bash
# Ensure the pipeline file path is correct
azp-local validate --pipeline azure-pipelines.yml

# Use absolute paths if needed
azp-local validate --pipeline "C:/projects/myapp/azure-pipelines.yml"
```

**Error: Template not found**

```bash
# Specify the correct base path for templates
azp-local validate --pipeline build.yml --base-path ./
```

**Error: Undefined variable**

```bash
# Define variables using --var or --vars
azp-local validate --pipeline build.yml --var myVar=value

# Or allow unresolved variables as warnings
azp-local validate --pipeline build.yml --allow-unresolved
```

## License

See [LICENSE](LICENSE) file for details.

## Badge Setup

The project uses GitHub Actions to generate coverage and test badges. To set up the dynamic badges (coverage percentage and test counts):

1. See the detailed setup guide: [Badge Setup Documentation](.github/BADGE_SETUP.md)
2. You'll need to:
   - Create a GitHub Personal Access Token with `gist` scope
   - Create a public gist to store badge data
   - Add `GIST_SECRET` and `GIST_ID` to repository secrets
   - Update the badge URLs in this README with your username and gist ID

The static badges (build status, .NET version, license) work immediately without setup.

## Related Documentation

- [Phase 1 MVP Specifications](docs/Phase1-MVP-Specs.md)
- [Architecture Documentation](docs/ARCHITECTURE.md)
- [Functional Requirements Verification](../FUNCTIONAL_REQUIREMENTS_VERIFICATION.md)
- [Acceptance Criteria Validation](../ACCEPTANCE_CRITERIA_VALIDATION.md)
- [Badge Setup Guide](.github/BADGE_SETUP.md)

## Support

For issues, questions, or contributions, please refer to the project documentation in the `docs/` directory.
