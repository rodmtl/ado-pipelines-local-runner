# GitHub Actions Workflows

This directory contains GitHub Actions workflows for the ADO Pipelines Local Runner project.

## Workflows

### 1. CI - Build and Test (`ci-build-test.yml`)

**Triggers:**

- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Manual workflow dispatch

**Jobs:**

- **build-and-test**: Runs on multiple OS (Ubuntu, Windows, macOS)
  - Restores dependencies
  - Builds the solution in Release configuration
  - Runs tests with code coverage
  - Enforces 80% minimum coverage threshold
  - Uploads coverage artifacts (Ubuntu only)

- **coverage-report**: Generates detailed HTML coverage report
  - Creates HTML, JSON, and badge reports using ReportGenerator
  - Verifies coverage meets 80% threshold
  - Posts coverage summary as PR comment
  - Uploads HTML report as artifact

- **publish**: Publishes cross-platform executables (only on main branch)
  - Publishes self-contained executables for Windows, Linux, and macOS
  - Uploads build artifacts with 90-day retention

**Coverage Threshold:**

- Minimum: **80% line coverage**
- Build fails if coverage is below threshold

### 2. Code Coverage (`coverage.yml`)

**Triggers:**

- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Scheduled: Every Monday at 9 AM UTC

**Features:**

- Comprehensive coverage analysis with multiple output formats
- Detailed coverage reports (HTML, JSON, Markdown, Text)
- Coverage metrics displayed in job summary
- PR comments with coverage breakdown
- Test result publishing
- Coverage badge generation (requires GIST_SECRET configuration)

**Coverage Formats Generated:**

- OpenCover XML (for tooling integration)
- Cobertura XML (for Azure DevOps compatibility)
- JSON (for programmatic access)
- HTML (for human-readable reports)
- Markdown summary
- Text summary
- Coverage badges

## Prerequisites

### Required Secrets (for coverage badge)

To enable coverage badge generation, add the following secret to your repository:

1. **GIST_SECRET**: Personal Access Token with `gist` scope
   - Go to GitHub Settings → Developer settings → Personal access tokens
   - Generate new token with `gist` permission
   - Add to repository secrets as `GIST_SECRET`

2. Update `YOUR_GIST_ID_HERE` in `coverage.yml` with your actual Gist ID

### Repository Configuration

Ensure the following settings in your repository:

1. **Actions permissions**: Allow GitHub Actions to create and approve pull requests
2. **Branch protection** (recommended):
   - Require status checks to pass before merging
   - Require "Build and Test" and "Code Coverage Analysis" checks
   - Require branches to be up to date before merging

## Usage

### Viewing Coverage Reports

After a workflow run completes:

1. Go to the **Actions** tab in your repository
2. Select the workflow run
3. Download the `coverage-html-report` artifact
4. Extract and open `index.html` in a browser

### Local Coverage Testing

To run coverage locally before pushing:

```bash
# Generate coverage with threshold check
dotnet test ado-pipelines-local-runner/AdoPipelinesLocalRunner.sln \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=./coverage/ \
  /p:Threshold=80 \
  /p:ThresholdType=line

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"**/coverage/*.opencover.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html"

# View report
# Windows: start coverage-report/index.html
# Linux/macOS: open coverage-report/index.html
```

## Workflow Status Badges

Add these badges to your README.md:

```markdown
[![CI - Build and Test](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci-build-test.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci-build-test.yml)

[![Code Coverage](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/coverage.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/coverage.yml)
```

Replace `YOUR_USERNAME` and `YOUR_REPO` with your actual GitHub username and repository name.

## Customization

### Adjusting Coverage Threshold

To change the minimum coverage requirement, update the `MIN_COVERAGE` environment variable in the workflow files:

```yaml
env:
  MIN_COVERAGE: 80  # Change this value
```

### Adding Additional Platforms

To test on additional platforms, add them to the matrix in `ci-build-test.yml`:

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest, macos-13]  # Add more OS versions
```

### Excluding Files from Coverage

To exclude specific files or patterns from coverage analysis, update the Coverlet configuration:

```yaml
/p:Exclude="[*.Tests]*;[*]*.Designer;[*]*.g.cs"
```

## Troubleshooting

### Coverage threshold not enforced

If the build passes despite low coverage:

- Verify the `/p:Threshold` parameter is set correctly
- Check that `/p:ThresholdType=line` is specified
- Ensure `/p:ThresholdStat=total` is included

### Coverage report not generated

If the coverage report is missing:

- Check that tests actually ran (look for test output in logs)
- Verify the coverage output path matches the ReportGenerator input
- Ensure the test project references Coverlet packages

### PR comments not appearing

If coverage comments don't appear on PRs:

- Verify the workflow has `pull_request` trigger enabled
- Check that the GitHub token has appropriate permissions
- Ensure the repository allows Actions to create comments

## Best Practices

1. **Keep coverage above 80%**: This ensures good test quality
2. **Review coverage reports**: Check which lines/branches are not covered
3. **Test critical paths first**: Focus on high-value, high-risk code
4. **Don't chase 100%**: Some code (like trivial properties) may not need tests
5. **Update tests with features**: Add tests when adding new functionality

## Additional Resources

- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
