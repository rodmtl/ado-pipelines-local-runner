# Coverage Badge Setup Guide

This guide explains how to set up coverage and test badges for the ADO Pipelines Local Runner project.

## Overview

The project uses GitHub Actions to generate coverage reports and create dynamic badges that display:
- Build status
- Code coverage percentage
- Test results
- .NET version
- License

## Prerequisites

1. A GitHub account with repository admin access
2. A GitHub Personal Access Token (PAT) with `gist` scope

## Setup Steps

### 1. Create a GitHub Personal Access Token (PAT)

1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token (classic)"
3. Give it a descriptive name (e.g., "Coverage Badge Token")
4. Select the `gist` scope (only this scope is needed)
5. Set an expiration (recommended: 1 year)
6. Click "Generate token"
7. **Copy the token immediately** - you won't be able to see it again

### 2. Create a Public Gist

1. Go to https://gist.github.com/
2. Create a new gist with the filename `coverage-badge.json`
3. Add this initial content:
   ```json
   {
     "schemaVersion": 1,
     "label": "Coverage",
     "message": "Initializing...",
     "color": "lightgrey"
   }
   ```
4. Make sure it's set to **Public**
5. Click "Create public gist"
6. Copy the Gist ID from the URL (e.g., `https://gist.github.com/username/abc123def456` → `abc123def456`)

### 3. Add GitHub Secrets

1. Go to your repository on GitHub
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Add the following secrets:

   **Secret 1: GIST_SECRET**
   - Name: `GIST_SECRET`
   - Value: Your Personal Access Token from Step 1

   **Secret 2: GIST_ID**
   - Name: `GIST_ID`
   - Value: Your Gist ID from Step 2

### 4. Update README.md

Replace the placeholder values in README.md:

```markdown
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/coverage-badge.json)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/coverage.yml)
```

Replace:
- `YOUR_USERNAME` with your GitHub username
- `YOUR_GIST_ID` with your Gist ID from Step 2
- `YOUR_REPO` with your repository name

Example:
```markdown
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/johndoe/abc123def456/raw/coverage-badge.json)](https://github.com/johndoe/ado-pipelines-runner/actions/workflows/coverage.yml)
```

### 5. Trigger the Workflow

1. Push a commit to the `main` branch, or
2. Manually trigger the "Code Coverage" workflow from the Actions tab
3. Wait for the workflow to complete
4. The badges in your README should now display the correct values

## Badge Types

### 1. Build Status Badge
```markdown
[![CI - Build and Test](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci-build-test.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci-build-test.yml)
```
- Shows: passing/failing status
- Updates: On every CI run
- No setup required

### 2. Coverage Workflow Badge
```markdown
[![Code Coverage](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/coverage.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/coverage.yml)
```
- Shows: passing/failing status
- Updates: On every coverage run
- No setup required

### 3. Coverage Percentage Badge (Dynamic)
```markdown
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/coverage-badge.json)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/coverage.yml)
```
- Shows: Actual coverage percentage (e.g., 85%)
- Color: Green (≥80%), Yellow (≥60%), Red (<60%)
- Updates: On main branch pushes
- **Requires setup** (Steps 1-4 above)

### 4. Test Results Badge (Dynamic)
```markdown
[![Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/tests-badge.json)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci-build-test.yml)
```
- Shows: Number of passing tests
- Updates: On main branch pushes
- **Requires setup** (Steps 1-4 above)

### 5. Static Badges (No Setup Required)

**.NET Version:**
```markdown
[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
```

**License:**
```markdown
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
```

## Troubleshooting

### Badge Shows "Initializing..." or "unknown"
- The workflow hasn't run yet on the main branch
- Check that the workflow completed successfully
- Verify your GIST_SECRET and GIST_ID are correct
- Wait a few minutes for GitHub's CDN to update

### Badge Shows Error
- Verify the Gist is public (not secret)
- Check that the PAT has the `gist` scope
- Ensure GIST_SECRET and GIST_ID secrets are set correctly
- Check the workflow logs for errors

### Badge Not Updating
- Badges only update on pushes to the `main` branch
- Check the workflow condition: `if: github.ref == 'refs/heads/main'`
- Clear your browser cache or view in incognito mode
- Add `?cacheSeconds=300` to force refresh: `raw/coverage-badge.json?cacheSeconds=300`

### Token Expired
- Create a new PAT following Step 1
- Update the GIST_SECRET in repository secrets

## Alternative: Codecov.io

If you prefer a third-party service:

1. Sign up at https://codecov.io/ with your GitHub account
2. Add the repository to Codecov
3. Get your Codecov token
4. Add to GitHub secrets as `CODECOV_TOKEN`
5. Add this step to coverage.yml:
   ```yaml
   - name: Upload coverage to Codecov
     uses: codecov/codecov-action@v4
     with:
       token: ${{ secrets.CODECOV_TOKEN }}
       files: ./coverage/coverage.opencover.xml
       fail_ci_if_error: true
   ```
6. Use Codecov's badge in README:
   ```markdown
   [![codecov](https://codecov.io/gh/YOUR_USERNAME/YOUR_REPO/branch/main/graph/badge.svg)](https://codecov.io/gh/YOUR_USERNAME/YOUR_REPO)
   ```

## Maintenance

- PAT tokens expire - set a calendar reminder to renew
- Keep the gist public for badges to work
- Update badge URLs if you rename the repository
- Review coverage thresholds periodically in workflow files

## Resources

- [Shields.io Documentation](https://shields.io/)
- [Dynamic Badges Action](https://github.com/schneegans/dynamic-badges-action)
- [GitHub Actions Badge Documentation](https://docs.github.com/en/actions/monitoring-and-troubleshooting-workflows/adding-a-workflow-status-badge)
- [Codecov Documentation](https://docs.codecov.com/)
