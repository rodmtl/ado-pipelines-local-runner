# Phase 1 MVP - QA Acceptance Criteria

<!-- markdownlint-disable MD007 MD013 MD024 MD031 MD032 MD060 -->

**Version:** 1.0
**Date:** December 12, 2025  
**Target Release:** Phase 1 MVP  
**Framework:** .NET 8.0  
**Platforms:** Windows, macOS, Linux

---

## Table of Contents

1. [Functional Requirements](#1-functional-requirements)
2. [Non-Functional Requirements](#2-non-functional-requirements)
3. [Test Scenarios Matrix](#3-test-scenarios-matrix)
4. [Exit Criteria](#4-exit-criteria)

---

## 1. Functional Requirements

### 1.1 YAML Validation

#### AC-F1.1: Basic YAML Syntax Validation

**Given** a valid Azure DevOps pipeline YAML file  
**When** the validation command is executed  
**Then** the system shall parse the file successfully  
**And** return a success status code (0)  
**And** output a confirmation message indicating valid syntax

**Acceptance Test:**
```bash
azp-local validate azure-pipelines.yml
# Expected: Exit code 0, Message: "✓ Syntax validation passed"
```

**Measurable Criteria:**
- Parsing completes within 100ms for files ≤ 10KB
- No false positive syntax errors
- Zero unhandled exceptions

---

#### AC-F1.2: Malformed YAML Detection

**Given** a YAML file with syntax errors (invalid indentation, missing colons, etc.)  
**When** the validation command is executed  
**Then** the system shall detect the syntax error  
**And** return an error status code (non-zero)  
**And** report the error with line number and column position  
**And** provide a descriptive error message

**Acceptance Test:**
```yaml
# Invalid YAML: Missing colon after 'trigger'
trigger
  - main

steps:
  - script: echo hello
```

```bash
azp-local validate invalid-pipeline.yml
# Expected: Exit code 1
# Output: "✗ Syntax error at line 2, column 3: Expected ':' after key"
```

**Measurable Criteria:**
- 100% detection rate for common YAML syntax errors
- Error location accurate to within ±1 line
- Error messages are actionable and clear

---

#### AC-F1.3: Empty and Whitespace-Only Files

**Given** an empty file or a file containing only whitespace  
**When** the validation command is executed  
**Then** the system shall report an error  
**And** indicate that the pipeline definition is empty  
**And** return an error status code

**Acceptance Test:**
```bash
echo "" > empty.yml
azp-local validate empty.yml
# Expected: Exit code 1, Message: "✗ Pipeline file is empty"
```

**Measurable Criteria:**
- Detects empty files (0 bytes)
- Detects whitespace-only files (spaces, tabs, newlines)
- Completes validation in < 50ms

---

#### AC-F1.4: Large File Handling

**Given** a valid YAML file exceeding size threshold (> 2MB)  
**When** the validation command is executed  
**Then** the system shall either:
  - Process the file successfully with a performance warning, OR
  - Reject the file with a clear size limit error message

**Acceptance Test:**
```bash
# Generate 3MB file
azp-local validate large-pipeline.yml
# Expected: Warning or rejection with clear message
```

**Measurable Criteria:**
- Defined size limit documented (default: 2MB)
- Clear error message if limit exceeded
- Configurable size limit via CLI option

---

### 1.2 Schema Validation

#### AC-F2.1: Valid Pipeline Structure

**Given** a YAML file with valid Azure DevOps pipeline schema structure  
**When** schema validation is performed  
**Then** the system shall validate all required properties exist  
**And** validate all property types are correct  
**And** return success status

**Acceptance Test:**
```yaml
# Valid minimal pipeline
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - script: echo "Hello World"
    displayName: 'Run script'
```

```bash
azp-local validate --schema pipeline.yml
# Expected: Exit code 0, Message: "✓ Schema validation passed"
```

**Measurable Criteria:**
- Validates all top-level properties (trigger, pool, stages, jobs, steps)
- Validates nested properties according to ADO schema
- Zero false negatives for valid pipelines

---

#### AC-F2.2: Invalid Property Types

**Given** a pipeline with properties of incorrect type  
**When** schema validation is performed  
**Then** the system shall detect the type mismatch  
**And** report the property path and expected type  
**And** return error status

**Acceptance Test:**
```yaml
# Invalid: displayName should be string, not number
steps:
  - script: echo test
    displayName: 123
```

```bash
azp-local validate --schema invalid-type.yml
# Expected: Exit code 1
# Output: "✗ Type error at 'steps[0].displayName': expected string, got number"
```

**Measurable Criteria:**
- Detects type mismatches for all primitive types (string, number, boolean)
- Detects type mismatches for complex types (arrays, objects)
- Error messages include property path

---

#### AC-F2.3: Unknown Properties Detection

**Given** a pipeline with properties not in the Azure DevOps schema  
**When** schema validation is performed with strict mode  
**Then** the system shall report unknown properties  
**And** suggest similar valid properties if available

**Acceptance Test:**
```yaml
# Invalid: 'vmImag' instead of 'vmImage'
pool:
  vmImag: 'ubuntu-latest'
```

```bash
azp-local validate --schema --strict pipeline.yml
# Expected: Exit code 1
# Output: "✗ Unknown property 'pool.vmImag'. Did you mean 'vmImage'?"
```

**Measurable Criteria:**
- Detects 100% of unknown properties in strict mode
- Provides suggestions for properties with edit distance ≤ 2
- Allows unknown properties in non-strict mode

---

#### AC-F2.4: Required Properties Validation

**Given** a pipeline missing required properties  
**When** schema validation is performed  
**Then** the system shall report all missing required properties  
**And** indicate where they are required  
**And** return error status

**Acceptance Test:**
```yaml
# Invalid: 'steps' is required but missing
trigger:
  - main
pool:
  vmImage: 'ubuntu-latest'
```

```bash
azp-local validate --schema missing-required.yml
# Expected: Exit code 1
# Output: "✗ Required property 'steps' is missing"
```

**Measurable Criteria:**
- Detects all required properties according to ADO schema
- Reports all missing properties in a single validation pass
- Clear indication of where property is required

---

### 1.3 Local Template Resolution

#### AC-F3.1: Relative Path Template Resolution

**Given** a pipeline with a template reference using relative path  
**When** template resolution is performed  
**Then** the system shall resolve the template file path  
**And** load the template content  
**And** validate the template syntax  
**And** return success status

**Acceptance Test:**
```yaml
# main.yml
steps:
  - template: templates/build-steps.yml

# templates/build-steps.yml
steps:
  - script: dotnet build
```

```bash
azp-local validate --resolve-templates main.yml
# Expected: Exit code 0, Message: "✓ All templates resolved successfully"
```

**Measurable Criteria:**
- Resolves relative paths from pipeline file directory
- Supports nested directory structures
- Handles both forward slash and backslash on Windows

---

#### AC-F3.2: Template Not Found Error

**Given** a pipeline referencing a non-existent template file  
**When** template resolution is performed  
**Then** the system shall report a "template not found" error  
**And** indicate the expected file path  
**And** indicate the source location of the template reference  
**And** return error status

**Acceptance Test:**
```yaml
steps:
  - template: templates/missing.yml
```

```bash
azp-local validate --resolve-templates pipeline.yml
# Expected: Exit code 1
# Output: "✗ Template not found at line 2: 'templates/missing.yml'"
```

**Measurable Criteria:**
- Error message includes expected file path
- Error message includes source line number
- Completes resolution check in < 200ms for pipelines with ≤ 10 templates

---

#### AC-F3.3: Circular Template Dependency Detection

**Given** templates with circular dependencies (A → B → A)  
**When** template resolution is performed  
**Then** the system shall detect the circular dependency  
**And** report the dependency chain  
**And** return error status

**Acceptance Test:**
```yaml
# template-a.yml
steps:
  - template: template-b.yml

# template-b.yml
steps:
  - template: template-a.yml
```

```bash
azp-local validate --resolve-templates template-a.yml
# Expected: Exit code 1
# Output: "✗ Circular template dependency: template-a.yml → template-b.yml → template-a.yml"
```

**Measurable Criteria:**
- Detects all circular dependencies regardless of depth
- Reports full dependency chain
- Prevents infinite recursion (timeout after 100 template traversals)

---

#### AC-F3.4: Template Parameter Passing

**Given** a template with parameters  
**When** the template is used with parameter values  
**Then** the system shall validate parameter names match template definition  
**And** validate parameter types if specified  
**And** apply default values for optional parameters  
**And** return success status

**Acceptance Test:**
```yaml
# main.yml
steps:
  - template: templates/parameterized.yml
    parameters:
      buildConfiguration: 'Release'
      runTests: true

# templates/parameterized.yml
parameters:
  - name: buildConfiguration
    type: string
    default: 'Debug'
  - name: runTests
    type: boolean
    default: false

steps:
  - script: dotnet build --configuration ${{ parameters.buildConfiguration }}
```

```bash
azp-local validate --resolve-templates main.yml
# Expected: Exit code 0
```

**Measurable Criteria:**
- Validates all parameter names
- Validates parameter types (string, number, boolean, object, step, stepList, job, jobList, deployment, deploymentList, stage, stageList)
- Applies default values correctly
- Reports errors for missing required parameters

---

### 1.4 Variable Processing

#### AC-F4.1: Simple Variable Substitution

**Given** a pipeline with variable definitions and references  
**When** variable processing is performed  
**Then** the system shall substitute all variable references with their values  
**And** preserve the data types  
**And** return success status

**Acceptance Test:**
```yaml
variables:
  solution: '**/*.sln'
  buildPlatform: 'Any CPU'

steps:
  - script: dotnet build $(solution) --platform "$(buildPlatform)"
```

```bash
azp-local validate --process-variables pipeline.yml
# Expected: Exit code 0
# Output should show expanded: dotnet build **/*.sln --platform "Any CPU"
```

**Measurable Criteria:**
- Substitutes all `$(variableName)` references
- Preserves special characters in values
- Handles empty string values

---

#### AC-F4.2: Undefined Variable Detection

**Given** a pipeline referencing undefined variables  
**When** variable processing is performed  
**Then** the system shall report all undefined variables  
**And** indicate the source location of each reference  
**And** return error status in strict mode

**Acceptance Test:**
```yaml
steps:
  - script: echo $(undefinedVar)
```

```bash
azp-local validate --process-variables --strict pipeline.yml
# Expected: Exit code 1
# Output: "✗ Undefined variable at line 2: 'undefinedVar'"
```

**Measurable Criteria:**
- Detects 100% of undefined variable references
- Reports all undefined variables in single pass
- Provides warning in non-strict mode, error in strict mode

---

#### AC-F4.3: Variable Scope Resolution

**Given** a pipeline with variables defined at multiple scopes (global, stage, job)  
**When** variable processing is performed  
**Then** the system shall resolve variables according to scope hierarchy  
**And** apply correct precedence (job > stage > global)  
**And** return success status

**Acceptance Test:**
```yaml
variables:
  environment: 'dev'  # Global

stages:
  - stage: Build
    variables:
      environment: 'staging'  # Stage scope
    jobs:
      - job: BuildJob
        variables:
          environment: 'production'  # Job scope
        steps:
          - script: echo $(environment)  # Should use 'production'
```

**Measurable Criteria:**
- Correct precedence: job > stage > global
- Validates scope boundaries
- No variable leakage between jobs

---

#### AC-F4.4: Expression Evaluation

**Given** a pipeline with variable expressions using `${{ }}` syntax  
**When** variable processing is performed  
**Then** the system shall evaluate compile-time expressions  
**And** substitute the evaluated results  
**And** return success status

**Acceptance Test:**
```yaml
variables:
  isDev: true
  environment: ${{ if eq(variables.isDev, true) }}
    value: 'development'
  ${{ else }}:
    value: 'production'

steps:
  - script: echo "Deploying to $(environment)"
```

**Measurable Criteria:**
- Evaluates conditional expressions (`if`, `else`)
- Evaluates functions (eq, ne, and, or, contains, startsWith, endsWith)
- Evaluates at compile time (before runtime variable substitution)

---

### 1.5 Error Reporting

#### AC-F5.1: Structured Error Output

**Given** a pipeline with multiple validation errors  
**When** validation is performed  
**Then** the system shall report all errors in structured format  
**And** include error severity (error, warning, info)  
**And** include error code for programmatic handling  
**And** include source location (file, line, column)  
**And** include descriptive message

**Acceptance Test:**
```bash
azp-local validate --format json pipeline.yml
```

Expected JSON output:
```json
{
  "success": false,
  "errors": [
    {
      "code": "YAML001",
      "severity": "error",
      "message": "Invalid YAML syntax",
      "file": "pipeline.yml",
      "line": 5,
      "column": 12,
      "suggestion": "Check indentation at this line"
    }
  ]
}
```

**Measurable Criteria:**
- JSON output is valid and parseable
- All required fields present in each error
- Error codes follow consistent naming convention

---

#### AC-F5.2: Multi-Format Output Support

**Given** a validation result  
**When** output format is specified  
**Then** the system shall support the following formats:
  - Text (human-readable, colored output)
  - JSON (machine-readable)
  - JUnit XML (CI/CD integration)
  - SARIF (security scanning tools)

**Acceptance Test:**
```bash
azp-local validate --format text pipeline.yml
azp-local validate --format json pipeline.yml
azp-local validate --format junit pipeline.yml
azp-local validate --format sarif pipeline.yml
```

**Measurable Criteria:**
- All formats produce valid output according to their specifications
- Text output uses ANSI colors when terminal supports it
- JSON output is valid and schema-compliant
- JUnit XML can be imported by standard CI/CD tools
- SARIF output conforms to SARIF 2.1.0 specification

---

#### AC-F5.3: Error Aggregation

**Given** a pipeline with errors in multiple files (main + templates)  
**When** validation is performed  
**Then** the system shall aggregate errors from all files  
**And** group errors by file  
**And** sort errors by file, then line number  
**And** provide error count summary

**Acceptance Test:**
```bash
azp-local validate --resolve-templates main.yml
# Expected output:
# Errors in main.yml:
#   Line 5: Invalid trigger syntax
# Errors in templates/build.yml:
#   Line 12: Unknown property 'vmImag'
# 
# Total: 2 errors in 2 files
```

**Measurable Criteria:**
- All errors from all files are reported
- Errors are grouped logically by file
- Summary includes total error count and file count
- Execution completes even when errors are found in multiple files

---

#### AC-F5.4: Error Suppression

**Given** a pipeline with validation errors  
**When** specific error codes are suppressed via configuration  
**Then** the system shall not report suppressed errors  
**And** shall still report non-suppressed errors  
**And** shall include a summary of suppressed warnings

**Acceptance Test:**
```yaml
# .azp-local.yml config
errorSuppression:
  - SCHEMA003  # Suppress unknown property warnings
```

```bash
azp-local validate --config .azp-local.yml pipeline.yml
# Expected: Suppressed errors not shown, others shown
# Summary: "2 errors, 3 warnings (1 warning suppressed)"
```

**Measurable Criteria:**
- Suppression by error code works correctly
- Suppression does not affect other validation
- Summary indicates number of suppressed errors/warnings

---

## 2. Non-Functional Requirements

### 2.1 Performance Targets

#### AC-P1.1: Validation Performance

**Requirement:** Validation operations shall complete within specified time limits

| Pipeline Size | File Count | Max Duration | Target Duration |
|--------------|------------|--------------|-----------------|
| Small (< 10KB) | 1 file | 200ms | 100ms |
| Medium (10-100KB) | 1-5 files | 1s | 500ms |
| Large (100KB-1MB) | 5-20 files | 5s | 2s |
| Extra Large (1-2MB) | 20-50 files | 15s | 8s |

**Acceptance Test:**
```bash
time azp-local validate small-pipeline.yml
# Expected: < 200ms

time azp-local validate --resolve-templates large-pipeline.yml
# Expected: < 5s
```

**Measurable Criteria:**
- 95th percentile of operations meet target duration
- 99th percentile of operations meet max duration
- Performance tests run on standard hardware (4 core CPU, 8GB RAM)

---

#### AC-P1.2: Memory Consumption

**Requirement:** Memory usage shall remain within acceptable limits

| Pipeline Size | Max Memory | Target Memory |
|--------------|-----------|---------------|
| Small | 50MB | 30MB |
| Medium | 150MB | 100MB |
| Large | 500MB | 300MB |
| Extra Large | 1GB | 700MB |

**Acceptance Test:**
```bash
# Use profiling tools to measure memory
dotnet-counters monitor --process-id <pid> --counters System.Runtime
```

**Measurable Criteria:**
- Peak memory usage does not exceed max thresholds
- No memory leaks (memory returns to baseline after validation)
- Garbage collection does not cause pauses > 100ms

---

#### AC-P1.3: Cold Start Performance

**Requirement:** First execution after installation shall not significantly impact performance

**Acceptance Test:**
```bash
# First run after installation
time azp-local validate pipeline.yml
# Expected: < 2x normal validation time
```

**Measurable Criteria:**
- Cold start adds < 1s to normal validation time
- Subsequent executions meet normal performance targets
- JIT compilation warming does not degrade UX

---

#### AC-P1.4: Concurrent Validation

**Requirement:** Tool shall support concurrent validation of multiple pipelines

**Acceptance Test:**
```bash
# Validate multiple pipelines in parallel
azp-local validate pipeline1.yml pipeline2.yml pipeline3.yml
```

**Measurable Criteria:**
- Concurrent validation of N files completes in ≤ 1.5x single file time
- No race conditions or data corruption
- Thread-safe operation verified by concurrency tests

---

### 2.2 Cross-Platform Support

#### AC-P2.1: Windows Support

**Requirement:** Full functionality on Windows 10/11 and Windows Server 2019+

**Platforms Tested:**
- Windows 10 (x64)
- Windows 11 (x64, ARM64)
- Windows Server 2019 (x64)
- Windows Server 2022 (x64)

**Acceptance Test:**
```powershell
# Windows PowerShell and PowerShell Core
azp-local validate azure-pipelines.yml
# Expected: Exit code 0, successful validation
```

**Measurable Criteria:**
- All functional tests pass on all Windows platforms
- Path handling works with backslashes
- Line endings (CRLF) handled correctly
- Works with Windows PowerShell 5.1 and PowerShell 7+

---

#### AC-P2.2: macOS Support

**Requirement:** Full functionality on macOS 11 (Big Sur) and later

**Platforms Tested:**
- macOS 11 (Big Sur) - x64
- macOS 12 (Monterey) - x64, ARM64 (M1)
- macOS 13 (Ventura) - ARM64 (M1/M2)
- macOS 14 (Sonoma) - ARM64 (M2/M3)

**Acceptance Test:**
```bash
# macOS Terminal and zsh
azp-local validate azure-pipelines.yml
# Expected: Exit code 0, successful validation
```

**Measurable Criteria:**
- All functional tests pass on all macOS platforms
- ARM64 (Apple Silicon) builds available and tested
- Path handling works with forward slashes
- Line endings (LF) handled correctly
- Works with bash and zsh shells

---

#### AC-P2.3: Linux Support

**Requirement:** Full functionality on major Linux distributions

**Distributions Tested:**
- Ubuntu 20.04, 22.04, 24.04 (x64, ARM64)
- Debian 11, 12 (x64)
- Fedora 38, 39 (x64)
- Alpine Linux 3.18+ (x64, for container scenarios)

**Acceptance Test:**
```bash
# Linux bash
azp-local validate azure-pipelines.yml
# Expected: Exit code 0, successful validation
```

**Measurable Criteria:**
- All functional tests pass on all tested distributions
- Self-contained build does not require additional dependencies
- Path handling works with forward slashes
- Line endings (LF) handled correctly
- Works with bash shell

---

#### AC-P2.4: Platform-Specific Path Handling

**Requirement:** Correctly handle file paths on all platforms

**Acceptance Test:**
```yaml
# Template paths should work on all platforms
steps:
  - template: templates/build.yml        # Relative path
  - template: ./ci/templates/test.yml   # Relative with ./
```

```bash
azp-local validate --resolve-templates pipeline.yml
# Expected: Works on Windows, macOS, Linux
```

**Measurable Criteria:**
- Supports both forward slash and backslash on Windows
- Supports forward slash on macOS and Linux
- Correctly normalizes paths across platforms
- Handles case-sensitive filesystems (Linux, macOS) vs case-insensitive (Windows)

---

#### AC-P2.5: Encoding Support

**Requirement:** Support files with different text encodings

**Encodings Tested:**
- UTF-8 (with and without BOM)
- UTF-16 LE
- UTF-16 BE
- ASCII

**Acceptance Test:**
```bash
# Files with different encodings
azp-local validate utf8-with-bom.yml
azp-local validate utf16-le.yml
# Expected: All parse correctly
```

**Measurable Criteria:**
- UTF-8 files (with or without BOM) parse correctly
- UTF-16 files parse correctly
- ASCII files parse correctly
- Invalid encoding results in clear error message

---

### 2.3 Reliability

#### AC-P3.1: Error Resilience

**Requirement:** Tool shall handle errors gracefully without crashes

**Acceptance Test:**
```bash
# Invalid inputs should not cause crashes
azp-local validate non-existent-file.yml
azp-local validate binary-file.exe
azp-local validate ""
```

**Measurable Criteria:**
- Zero unhandled exceptions in production code
- All errors result in proper error messages and exit codes
- No data corruption or file system changes on error
- Graceful degradation when resources unavailable

---

#### AC-P3.2: Deterministic Behavior

**Requirement:** Identical inputs shall produce identical outputs

**Acceptance Test:**
```bash
# Run validation multiple times
for i in {1..10}; do
  azp-local validate pipeline.yml > output-$i.txt
done
# Expected: All output files are identical
```

**Measurable Criteria:**
- Same input produces same output across runs
- Same input produces same output across platforms
- No timing-dependent behavior
- No non-deterministic operations (random, dates) in validation logic

---

#### AC-P3.3: Backward Compatibility

**Requirement:** CLI interface and configuration format shall remain compatible within major version

**Acceptance Test:**
```bash
# Commands from version 1.0 should work in version 1.x
azp-local validate --schema pipeline.yml
```

**Measurable Criteria:**
- All commands documented in v1.0 work in v1.x
- All configuration options from v1.0 supported in v1.x
- New features added in opt-in manner
- Breaking changes only in major version updates

---

### 2.4 Usability

#### AC-P4.1: Clear Error Messages

**Requirement:** Error messages shall be actionable and understandable

**Acceptance Test:**
```yaml
# Example error-prone pipeline
pool:
  vmImag: 'ubuntu-latest'  # Typo
```

```bash
azp-local validate pipeline.yml
# Expected: "Unknown property 'pool.vmImag'. Did you mean 'vmImage'?"
```

**Measurable Criteria:**
- Error messages use plain language
- Error messages indicate what's wrong and where
- Error messages suggest corrective action when possible
- Technical jargon minimized

---

#### AC-P4.2: Help Documentation

**Requirement:** Built-in help shall be comprehensive and accurate

**Acceptance Test:**
```bash
azp-local --help
azp-local validate --help
```

**Measurable Criteria:**
- `--help` available for all commands
- Help text includes usage examples
- Help text documents all options
- Help text is accurate and up-to-date

---

#### AC-P4.3: Progress Indication

**Requirement:** Long-running operations shall provide progress feedback

**Acceptance Test:**
```bash
azp-local validate --resolve-templates large-pipeline-with-many-templates.yml
# Expected: Progress indicator shows template resolution progress
```

**Measurable Criteria:**
- Operations > 2s show progress indicator
- Progress indicator shows current step
- Progress indicator can be disabled for CI/CD scenarios (--no-progress)

---

### 2.5 Installation and Distribution

#### AC-P5.1: Easy Installation

**Requirement:** Tool shall be easy to install on all platforms

**Distribution Methods:**
- .NET Tool (dotnet tool install)
- Standalone executable (self-contained)
- Package managers (Homebrew, Chocolatey, apt/yum)

**Acceptance Test:**
```bash
# .NET Tool
dotnet tool install --global azp-local

# Standalone
./install.sh

# Homebrew (macOS/Linux)
brew install azp-local

# Chocolatey (Windows)
choco install azp-local
```

**Measurable Criteria:**
- Installation completes in < 2 minutes
- No manual configuration required
- Tool available in PATH after installation
- Uninstallation completely removes all files

---

#### AC-P5.2: Version Management

**Requirement:** Users shall be able to check version and update tool

**Acceptance Test:**
```bash
azp-local --version
# Expected: "azp-local version 1.0.0"

azp-local update
# Expected: Checks for updates and installs if available
```

**Measurable Criteria:**
- Version displayed in semantic versioning format (x.y.z)
- Version info includes build date and commit hash
- Update command checks online registry
- Update command preserves user configuration

---

## 3. Test Scenarios Matrix

### 3.1 Functional Test Matrix

| Scenario ID | Category | Description | Priority | Automated | Platform Coverage |
|-------------|----------|-------------|----------|-----------|-------------------|
| TS-F-001 | YAML Validation | Valid simple pipeline | P0 | Yes | All |
| TS-F-002 | YAML Validation | Valid multi-stage pipeline | P0 | Yes | All |
| TS-F-003 | YAML Validation | Malformed YAML syntax | P0 | Yes | All |
| TS-F-004 | YAML Validation | Empty file | P1 | Yes | All |
| TS-F-005 | YAML Validation | Whitespace-only file | P1 | Yes | All |
| TS-F-006 | YAML Validation | Large file (> 2MB) | P2 | Yes | All |
| TS-F-007 | YAML Validation | Binary file | P2 | Yes | All |
| TS-F-008 | YAML Validation | Invalid UTF-8 encoding | P2 | Yes | All |
| TS-F-009 | Schema Validation | Valid schema compliance | P0 | Yes | All |
| TS-F-010 | Schema Validation | Invalid property type | P0 | Yes | All |
| TS-F-011 | Schema Validation | Unknown property | P1 | Yes | All |
| TS-F-012 | Schema Validation | Missing required property | P0 | Yes | All |
| TS-F-013 | Schema Validation | Extra properties (strict mode) | P1 | Yes | All |
| TS-F-014 | Template Resolution | Relative path template | P0 | Yes | All |
| TS-F-015 | Template Resolution | Nested template hierarchy | P0 | Yes | All |
| TS-F-016 | Template Resolution | Template not found | P0 | Yes | All |
| TS-F-017 | Template Resolution | Circular dependency | P1 | Yes | All |
| TS-F-018 | Template Resolution | Template with parameters | P0 | Yes | All |
| TS-F-019 | Template Resolution | Template parameter type validation | P1 | Yes | All |
| TS-F-020 | Template Resolution | Template parameter defaults | P1 | Yes | All |
| TS-F-021 | Variable Processing | Simple variable substitution | P0 | Yes | All |
| TS-F-022 | Variable Processing | Undefined variable (strict) | P0 | Yes | All |
| TS-F-023 | Variable Processing | Variable scope resolution | P1 | Yes | All |
| TS-F-024 | Variable Processing | Compile-time expression evaluation | P1 | Yes | All |
| TS-F-025 | Variable Processing | Nested variable references | P2 | Yes | All |
| TS-F-026 | Error Reporting | Text format output | P0 | Yes | All |
| TS-F-027 | Error Reporting | JSON format output | P0 | Yes | All |
| TS-F-028 | Error Reporting | JUnit XML format output | P1 | Yes | All |
| TS-F-029 | Error Reporting | SARIF format output | P2 | Yes | All |
| TS-F-030 | Error Reporting | Error aggregation | P1 | Yes | All |
| TS-F-031 | Error Reporting | Error suppression | P2 | Yes | All |
| TS-F-032 | CLI | Help command | P0 | Yes | All |
| TS-F-033 | CLI | Version command | P0 | Yes | All |
| TS-F-034 | CLI | Invalid command | P1 | Yes | All |
| TS-F-035 | CLI | Missing required argument | P1 | Yes | All |

**Priority Levels:**
- **P0:** Critical - Must pass for release
- **P1:** High - Should pass for release, blockers require justification
- **P2:** Medium - Nice to have, can defer to patch release

---

### 3.2 Non-Functional Test Matrix

| Scenario ID | Category | Description | Priority | Automated | Platform Coverage |
|-------------|----------|-------------|----------|-----------|-------------------|
| TS-NF-001 | Performance | Small file validation < 200ms | P0 | Yes | All |
| TS-NF-002 | Performance | Medium file validation < 1s | P0 | Yes | All |
| TS-NF-003 | Performance | Large file validation < 5s | P1 | Yes | All |
| TS-NF-004 | Performance | Memory usage - small pipeline | P0 | Yes | All |
| TS-NF-005 | Performance | Memory usage - large pipeline | P1 | Yes | All |
| TS-NF-006 | Performance | Cold start performance | P2 | Yes | All |
| TS-NF-007 | Performance | Concurrent validation | P2 | Yes | All |
| TS-NF-008 | Cross-Platform | Windows 10 compatibility | P0 | Yes | Windows |
| TS-NF-009 | Cross-Platform | Windows 11 compatibility | P0 | Yes | Windows |
| TS-NF-010 | Cross-Platform | macOS Intel compatibility | P0 | Yes | macOS |
| TS-NF-011 | Cross-Platform | macOS Apple Silicon compatibility | P0 | Yes | macOS |
| TS-NF-012 | Cross-Platform | Ubuntu 22.04 compatibility | P0 | Yes | Linux |
| TS-NF-013 | Cross-Platform | Ubuntu 24.04 compatibility | P0 | Yes | Linux |
| TS-NF-014 | Cross-Platform | Alpine Linux compatibility | P1 | Yes | Linux |
| TS-NF-015 | Cross-Platform | Path handling - Windows backslash | P0 | Yes | Windows |
| TS-NF-016 | Cross-Platform | Path handling - Unix forward slash | P0 | Yes | macOS, Linux |
| TS-NF-017 | Cross-Platform | UTF-8 encoding support | P0 | Yes | All |
| TS-NF-018 | Cross-Platform | UTF-16 encoding support | P1 | Yes | All |
| TS-NF-019 | Reliability | No unhandled exceptions | P0 | Yes | All |
| TS-NF-020 | Reliability | Deterministic output | P0 | Yes | All |
| TS-NF-021 | Reliability | Graceful error handling | P0 | Yes | All |
| TS-NF-022 | Usability | Clear error messages | P0 | Manual | All |
| TS-NF-023 | Usability | Help documentation | P0 | Yes | All |
| TS-NF-024 | Usability | Progress indication | P1 | Manual | All |
| TS-NF-025 | Installation | .NET Tool installation | P0 | Manual | All |
| TS-NF-026 | Installation | Standalone installation | P1 | Manual | All |
| TS-NF-027 | Installation | Package manager installation | P2 | Manual | Various |

---

### 3.3 Integration Test Scenarios

| Scenario ID | Description | Input | Expected Output | Priority |
| --- | --- | --- | --- | --- |
| TS-I-001 | End-to-end simple | Simple YAML | Exit 0 | P0 |
| TS-I-002 | With templates | Main + 2 templates | Exit 0 | P0 |
| TS-I-003 | Multiple errors | Multiple issues | Exit 1 | P0 |
| TS-I-004 | Full expansion | Templates + vars | Expanded | P1 |
| TS-I-005 | CI/CD integration | Auto validation | JUnit XML | P1 |
| TS-I-006 | Config file loading | With config | Applied | P1 |
| TS-I-007 | Multi-file batch | 10 pipelines | Summary | P2 |

---

### 3.4 Edge Case and Stress Test Scenarios

| Scenario ID | Description | Input | Behavior | Priority |
| --- | --- | --- | --- | --- |
| TS-E-001 | Empty file | 0 bytes | Error | P0 |
| TS-E-002 | Only comments | Comments | Error | P1 |
| TS-E-003 | Max nesting | 20 levels | Validate | P2 |
| TS-E-004 | Max file size | 5MB file | Error | P2 |
| TS-E-005 | Many templates | 100 templates | Acceptable | P2 |
| TS-E-006 | Special chars | Unicode, spaces | Resolved | P1 |
| TS-E-007 | Symlink | Via symlink | Follow | P2 |
| TS-E-008 | Read-only FS | No write | Works | P1 |
| TS-E-009 | Concurrent | Multiple procs | Succeeds | P2 |
| TS-E-010 | Locale | Non-English | Correct | P2 |

---

## 4. Exit Criteria

### 4.1 Functional Completeness

#### Must Have (Release Blockers)

- [ ] **All P0 functional test scenarios pass** on all platforms (Windows, macOS, Linux)
- [ ] **YAML parsing** works for all valid Azure DevOps pipeline formats
- [ ] **Schema validation** detects all schema violations with < 5% false positive rate
- [ ] **Template resolution** works for relative paths with circular dependency detection
- [ ] **Variable processing** handles simple variable substitution correctly
- [ ] **Error reporting** provides text and JSON output formats
- [ ] **CLI commands** (validate, --help, --version) work as documented
- [ ] **Zero P0 bugs** in issue tracker

#### Should Have (High Priority)

- [ ] **All P1 functional test scenarios pass** on primary platforms (Windows 11, macOS 13+, Ubuntu 22.04+)
- [ ] **Template parameters** validated with type checking
- [ ] **Variable scope resolution** works correctly (global, stage, job)
- [ ] **JUnit XML output** format supported
- [ ] **Strict mode** for validation available
- [ ] **Configuration file** (.azp-local.yml) loading works
- [ ] **≤ 3 P1 bugs** in issue tracker

---

### 4.2 Performance Completeness

#### Must Have (Release Blockers)

- [ ] **Small pipeline validation** (< 10KB) completes in < 200ms (95th percentile)
- [ ] **Medium pipeline validation** (10-100KB) completes in < 1s (95th percentile)
- [ ] **Memory consumption** < 150MB for medium pipelines
- [ ] **No memory leaks** detected in 1-hour soak test
- [ ] **Performance tests pass** on reference hardware (4 core, 8GB RAM)

#### Should Have (High Priority)

- [ ] **Large pipeline validation** (100KB-1MB) completes in < 5s (95th percentile)
- [ ] **Cold start** adds < 1s overhead
- [ ] **Concurrent validation** of 3 pipelines completes in < 2x single pipeline time

---

### 4.3 Cross-Platform Completeness

#### Must Have (Release Blockers)

- [ ] **Windows 10/11** (x64): All P0 tests pass
- [ ] **macOS 12+** (ARM64 - Apple Silicon): All P0 tests pass
- [ ] **Ubuntu 22.04** (x64): All P0 tests pass
- [ ] **Path handling** works correctly on all platforms (forward slash, backslash on Windows)
- [ ] **Line ending handling** works correctly (CRLF on Windows, LF on Unix)
- [ ] **Self-contained builds** available for all platforms (no runtime dependencies)

#### Should Have (High Priority)

- [ ] **Ubuntu 24.04** (x64): All P0 tests pass
- [ ] **macOS 13+** (ARM64): All P1 tests pass
- [ ] **Alpine Linux** (for containers): Basic validation works
- [ ] **UTF-16 encoding** supported on all platforms

---

### 4.4 Quality Completeness

#### Must Have (Release Blockers)

- [ ] **Unit test coverage** ≥ 85% overall
- [ ] **Critical components coverage** ≥ 90% (SyntaxValidator, SchemaManager)
- [ ] **Zero critical security vulnerabilities** (from dependency scanning)
- [ ] **Zero high-severity security vulnerabilities** (from dependency scanning)
- [ ] **Static analysis** passes with zero P0/P1 issues
- [ ] **All integration tests pass** (end-to-end scenarios)
- [ ] **No known crashes** or unhandled exceptions

#### Should Have (High Priority)

- [ ] **Branch coverage** ≥ 80%
- [ ] **Zero medium-severity security vulnerabilities** (from dependency scanning)
- [ ] **Code review completed** for all components
- [ ] **Performance profiling completed** with no identified bottlenecks

---

### 4.5 Documentation Completeness

#### Must Have (Release Blockers)

- [ ] **README.md** with installation instructions, quick start, and examples
- [ ] **CLI help text** accurate and complete for all commands
- [ ] **Error code documentation** listing all error codes and meanings
- [ ] **Architecture documentation** (this document and related specs)
- [ ] **Known limitations** documented

#### Should Have (High Priority)

- [ ] **User guide** with common scenarios and troubleshooting
- [ ] **API documentation** for library usage (if exposed)
- [ ] **Contributing guide** for open source contributors
- [ ] **Changelog** documenting all changes from initial release

---

### 4.6 Release Readiness

#### Must Have (Release Blockers)

- [ ] **Version number finalized** (following semantic versioning)
- [ ] **Build pipeline** creates artifacts for all platforms
- [ ] **Installation tested** via .NET tool on all platforms
- [ ] **License file** included (MIT or Apache 2.0)
- [ ] **Security scan** completed and vulnerabilities addressed
- [ ] **Release notes** drafted
- [ ] **Rollback plan** documented

#### Should Have (High Priority)

- [ ] **Package manager submissions** prepared (NuGet, Homebrew, Chocolatey)
- [ ] **GitHub release** with binaries and release notes
- [ ] **Announcement blog post** drafted
- [ ] **Demo video** or GIF showing basic usage

---

### 4.7 Defect Thresholds

| Severity | Open Bugs | Description |
| --- | --- | --- |
| P0 - Critical | 0 | Crashes, data loss, security |
| P1 - High | ≤ 3 | Major impairment |
| P2 - Medium | ≤ 10 | Functionality impaired |
| P3 - Low | Unlimited | Minor issues |

**Additional Requirements:**

- No increase in P0/P1 bugs for 5 consecutive days before release
- All P0 bugs must have root cause analysis documented
- All P1 bugs must have workaround documented if not fixed

---

### 4.8 Sign-Off Requirements

**Required Approvals:**

1. **Engineering Lead** - Technical implementation meets architecture standards
2. **QA Lead** - All test scenarios executed and documented
3. **Product Owner** - Feature set meets MVP requirements
4. **Security Lead** - Security review completed, vulnerabilities addressed
5. **DevOps Lead** - Build and release pipeline validated

**Sign-Off Criteria:**

- [ ] All "Must Have" exit criteria met
- [ ] Risk assessment completed for any "Should Have" criteria not met
- [ ] Release notes reviewed and approved
- [ ] Support escalation plan in place
- [ ] Post-release monitoring plan defined

---

## 5. Measurement and Metrics

### 5.1 Test Execution Metrics

| Metric | Target | Measurement Method |
| --- | --- | --- |
| Test Pass Rate | ≥ 98% | Passed / Total × 100 |
| Test Execution Time | < 5 minutes | CI/CD duration |
| Flaky Test Rate | < 2% | Intermittent fails |
| Code Coverage | ≥ 85% | Coverlet |
| Branch Coverage | ≥ 80% | Coverlet |

---

### 5.2 Quality Metrics

| Metric | Target | Measurement Method |
| --- | --- | --- |
| Defect Density | < 2 per KLOC | Defects / (LOC / 1000) |
| Defect Escape Rate | < 5% | Defects in prod / Total |
| MTTR | < 2 days (P0/P1) | Avg time to close P0/P1 |
| Technical Debt Ratio | < 5% | SonarQube assessment |

---

### 5.3 Performance Metrics

| Metric | Target | Measurement Method |
| --- | --- | --- |
| Validation Throughput | ≥ 100 files/min | Small pipelines/min |
| P95 Response Time | < 200ms (small) | 95th percentile |
| P99 Response Time | < 500ms (small) | 99th percentile |
| Memory Efficiency | < 150MB (medium) | Peak memory usage |
| CPU Utilization | < 80% (single core) | Avg CPU usage |

---

### 5.4 Usability Metrics

| Metric | Target | Measurement Method |
| --- | --- | --- |
| Installation Success Rate | ≥ 95% | Success / Total attempts |
| First-Time Success Rate | ≥ 80% | First try success |
| Error Message Clarity | ≥ 4.0/5.0 | User survey rating |
| Documentation Complete | ≥ 90% | Answers / Questions |

---

## 6. Test Data Requirements

### 6.1 Valid Pipeline Samples

- [ ] **Simple pipeline** (single job, few steps)
- [ ] **Multi-stage pipeline** (dev → staging → production)
- [ ] **Pipeline with templates** (build template, test template, deploy template)
- [ ] **Pipeline with variables** (global, stage-scoped, job-scoped)
- [ ] **Pipeline with parameters** (string, number, boolean, object types)
- [ ] **Pipeline with resources** (repositories, pipelines, containers)
- [ ] **Complex real-world pipeline** (from actual ADO project)

### 6.2 Invalid Pipeline Samples

- [ ] **Syntax errors** (indentation, missing colons, invalid characters)
- [ ] **Schema violations** (wrong types, unknown properties, missing required)
- [ ] **Template errors** (not found, circular dependency, invalid parameters)
- [ ] **Variable errors** (undefined variables, invalid references)
- [ ] **Multiple simultaneous errors** (combination of above)

### 6.3 Edge Case Samples

- [ ] **Empty files**
- [ ] **Very large files** (2MB+)
- [ ] **Deeply nested structures** (20+ levels)
- [ ] **Many templates** (100+ template references)
- [ ] **Special characters** (Unicode, emojis, symbols)
- [ ] **Various encodings** (UTF-8, UTF-16, with/without BOM)

---

## Appendix A: Test Execution Schedule

### Week 1-2: Unit Testing

- Component-level tests (YamlParser, SyntaxValidator, etc.)
- Mock-based testing
- Coverage measurement

### Week 3: Integration Testing

- Component interaction tests
- End-to-end validation flows
- Template resolution scenarios

### Week 4: Cross-Platform Testing

- Windows validation (10, 11, Server)
- macOS validation (Intel, Apple Silicon)
- Linux validation (Ubuntu, Debian, Alpine)

### Week 5: Performance & Stress Testing

- Performance benchmarks
- Memory profiling
- Stress tests (large files, many templates)

### Week 6: User Acceptance Testing

- Beta testing with select users
- Usability feedback
- Documentation review

### Week 7: Regression & Hardening

- Re-test all P0/P1 scenarios
- Fix remaining defects
- Final sign-off

---

## Appendix B: Traceability Matrix

| Requirement ID | Criteria | Test Scenario(s) | Method |
| --- | --- | --- | --- |
| REQ-F-001 | AC-F1.1 - AC-F1.4 | TS-F-001 to TS-F-008 | Auto tests |
| REQ-F-002 | AC-F2.1 - AC-F2.4 | TS-F-009 to TS-F-013 | Auto tests |
| REQ-F-003 | AC-F3.1 - AC-F3.4 | TS-F-014 to TS-F-020 | Auto tests |
| REQ-F-004 | AC-F4.1 - AC-F4.4 | TS-F-021 to TS-F-025 | Auto tests |
| REQ-F-005 | AC-F5.1 - AC-F5.4 | TS-F-026 to TS-F-031 | Auto/manual |
| REQ-P-001 | AC-P1.1 - AC-P1.4 | TS-NF-001 to TS-NF-007 | Perf tests |
| REQ-P-002 | AC-P2.1 - AC-P2.5 | TS-NF-008 to TS-NF-018 | Platform |
| REQ-P-003 | AC-P3.1 - AC-P3.3 | TS-NF-019 to TS-NF-021 | Auto tests |
| REQ-P-004 | AC-P4.1 - AC-P4.3 | TS-NF-022 to TS-NF-024 | Manual |
| REQ-P-005 | AC-P5.1 - AC-P5.2 | TS-NF-025 to TS-NF-027 | Manual |

---

## Document Control

| Version | Date | Author | Changes |
| --- | --- | --- | --- |
| 1.0 | 2025-12-12 | QA Team | Initial Phase 1 MVP criteria |

**Review History:**

| Reviewer | Role | Date | Status |
| --- | --- | --- | --- |
| [Name] | Engineering Lead | TBD | Pending |
| [Name] | QA Lead | TBD | Pending |
| [Name] | Product Owner | TBD | Pending |
| [Name] | Security Lead | TBD | Pending |

---

## End of Document
