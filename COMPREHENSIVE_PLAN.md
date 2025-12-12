# Azure DevOps YAML Pipeline Local Validator & Debugger - Comprehensive Plan

**Document Version:** 1.0  
**Created:** December 12, 2025  
**Status:** Approved for Implementation  

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Technical Architecture](#technical-architecture)
3. [CLI design](#cli-design)
4. [Risk Analysis & Mitigation](#risk-analysis--mitigation)
5. [Implementation Roadmap](#implementation-roadmap)
6. [Success Criteria](#success-criteria)

---

## Executive Summary

This document outlines the comprehensive plan for developing **azp-local** – a local CLI validator and debugger tool for Azure DevOps YAML pipelines. The tool eliminates the need for continuous push/run/debug cycles by enabling developers to validate, expand, and simulate pipeline execution on their local machines before pushing to the remote repository.

### Key Objectives

- **Reduce lead time** for pipeline development and debugging
- **Improve developer experience** with fast local validation and execution simulation
- **Support complex pipelines** with templates, variables, and service connections
- **Cross-platform compatibility** (Windows, macOS, Linux)
- **Extensible architecture** for custom task implementations

### Target Audience

- Pipeline developers and engineers
- DevOps teams managing CI/CD infrastructure
- Release managers and build engineers

---

## Technical Architecture

### System Overview

```md
┌─────────────────────────────────────────────────────────────────┐
│                         CLI Interface                            │
│              (validate, expand, exec, lint, diag)                │
└──────────────────┬──────────────────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
┌───────▼────────┐   ┌───────▼────────┐
│  YAML Parser   │   │  Schema        │
│  & Validator   │   │  Manager       │
└───────┬────────┘   └───────┬────────┘
        │                     │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │  Template Resolver  │
        │  (HTTP/File)        │
        └─────────┬────────────┘
                  │
        ┌─────────▼──────────┐
        │  Variable          │
        │  Processor         │
        │  (Substitution)    │
        └─────────┬──────────┘
                  │
        ┌─────────▼──────────┐
        │  Execution Engine   │
        │  (Graph + Sim)      │
        └─────────┬──────────┘
                  │
        ┌─────────▼──────────┐
        │  Mock Services      │
        │  (VGs, SCs, etc)    │
        └─────────┬──────────┘
                  │
        ┌─────────▼──────────┐
        │  Shell Executor    │
        │  (PowerShell/Bash) │
        └────────────────────┘
```

### Core System Components

#### 1. **CLI Executor**

- Entry point for all commands
- Argument parsing and validation
- Configuration file loading
- Command routing and dispatch
- Output formatting and reporting

#### 2. **YAML Parser & Validator**

- Syntactic validation using YamlDotNet
- Schema-based validation against Azure DevOps pipeline schema
- Error reporting with line/column references
- Support for multiple YAML versions

#### 3. **Schema Manager**

- Fetches and caches Azure DevOps pipeline schema
- Version management (auto-detect or explicit specification)
- Local schema fallback for offline mode
- ETag-based cache invalidation

#### 4. **Template Resolver**

- Resolves template references (local files and HTTP URLs)
- Handles nested/recursive template inclusion
- Manages template parameters
- Cache optimization with Last-Modified headers

#### 5. **Variable Processor**

- Processes variable files (YAML/JSON)
- Inline variable definitions
- Hierarchical scope resolution (global → stage → job → step)
- Collision detection and warnings
- Circular dependency detection

#### 6. **Execution Engine**

- Builds dependency graph (stages → jobs → steps)
- Validates conditions and gates
- Detects cycles and circular dependencies
- Simulates execution with respecting job dependencies
- Handles matrix parameters

#### 7. **Mock Services Layer**

- **Variable Groups** - mock variable group retrieval
- **Service Connections** - simulate service connection configurations
- **Agent Pools** - mock agent specification and capabilities
- **Environments** - simulate environment configuration

#### 8. **Shell Executor**

- Abstraction layer for platform-specific shell execution
- Supports PowerShell, Bash, and CMD
- Path normalization and encoding handling
- Process timeout and signal handling
- Output capture and streaming

#### 9. **Task Plugin System**

- Extensible task registry
- Built-in tasks: Script, PowerShell, Bash, CmdLine
- Plugin interface for custom tasks
- Task validation and error handling

#### 10. **Diagnostic & Logging**

- Structured logging (Serilog)
- Multiple verbosity levels (quiet, minimal, normal, detailed, diagnostic)
- Timestamped entries with correlation IDs
- Performance metrics and profiling data
- Diagnostic bundle generation

#### 11. **Cache Manager**

- Schema caching (24-hour TTL)
- Template caching (1-hour TTL)
- Variable resolution cache
- Configurable storage location
- Cache invalidation strategies

### Core Modules

#### Module: YAML Processing

- **YamlDotNet**: YAML parsing and generation
- **JsonSchema.Net**: JSON schema validation
- **AnchorResolver**: Manage YAML anchors and aliases
- **CommentPreserver**: Optional comment preservation during expansion

#### Module: Remote Resolution

- **HttpClient** with configurable timeouts and retries
- **Proxy support** for corporate environments
- **ETag/Last-Modified** cache validation
- **Circuit breaker** pattern for fallback

#### Module: Variable Substitution

- **Expression parser** for `$(var)` and `${{ variables.var }}` syntax
- **Scope hierarchy** enforcement
- **Default value** handling
- **Secret masking** for output

#### Module: Execution Simulation

- **Dependency graph** builder (DAG)
- **Condition evaluator** (if conditions, stages, jobs)
- **Parallel execution** simulator with constraints
- **Step execution** with error handling

#### Module: Error Handling & Reporting

- **Severity levels**: Error, Warning, Info, Debug
- **Error codes** for categorization
- **Remediation hints** for common issues
- **SARIF export** for integration with CI/CD tools

### Technology Stack

| Component | Recommendation | Rationale |
|-----------|---|---|
| **Runtime** | .NET 8 / C# | Cross-platform, performance, mature tooling |
| **YAML Parsing** | YamlDotNet | Feature-complete, well-maintained |
| **Schema Validation** | JsonSchema.Net | Robust, spec-compliant |
| **Logging** | Serilog | Structured logging, multiple sinks |
| **CLI Framework** | System.CommandLine | Modern, intuitive command structure |
| **HTTP Client** | HttpClientFactory | Built-in, efficient connection pooling |
| **JSON Serialization** | System.Text.Json | Modern, fast, built-in |
| **Testing** | xUnit + Moq | Standard .NET testing stack |

### Data Flow Diagrams

#### Validation Flow

```md
Input YAML
    │
    ▼
Syntax Check (YamlDotNet)
    │
    ├─ FAIL → Report syntax errors → END
    │
    ▼
Schema Validation
    │
    ├─ FAIL → Report schema violations → END
    │
    ▼
Template Resolution (fetch + parse)
    │
    ├─ FAIL → Report missing/invalid templates → END
    │
    ▼
Variable Processing (scope + substitution)
    │
    ├─ FAIL → Report missing/circular vars → END
    │
    ▼
Graph Validation (dependencies, cycles)
    │
    ├─ FAIL → Report execution issues → END
    │
    ▼
SUCCESS → Report valid pipeline
```

#### Template Resolution Flow

```md
Template Reference (URL or file)
    │
    ├─ Local file? → Load from disk → Parse
    │
    └─ HTTP URL?
        │
        ├─ Check cache (ETag/Last-Modified valid?)
        │  ├─ HIT → Return cached → Parse
        │  └─ MISS or expired
        │      │
        │      ▼
        │  Fetch with retry logic (exponential backoff)
        │      │
        │      ├─ Success → Cache → Parse
        │      │
        │      └─ Fail → Fallback (offline mode or error)
        │
    └─ Parse → Validate → Check for nested templates
        │
        ▼
    Recursively resolve nested templates
```

#### Variable Processing Flow

```md
Variable Sources (files, inline, mocks)
    │
    ├─ Load from files
    ├─ Load inline definitions
    └─ Load from mock variable groups
    │
    ▼
Merge with scope resolution:
  Global ← Stage ← Job ← Step
    │
    ├─ Detect collisions → Warn/Error
    │
    └─ Build scope map
    │
    ▼
Substitute in pipeline:
  1. Find all $(var) and ${{ variables.var }}
  2. Resolve in scope hierarchy
  3. Apply defaults if not found
  4. Detect circular refs
    │
    ▼
Mask secrets in output
    │
    ▼
Return expanded pipeline with variable map
```

#### Execution Flow

```md
Expanded Pipeline
    │
    ▼
Build Dependency Graph
  ├─ Validate stage order
  ├─ Validate job dependencies
  └─ Detect cycles
    │
    ▼
Evaluate Conditions (if: conditions)
    │
    ├─ Remove conditional stages/jobs/steps
    │
    └─ Flag as skipped
    │
    ▼
Simulate Parallel Execution
  1. Execute runnable stages in order
  2. Within stage: execute jobs respecting dependencies
  3. Within job: execute steps in sequence
    │
    ▼
For each step:
  1. Prepare shell environment
  2. Mock services available
  3. Execute (or dry-run)
  4. Capture output/errors
  5. Apply continue-on-error
    │
    ▼
Collect Results
  ├─ Per-stage status
  ├─ Per-job status
  ├─ Per-step output
  └─ Timing metrics
    │
    ▼
Generate Report
  └─ Text/JSON/SARIF
```

### Integration Points

| Component A | Component B | Contract | Direction |
|---|---|---|---|
| CLI | YAML Parser | Pipeline YAML bytes | Request-Response |
| YAML Parser | Schema Manager | Schema version ID | Request-Response |
| Template Resolver | YAML Parser | Template YAML bytes | Request-Response |
| Variable Processor | Template Resolver | Resolved templates | Dependency |
| Execution Engine | Variable Processor | Variable map | Dependency |
| Shell Executor | Execution Engine | Step execution results | Callback |
| Diagnostic | All Components | Events/Logs | Event Stream |

### Mock Services Architecture

#### Variable Groups

```yaml
mockServices:
  variableGroups:
    - name: "common-vars"
      scope: "Release"  # Release or Build
      values:
        buildNumber: "12345"
        environment: "dev"
    - name: "secrets"
      values:
        apiKey: "***secret***"
```

#### Service Connections

```yaml
mockServices:
  serviceConnections:
    - name: "azure-rm"
      type: "azureRM"
      subscriptionId: "12345..."
      tenantId: "abcde..."
    - name: "github"
      type: "github"
      endpoint: "https://github.com"
```

#### Agent Pools

```yaml
mockServices:
  agentPools:
    - name: "Azure Pipelines"
      vmImages:
        - "ubuntu-latest"
        - "windows-latest"
        - "macos-latest"
```

#### Environments

```yaml
mockServices:
  environments:
    - name: "Production"
      approvers: ["user1@example.com"]
      checks: []
```

### Extensibility Architecture

#### Task Plugin System

**Interface:**

```csharp
public interface ITask
{
    string Name { get; }
    Version Version { get; }
    Task<TaskResult> ExecuteAsync(TaskInput input, CancellationToken ct);
}
```

**Built-in Tasks:**

- `script` - Execute shell script
- `powershell` - PowerShell script
- `bash` - Bash script
- `cmd` - CMD command

**Custom Task Example:**

```csharp
public class CustomDeployTask : ITask
{
    public string Name => "customDeploy";
    public Version Version => new(1, 0);
    
    public async Task<TaskResult> ExecuteAsync(
        TaskInput input,
        CancellationToken ct)
    {
        // Custom logic
    }
}
```

**Plugin Discovery:**

- Directory scanning: `./plugins/*.dll`
- Assembly loading with isolation
- Registration in task registry
- Error handling for missing/broken plugins

---

## CLI Design

### Command Surface

#### `azp-local validate`

Syntax and schema validation with template/variable resolution.

```bash
azp-local validate \
  --pipeline azure-pipelines.yml \
  --vars vars/common.yml \
  --output table \
  --strict
```

**Options:**

- `--no-template`: Skip template fetching (syntax only)
- `--no-schema`: Skip schema validation
- `--report <path>`: Save report to file

#### `azp-local expand`

Resolve templates/variables and emit fully expanded YAML.

```bash
azp-local expand \
  --pipeline pipelines/ci.yml \
  --vars vars/common.yml \
  --emit out/ci.expanded.yml
```

**Options:**

- `--include-comments`: Preserve comments
- `--preserve-lines`: Keep line references
- `--emit <path>`: Output file path

#### `azp-local exec`

Local execution simulator with stages/jobs/steps simulation.

```bash
azp-local exec \
  --pipeline azure-pipelines.yml \
  --stage build \
  --mock-services mocks.yml \
  --dry-run
```

**Options:**

- `--stage <name...>`: Filter by stage(s)
- `--job <name...>`: Filter by job(s)
- `--step <name...>`: Filter by step(s)
- `--continue-on-error`: Continue after step failures
- `--env <KEY=VAL...>`: Environment variables
- `--agent <spec>`: Agent specification
- `--dry-run`: Plan only, no execution
- `--artifacts <path>`: Artifacts directory

#### `azp-local lint`

Opinionated rules for code quality.

```bash
azp-local lint \
  --pipeline . \
  --ruleset default \
  --output sarif \
  --report reports/lint.sarif
```

**Options:**

- `--ruleset <built-in|file>`: Rule set to apply
- `--fail-level <error|warn>`: Fail on warning?
- `--baseline <file>`: Comparison baseline

#### `azp-local diag`

Collect diagnostics bundle.

```bash
azp-local diag \
  --pipeline . \
  --include-system \
  --bundle diagnostics.zip
```

**Options:**

- `--include-cache`: Include cache contents
- `--include-system`: Include system info
- `--bundle <path>`: Output bundle path

#### `azp-local schema pull`

Fetch/cache Azure DevOps schema.

```bash
azp-local schema pull --version 2024-10-01 --force
```

#### `azp-local config init`

Scaffold configuration file.

```bash
azp-local config init --template full
```

#### `azp-local cache clean`

Clear caches.

```bash
azp-local cache clean --what all
```

### Configuration File Format

**File:** `azp-local.config.yaml`

```yaml
pipeline:
  path: "azure-pipelines.yml"
  templateBase: "./templates"
  schemaVersion: "auto"

variables:
  files:
    - "vars/common.yml"
    - "vars/${ENVIRONMENT}.yml"
  inline:
    buildConfiguration: "Release"
    targetPlatform: "x64"

mockServices:
  variableGroups:
    - name: "vg-common"
      values:
        buildNumber: "$(Build.BuildNumber)"
  serviceConnections:
    - name: "sc-azure"
      type: "azureRM"
      endpoint: "https://example.com"
  agentPools:
    - name: "Default"
      vmImage: "ubuntu-latest"
  environments:
    - name: "Test"
      checks: []

execution:
  agent: "ubuntu-latest"
  timeout: "30m"
  parallel: 4
  continueOnError: false

lint:
  ruleset: "default"
  failLevel: "error"
  baseline: ".azp-lint-baseline.json"

output:
  format: "table"
  reportPath: "reports/validate.json"
  color: true

cache:
  schemaTtl: "24h"
  templateTtl: "1h"
  location: ".azp-local/cache"

telemetry:
  enabled: false
  level: "error"
```

### Global Options

| Option | Default | Description |
|--------|---------|---|
| `--pipeline <file\|dir>` | Required | Pipeline file or directory |
| `--cwd <path>` | `.` | Working directory |
| `--config <file>` | `azp-local.config.yaml` | Configuration file |
| `--schema-version <ver\|auto>` | `auto` | Schema version |
| `--vars <file...>` | | Variable files |
| `--var <key=value...>` | | Inline variables |
| `--template-base <url\|path>` | | Base URL/path for templates |
| `--offline` | false | Work offline (use cache) |
| `--strict` | false | Fail on warnings |
| `--verbosity <level>` | `normal` | Log verbosity |
| `--output <format>` | `text` | Output format |
| `--no-color` | false | Disable colored output |
| `--log-file <path>` | | Write logs to file |

### Output Formats

#### Text/Table

Human-readable summary with status indicators.

```md
Azure DevOps Pipeline Validator v1.0

Pipeline: azure-pipelines.yml
Status: ✓ VALID

Validation Summary
  Syntax:      ✓ Valid
  Schema:      ✓ Valid
  Templates:   ✓ Resolved (3 imports)
  Variables:   ✓ All resolved
  Graph:       ✓ No cycles detected

Details
  Stages:      3
  Jobs:        7
  Steps:       24

Timing
  Parse:       45ms
  Validate:    123ms
  Templates:   234ms
  Total:       402ms
```

#### JSON

Machine-friendly output.

```json
{
  "status": "valid",
  "summary": {
    "totalStages": 3,
    "totalJobs": 7,
    "totalSteps": 24
  },
  "validation": {
    "syntax": { "valid": true },
    "schema": { "valid": true },
    "templates": { "valid": true, "count": 3 },
    "variables": { "valid": true }
  },
  "issues": [],
  "timing": {
    "parse": 45,
    "validate": 123,
    "templates": 234,
    "total": 402
  }
}
```

#### SARIF (for lint)

OASIS Static Analysis Results Format.

```json
{
  "$schema": "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
  "version": "2.1.0",
  "runs": [
    {
      "tool": { "driver": { "name": "azp-local-lint" } },
      "results": [
        {
          "ruleId": "ADO001",
          "message": { "text": "Trigger not specified" },
          "level": "warning",
          "locations": [
            {
              "physicalLocation": {
                "artifactLocation": { "uri": "azure-pipelines.yml" },
                "region": { "startLine": 1 }
              }
            }
          ]
        }
      ]
    }
  ]
}
```

### Error Handling

**Structured Error Format:**

```json
{
  "code": "TEMPLATE_NOT_FOUND",
  "message": "Template file not found: templates/build.yml",
  "severity": "error",
  "location": {
    "file": "azure-pipelines.yml",
    "line": 42,
    "column": 5
  },
  "hint": "Check that the template file exists in the specified location",
  "docsUrl": "https://docs.example.com/errors/TEMPLATE_NOT_FOUND"
}
```

**Error Categories:**

- `SyntaxError` - YAML syntax invalid
- `SchemaError` - Violates pipeline schema
- `TemplateError` - Template resolution failed
- `VariableError` - Variable resolution failed
- `ExecutionError` - Step execution failed
- `NetworkError` - Remote fetch failed
- `ConfigError` - Configuration invalid

**Exit Codes:**

- `0` - Success
- `1` - Validation/lint errors
- `2` - Execution failures
- `3` - Usage/configuration errors
- `4` - Network/schema fetch errors

## Risk Analysis & Mitigation

### High-Severity Risks

#### Risk 1: Remote Template Resolution & Schema Divergence

**Severity:** HIGH

**Description:**  
Resolving templates over HTTP and handling schema version mismatches can cause:

- Network timeouts
- Stale cached templates
- Schema incompatibility between local and cloud

**Mitigation Strategies:**

1. **Caching with Validation**
   - Cache templates with ETag/Last-Modified headers
   - Implement TTL (default 1 hour)
   - Validate cache before use

2. **Retry Logic**
   - Exponential backoff (100ms → 6.4s)
   - Max 3 retries for transient failures
   - Circuit breaker for persistent failures

3. **Offline Fallback**
   - `--offline` mode uses cached templates only
   - `--no-remote` skips HTTP fetches entirely
   - Clear logging of what's cached vs. missing

**Contingency:**

- Enable "degraded mode" when offline: ignore remote templates or prompt for local copies
- Fail-fast with clear guidance on what to do

---

#### Risk 2: Variable Substitution Complexity

**Severity:** HIGH

**Description:**  
Multi-level variable scoping (global → stage → job → step) can cause:

- Circular variable references
- Variable collisions
- Unresolved variables

**Mitigation Strategies:**

1. **Hierarchical Mapping**
   - Build scope hierarchy explicitly
   - Detect collisions with detailed warnings
   - Report all unresolved variables

2. **Validation**
   - Detect circular references before substitution
   - Fail-fast on critical issues
   - Warn on ambiguous scopes

3. **Remediation**
   - `--allow-unresolved` flag for permissive mode
   - Report all missing keys with suggestions

**Contingency:**

- `--allow-unresolved` mode continues with warnings
- Generate comprehensive report of all unresolved variables

---

#### Risk 3: Condition & Dependency Evaluation

**Severity:** HIGH

**Description:**  
Complex condition evaluation (if conditions, dependencies, parallelism) can cause:

- Incorrect execution order
- Cyclic dependencies
- Unmet dependencies

**Mitigation Strategies:**

1. **Semantic Graph Validation**
   - Build dependency DAG
   - Detect cycles before execution
   - Validate max-parallel constraints

2. **Condition Alignment**
   - Match Azure DevOps condition semantics
   - Evaluate `if:` expressions correctly
   - Support variables in conditions

3. **Testing**
   - Differential testing on real ADO pipelines
   - Test case library for common patterns

**Contingency:**

- Fail-fast with annotated dependency graph
- Propose alternative graph structures

---

#### Risk 4: Azure DevOps Behavior Divergence

**Severity:** HIGH

**Description:**  
Local execution may differ from actual Azure DevOps in:

- Task implementations
- Expression evaluation
- Secret masking
- Internal tasks not documented

**Mitigation Strategies:**

1. **Semantic Alignment**
   - Follow Azure DevOps documentation precisely
   - Differential testing on real pipelines
   - Version-specific implementations

2. **Task Support**
   - Built-in tasks for common operations
   - Whitelist/blacklist unsupported tasks
   - Mock tasks with clear warnings

3. **Secret Masking**
   - Mask values marked as secrets
   - Align masking with ADO behavior

**Contingency:**

- Document unsupported features clearly
- Provide workarounds (mock, skip, override)
- Link to ADO documentation

---

#### Risk 5: Local Script Execution Security

**Severity:** HIGH

**Description:**  
Executing arbitrary scripts locally poses security risks:

- Malicious scripts
- Data leakage
- Unintended side effects

**Mitigation Strategies:**

1. **Isolation**
   - Execute in dedicated working directory
   - Restrict file system access where possible
   - Use process isolation

2. **Warnings**
   - Explicit warning before executing external scripts
   - Option to dry-run without execution
   - Log all executed commands

3. **Safe Mode**
   - `--safe-mode` disables execution, only analyzes

**Contingency:**

- Default to dry-run for untrusted pipelines
- Require explicit `--execute` confirmation

---

### Medium-Severity Risks

#### Risk 6: Cross-Platform Compatibility

**Severity:** MEDIUM

**Description:**  
Supporting Windows, macOS, and Linux with multiple shells (PowerShell, Bash, CMD) introduces compatibility challenges.

**Mitigation:**

- Abstraction layer for shell execution
- Cross-platform path normalization
- Encoding handling per platform
- Matrix testing (3 OS × 3 shells)

**Contingency:**

- Gracefully disable unavailable shells
- Suggest alternatives (e.g., use Bash on Windows with WSL)

---

#### Risk 7: Performance & Startup Time

**Severity:** MEDIUM

**Description:**  
Large pipelines with recursive templates and variables can exceed 5-second startup target.

**Mitigation:**

- In-memory caching
- Streaming YAML parsing
- Lazy template loading
- Profiling and metrics

**Contingency:**

- Light validation mode (syntax + schema without expansion)
- Report non-evaluated elements

---

#### Risk 8: Task Coverage & Custom Tasks

**Severity:** MEDIUM

**Description:**  
Many custom/proprietary tasks may not be supported.

**Mitigation:**

- Extensible task registry
- Default mocks for unknown tasks
- Clear documentation of supported tasks

**Contingency:**

- Skip unsupported tasks with warnings
- Allow task filtering/ignoring

---

#### Risk 9: Usability & Diagnostics

**Severity:** MEDIUM

**Description:**  
Poor diagnostics make troubleshooting difficult.

**Mitigation:**

- Structured logging with timestamps
- Line number mapping
- Multiple verbosity levels
- `--debug-trace` mode for deep analysis
- Diagnostic bundle export

**Contingency:**

- Export detailed JSON trace
- Correlate logs across components

---

### Low-Severity Risks

#### Risk 10: Scope Creep

**Severity:** LOW

**Description:**  
V1 is CLI-only; users may expect GUI, CI integration, etc.

**Mitigation:**

- Clear communication of V1 scope
- Public roadmap
- Documentation of out-of-scope features

**Contingency:**

- v2 roadmap for GUI/integration features

---

## Implementation Roadmap

### Phase 1: MVP (Sprints 1-2, ~4 weeks)

**Objective:** Basic validation and template resolution

**Deliverables:**

- Project scaffolding (.NET 8, CLI framework)
- YAML parser and syntax validator
- Schema manager with caching
- Basic template resolver (local files only)
- Simple variable processor (no scopes)
- `validate` command
- CLI framework with argument parsing
- Basic logging and error reporting

**Acceptance Criteria:**

- Successfully validates simple YAML pipelines
- Reports syntax and schema errors with line numbers
- Resolves local templates recursively
- Processes inline variables

**Team:** 2 developers, 1 QA

---

### Phase 2: Core Features (Sprints 3-4, ~4 weeks)

**Objective:** Variable scoping, template expansion, mock services

**Deliverables:**

- Hierarchical variable scoping (global → stage → job → step)
- Collision detection and circular reference detection
- Template resolution with HTTP support
- `expand` command for YAML emission
- Mock services layer (variable groups, service connections, agent pools)
- Improved error messages and diagnostics
- Configuration file support

**Acceptance Criteria:**

- Correct variable substitution with scope hierarchy
- HTTP templates with caching and retry
- Expanded YAML accurately represents all substitutions
- Mock services integrate with execution

**Team:** 2 developers, 1 QA, 1 DevOps consultant

---

### Phase 3: Execution Engine (Sprints 5-6, ~4 weeks)

**Objective:** Local execution simulation

**Deliverables:**

- Dependency graph builder
- Condition evaluator
- Parallel execution simulator
- Shell executor abstraction (PowerShell, Bash, CMD)
- Built-in tasks (script, PowerShell, bash, cmd)
- `exec` command with stage/job/step filtering
- Dry-run mode
- Execution reporting

**Acceptance Criteria:**

- Correctly builds dependency DAG
- Detects cycles and circular dependencies
- Evaluates if conditions accurately
- Executes steps in correct order with proper isolation
- Supports cross-platform shell execution
- Reports per-stage/job/step status

**Team:** 2-3 developers, 1 QA, 1 DevOps engineer

---

### Phase 4: Polish & Release (Sprint 7, ~2 weeks)

**Objective:** Linting, diagnostics, documentation, and release

**Deliverables:**

- `lint` command with opinionated rules
- `diag` command for diagnostic bundles
- Comprehensive documentation
- Installation guides (all platforms)
- Troubleshooting guide
- Example pipelines and scenarios
- Performance profiling and optimization

**Acceptance Criteria:**

- All acceptance criteria from Phases 1-3 met
- Linting identifies common anti-patterns
- Diagnostic bundle useful for troubleshooting
- Documentation complete and tested
- CLI works on Windows, macOS, Linux
- Startup time < 5 seconds
- All test acceptance criteria passing

**Team:** 1-2 developers, 1 QA, 1 technical writer

---

### Phase 5: Optional Enhancements (Post-v1)

**Deliverables:**

- Colored CLI interface
- Advanced caching strategies
- Performance optimization for large pipelines
- Extended task library
- Remote artifact repository support

---

## Success Criteria

### Functional Success

| Criterion | Target | Validation |
|-----------|--------|---|
| YAML Validation | 100% syntax accuracy | Unit tests against spec |
| Schema Compliance | 100% schema rule enforcement | Integration tests with ADO examples |
| Template Resolution | 100% local + HTTP resolution | E2E tests with nested templates |
| Variable Processing | 100% scope accuracy | Differential testing vs. ADO |
| Execution Simulation | 90%+ stage/job accuracy | Comparison with actual ADO runs |
| Cross-Platform | Windows/macOS/Linux support | CI matrix testing |

### Performance Success

| Criterion | Target | Validation |
|-----------|--------|---|
| Startup Time | < 5 seconds | Profiling on reference pipelines |
| Validation Time | < 1 second (typical) | Benchmark suite |
| Memory Usage | < 200 MB (typical) | Memory profiling |
| Cache Hit Rate | 95%+ for templates | Logging and metrics |

### Quality Success

| Criterion | Target | Validation |
|-----------|--------|---|
| Test Coverage | 80%+ code coverage | Code coverage reports |
| Documentation | 100% API documented | Docs build with no warnings |
| Error Messages | All errors have hints | Manual testing |
| User Satisfaction | 80%+ positive feedback | Beta user survey |
