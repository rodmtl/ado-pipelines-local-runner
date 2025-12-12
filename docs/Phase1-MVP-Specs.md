# ADO Pipelines Local Runner — Phase 1 (MVP) Specifications

Document version: 1.0  
Date: 2025-12-12

## 1. Overview

Phase 1 delivers the core validation capabilities for Azure DevOps YAML pipelines locally via a CLI `validate` command. It includes YAML parsing, syntax validation, schema validation (with local caching), local template resolution (files only), simple variable processing, structured error reporting, and basic logging. The design applies SOLID and favors TDD for high testability.

## 2. Scope (Phase 1)

- CLI command: `azp-local validate`
- YAML parser and syntax validator
- Schema manager with local cache
- Template resolver for local files only (no HTTP)
- Variable processor (inline and files; basic resolution without advanced scoping)
- Structured error reporting and logging
- Configuration file support (`azp-local.config.yaml` minimal subset)

Out of scope (Phase 1): Execution simulation, remote template fetching, complex variable scoping, linting, diagnostics bundles.

## 3. Architecture (High-Level)

### 3.1 Component Diagram (Text)

```md
CLI (validate)
  └─ ValidationOrchestrator
       ├─ YamlParser
       ├─ SyntaxValidator
       ├─ SchemaManager
       ├─ TemplateResolver (Local)
       ├─ VariableProcessor (Basic)
       ├─ ErrorReporter
       └─ Logger
```

### 3.2 Layers

- Presentation: CLI + `ValidationOrchestrator`
- Domain/Business: Parser, Validators, Resolver, Variable Processor
- Infrastructure: FileSystem, CacheStore, ConfigProvider, Logger

### 3.3 Design Principles (SOLID)

- Single Responsibility: Each component handles one concern (parse, validate, resolve, process, report).
- Open/Closed: Validators/resolvers extend via interfaces; core orchestrator relies on abstractions.
- Liskov Substitution: Alternative implementations (e.g., different parsers) can be substituted transparently.
- Interface Segregation: Fine-grained interfaces (e.g., `IYamlParser`, `ISyntaxValidator`) for precise contracts.
- Dependency Inversion: Orchestrator depends on abstractions; DI container wires concrete implementations.

### 3.4 Key Patterns

- Strategy: Validators, resolvers, processors are strategies behind interfaces.
- Factory: Component factories for parser/validator instantiation.
- Builder: Validation result aggregation.
- Adapter: File system abstraction (`IFileSystem`).

## 4. Interfaces & Contracts (Phase 1)

### 4.1 Core Interfaces

- `IYamlParser`
  - `Task<PipelineDocument> ParseAsync(Stream content, CancellationToken ct)`
- `ISyntaxValidator`
  - `Task<IReadOnlyList<ValidationIssue>> ValidateSyntaxAsync(PipelineDocument doc, CancellationToken ct)`
- `ISchemaManager`
  - `Task<PipelineSchema> GetSchemaAsync(string? version, CancellationToken ct)`
  - `Task<IReadOnlyList<ValidationIssue>> ValidateSchemaAsync(PipelineDocument doc, PipelineSchema schema, CancellationToken ct)`
- `ITemplateResolver`
  - `Task<PipelineDocument> ResolveAsync(PipelineDocument doc, string basePath, CancellationToken ct)`
- `IVariableProcessor`
  - `Task<PipelineDocument> ApplyAsync(PipelineDocument doc, VariablesInput variables, CancellationToken ct)`
- `IErrorReporter`
  - `ValidationReport BuildReport(ValidationContext ctx, IEnumerable<ValidationIssue> issues)`
- `ILogger`
  - `void Log(LogLevel level, string message, object? data = null)`

### 4.2 DTOs & Models

- `PipelineDocument`: AST + `SourceMap` (file, line, column)
- `ValidationIssue`: `{ code, severity, message, location }`
- `ValidationReport`: `{ status, issues[], summary, timings }`
- `PipelineSchema`: JSON schema doc (cached)
- `VariablesInput`: `{ files: string[], inline: Dictionary<string, string> }`
- `Config`: `{ pipelinePath, varsFiles[], schemaVersion?, basePath }`

### 4.3 Error Types

- `ParseException`, `SchemaValidationException`, `TemplateResolutionException`, `VariableProcessingException`, `ConfigurationException`

## 5. Functional Requirements

### FR-1: YAML Syntax Validation

- The system parses YAML and reports syntax errors with file/line/column.
- Supports anchors/aliases; reports malformed constructs.

### FR-2: Schema Validation

- Validates against ADO pipeline schema (local cache, version auto or explicit).
- Reports missing required fields, type mismatches, and unknown properties.

### FR-3: Local Template Resolution

- Resolves `template:` references to local files relative to `basePath`.
- Supports recursive inclusion with depth limits (default 10).
- Reports missing files and circular references.

### FR-4: Variable Processing (Basic)

- Merges variables from files and inline.
- Substitutes `$(var)` and `${{ variables.var }}` where applicable.
- Reports undefined variables; allows `--allow-unresolved` to continue.

### FR-5: CLI `validate` Command

- Accepts `--pipeline`, `--vars`, `--var key=value`, `--schema-version`, `--base-path`, `--output` (`text|json|sarif`), `--strict`.
- Returns exit codes: 0 success, 1 validation errors, 3 config errors.

### FR-6: Structured Error Reporting

- Outputs issues with codes, severity (error/warn/info), and locations.
- Aggregates issues by category (Syntax, Schema, Template, Variable).

### FR-7: Logging

- Supports verbosity: `quiet|minimal|normal|detailed`.
- Writes to console; optional `--log-file` path.

## 6. Non-Functional Requirements

### NFR-1: Performance

- Syntax validation of typical pipelines (< 500 lines) completes in < 1s.
- Startup time < 5s under cold start.

### NFR-2: Reliability

- Deterministic outputs for same inputs.
- Clear failure modes; no hidden retries in Phase 1.

### NFR-3: Usability

- CLI `--help` shows commands/options with examples.
- Error messages include remediation hints.

### NFR-4: Maintainability

- 80%+ unit test coverage (line); adhere to SOLID.
- Modules decoupled via interfaces; DI for wiring.

### NFR-5: Portability

- Runs on Windows, macOS, Linux with .NET 8.
- No OS-specific assumptions in Phase 1.

## 7. Acceptance Criteria (Given-When-Then)

### AC-1: YAML Syntax Validation

- Given a malformed YAML file
- When running `azp-local validate --pipeline bad.yml`
- Then the output includes `SyntaxError` with file/line/column and exit code = 1

### AC-2: Schema Validation

- Given a valid YAML structure violating schema (missing `trigger`)
- When running validate
- Then a `SchemaError` is returned with the missing property and exit code = 1

### AC-3: Local Template Resolution

- Given a pipeline referencing `templates/build.yml` that exists
- When running validate
- Then the template content is included and validated; no errors reported

### AC-4: Circular Template Detection

- Given mutually recursive templates
- When running validate
- Then a `TemplateError` with `CIRCULAR_REFERENCE` is reported at depth > limit

### AC-5: Variable Substitution

- Given `$(buildConfiguration)` in YAML and `vars/common.yml` defines it
- When running validate with `--vars vars/common.yml`
- Then substitution occurs and no `VariableError` is reported

### AC-6: Undefined Variable Reporting

- Given `$(missingVar)` in YAML without definition
- When running validate
- Then a `VariableError` is reported; with `--allow-unresolved`, status is `warn` not `error`

### AC-7: CLI Output Formats

- Given `--output json`
- When running validate
- Then a JSON report with `issues[]`, `summary`, and `timings` is produced

### AC-8: Strict Mode

- Given warnings from schema or variables
- When running validate with `--strict`
- Then warnings are escalated to errors and exit code = 1

## 8. TDD Strategy

### 8.1 Unit Tests

- Parser: valid YAML, invalid YAML, anchors/aliases, large file
- Syntax: required fields presence, structure correctness
- Schema: required, types, unknown props
- Templates: resolve relative/absolute paths, missing files, depth limit, circular
- Variables: merge files+inline, substitution, undefined, allow-unresolved
- CLI: argument parsing, help text, exit codes, output formatting

### 8.2 Mocks/Stubs

- `IFileSystem` for file reads
- `ILogger` TestLogger capturing logs
- `ISchemaProvider` in-memory schemas

### 8.3 Coverage Targets

- 80%+ line coverage overall; 85%+ for validators

### 8.4 Integration Tests

- End-to-end validate on sample pipelines (simple, with templates, with variables)

## 9. Diagrams (Text)

### 9.1 Validation Flow

```md
CLI → ValidationOrchestrator
  → YamlParser → SyntaxValidator → SchemaManager
  → TemplateResolver (Local) → VariableProcessor (Basic)
  → ErrorReporter → Output
```

### 9.2 Template Resolution (Local)

```md
PipelineDocument
  └─ Find template refs
      └─ Resolve local path (basePath)
          └─ Load & parse included file
              └─ Merge into AST
                  └─ Repeat until no refs or depth limit
```

### 9.3 Error Handling

```md
Component raises issue
  └─ Orchestrator aggregates
      └─ Categorize by type
          └─ Map severity (warn/error)
              └─ Build ValidationReport
                  └─ Format as text/json/sarif
```

## 10. Configuration (Phase 1 Subset)

`azp-local.config.yaml` (subset):

```yaml
pipeline:
  path: "azure-pipelines.yml"
  basePath: "./"
variables:
  files:
    - "vars/common.yml"
  inline:
    buildConfiguration: "Release"
output:
  format: "text"
```

## 11. Release Criteria

- All acceptance criteria (AC-1..AC-8) pass on CI across Windows/macOS/Linux.
- Unit test coverage ≥ 80%; integration suite green.
- CLI help and usage documented.
- Performance targets met on reference pipelines.

---

Prepared by: Software Architect, Senior Developer, QA Engineer
