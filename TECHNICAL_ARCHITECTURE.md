# Azure DevOps YAML Pipeline Local Validator & Debugger

## Technical Architecture Plan

---

## 1. System Overview

### 1.1 High-Level Architecture Diagram

```md
┌─────────────────────────────────────────────────────────────────┐
│                     CLI Entry Point                             │
│              (Command Parser & Orchestrator)                    │
└────────────┬────────────────────────────────────────────────────┘
             │
             ├──────────────────────┬──────────────────────┐
             │                      │                      │
             v                      v                      v
    ┌────────────────┐    ┌────────────────┐    ┌────────────────┐
    │   File Input   │    │ YAML Validator │    │ Schema Manager │
    │   Resolver     │    │   (Syntax)     │    │   (ADO Schema) │
    └────────────────┘    └────────────────┘    └────────────────┘
             │                      │                      │
             └──────────────────────┴──────────────────────┘
                            │
                            v
                ┌─────────────────────────┐
                │  Template Resolver      │
                │  - Local includes       │
                │  - Remote templates     │
                │  - Template parameters  │
                └──────────┬──────────────┘
                           │
                           v
                ┌─────────────────────────┐
                │ Variable Processor      │
                │  - Var files            │
                │  - Inline vars          │
                │  - Mock var groups      │
                │  - Substitution engine  │
                └──────────┬──────────────┘
                           │
                           v
                ┌─────────────────────────┐
                │ AST/IR Builder          │
                │ (Normalized Pipeline)   │
                └──────────┬──────────────┘
                           │
                    ┌──────┴──────┐
                    │             │
                    v             v
            ┌──────────────┐  ┌───────────────┐
            │  Execution   │  │ Diagnostic    │
            │  Engine      │  │ Reporter      │
            │  - Stage Runner  │ - Logs      │
            │  - Job Runner    │ - Metrics   │
            │  - Step Runner   │ - Traces    │
            └──────────────┘  └───────────────┘
                    │                   │
                    └─────────┬─────────┘
                              │
                              v
                    ┌──────────────────┐
                    │ Output Formatter  │
                    │ & Report Builder  │
                    └──────────────────┘
```

---

## 2. System Components

### 2.1 Component Details and Responsibilities

#### 2.1.1 CLI Entry Point & Orchestrator

- **Purpose**: Main application entry point; orchestrates workflow
- **Responsibilities**:
  - Parse command-line arguments
  - Validate input parameters
  - Instantiate component pipeline
  - Coordinate execution flow
  - Handle error propagation
- **Key Exports**: Main function, configuration parser

#### 2.1.2 File Input Resolver

- **Purpose**: Discover and load YAML pipeline files
- **Responsibilities**:
  - Accept single file or directory path
  - Auto-discover pipeline files (*.yml,*.yaml)
  - Validate file accessibility
  - Load file content into memory
  - Build file dependency graph
- **Key Outputs**: File collection, file metadata

#### 2.1.3 YAML Validator (Syntax)

- **Purpose**: Validate YAML syntax correctness
- **Responsibilities**:
  - Parse YAML content
  - Report syntax errors with line/column references
  - Detect malformed structures
  - Provide clear error messages
- **Key Outputs**: Validation results, error details

#### 2.1.4 Schema Manager

- **Purpose**: Maintain and enforce Azure DevOps pipeline schema
- **Responsibilities**:
  - Load/cache ADO YAML schema definition
  - Validate document structure against schema
  - Enforce required properties and types
  - Track schema version compatibility
- **Key Outputs**: Schema validation results

#### 2.1.5 Template Resolver

- **Purpose**: Resolve and inline template references
- **Responsibilities**:
  - Detect template references (`- template:`)
  - Load referenced templates (local and remote)
  - Handle template parameters
  - Resolve nested templates recursively
  - Merge template content into pipeline
  - Track resolved dependencies
- **Key Outputs**: Expanded pipeline with inlined templates

#### 2.1.6 Variable Processor

- **Purpose**: Process and manage variable substitution
- **Responsibilities**:
  - Load variable files (JSON, YAML)
  - Parse inline variable definitions
  - Manage variable scopes (global, stage, job, step)
  - Simulate variable group definitions
  - Perform variable substitution across pipeline
  - Handle variable runtime evaluation
  - Support parameter defaults
- **Key Outputs**: Substituted pipeline, variable manifest

#### 2.1.7 AST/IR Builder

- **Purpose**: Construct normalized intermediate representation
- **Responsibilities**:
  - Transform YAML into typed data structures
  - Normalize pipeline structure
  - Resolve inheritance and defaults
  - Validate semantic correctness (e.g., job dependencies)
  - Build execution graph
- **Key Outputs**: Normalized pipeline IR/AST

#### 2.1.8 Execution Engine

- **Purpose**: Execute pipeline locally
- **Responsibilities**:
  - Stage runner: execute stages sequentially/parallel
  - Job runner: execute jobs with dependency resolution
  - Step runner: execute individual steps
  - Variable scoping during execution
  - Condition evaluation
  - Gate handling (approval gates, checks)
  - Job parallelization (respecting max-parallel settings)
- **Key Outputs**: Execution results, step outputs

#### 2.1.9 Mock Services Layer

- **Purpose**: Provide mock implementations of ADO services
- **Responsibilities**:
  - Mock variable groups with predefined values
  - Mock service connections (placeholder values)
  - Mock agent pool specifications
  - Mock environment definitions
  - Support configuration file for mock values
- **Key Outputs**: Service configuration objects

#### 2.1.10 Diagnostic Reporter

- **Purpose**: Capture and report execution diagnostics
- **Responsibilities**:
  - Collect timestamped log entries
  - Track execution metrics (duration, memory)
  - Build execution trace
  - Aggregate warnings/errors
  - Store diagnostic state
- **Key Outputs**: Diagnostic data structure

#### 2.1.11 Output Formatter & Report Builder

- **Purpose**: Format results for user consumption
- **Responsibilities**:
  - Format output based on user preference (JSON, table, text)
  - Build execution summary report
  - Highlight errors and warnings
  - Include performance metrics
  - Generate traces for debugging
- **Key Outputs**: Formatted reports, JSON exports

---

## 3. Core Modules Architecture

### 3.1 Module Dependency Graph

```md
┌─────────────────────────────────────────────────────────────┐
│                      Entry Point                            │
└────────────┬────────────────────────────────────────────────┘
             │
     ┌───────┴────────┐
     │                │
     v                v
┌──────────┐    ┌──────────────┐
│   CLI    │    │   Config     │
│  Parser  │    │   Loader     │
└──────────┘    └──────────────┘
     │                │
     └────────┬───────┘
              │
              v
     ┌─────────────────┐
     │  File Discovery │
     │   & Loading     │
     └────────┬────────┘
              │
      ┌───────┴────────┐
      │                │
      v                v
 ┌────────┐      ┌──────────┐
 │  YAML  │      │ Schema   │
 │ Parser │      │ Validator│
 └────┬───┘      └──────────┘
      │                │
      └────────┬───────┘
               │
               v
      ┌──────────────────┐
      │ Template Resolver│
      │  (Recursive)     │
      └────────┬─────────┘
               │
               v
      ┌──────────────────┐
      │ Variable         │
      │ Processor        │
      └────────┬─────────┘
               │
               v
      ┌──────────────────┐
      │  IR/AST Builder  │
      │  Semantic Val    │
      └────────┬─────────┘
               │
        ┌──────┴──────┐
        │             │
        v             v
   ┌─────────┐   ┌────────────┐
   │Execution│   │  Diagnostics
   │ Engine  │   │  Reporter  │
   └──────┬──┘   └────────────┘
          │
          v
   ┌─────────────┐
   │Output Format│
   │   & Report  │
   └─────────────┘
```

### 3.2 Core Modules Definition

#### 3.2.1 Parser Module

**Responsibilities**:

- YAML parsing with error recovery
- Schema validation
- Error reporting with source mapping

**Sub-modules**:

- `YamlParser`: YAML → JSON AST
- `SchemaValidator`: AST → Validation results
- `ErrorReporter`: Error formatting

#### 3.2.2 Template Module

**Responsibilities**:

- Template discovery and loading
- Template parameter binding
- Recursive resolution
- Cache management

**Sub-modules**:

- `TemplateLoader`: File/URL loading
- `TemplateResolver`: Reference resolution
- `ParameterBinder`: Parameter application
- `TemplateCache`: Caching layer

#### 3.2.3 Variable Module

**Responsibilities**:

- Variable file loading
- Variable scoping
- Substitution engine
- Mock variable groups

**Sub-modules**:

- `VariableFileLoader`: Load .json/.yml var files
- `VariableSubstitutor`: $(var) → value replacement
- `ScopeManager`: Handle variable scopes
- `MockVariableGroups`: Mock group definitions

#### 3.2.4 Execution Module

**Responsibilities**:

- Stage/Job/Step execution
- Condition evaluation
- Shell command execution
- Output capture

**Sub-modules**:

- `StageExecutor`: Stage orchestration
- `JobExecutor`: Job execution with dependencies
- `StepExecutor`: Individual step execution
- `ShellExecutor`: Command execution wrapper
- `ConditionEvaluator`: Condition logic

#### 3.2.5 Diagnostics Module

**Responsibilities**:

- Logging
- Metrics collection
- Trace generation

**Sub-modules**:

- `Logger`: Structured logging
- `MetricsCollector`: Performance metrics
- `TraceBuilder`: Execution trace construction

#### 3.2.6 Services Module

**Responsibilities**:

- Mock service implementations
- Service configuration

**Sub-modules**:

- `MockVariableGroupService`: Variable groups
- `MockServiceConnectionService`: Service connections
- `MockAgentPoolService`: Agent pools
- `MockEnvironmentService`: Environments

---

## 4. Technology Stack Recommendations

### 4.1 Programming Language Selection

#### Recommendation: **.NET 8 (C#)**

**Rationale**:

- Strong YAML parsing libraries (YamlDotNet)
- Excellent XML/JSON schema validation
- Cross-platform support (Windows, macOS, Linux)
- Built-in shell execution capabilities
- Rich diagnostics and logging frameworks
- Strong async/await support for parallel execution
- Azure SDK integration capabilities
- Large enterprise community

**Alternatives**:

- **Node.js**: Good cross-platform support, excellent JSON handling, but weaker YAML ecosystem
- **Python**: Rapid development, good YAML support (PyYAML), but deployment complexity for CLI

**Decision**: .NET 8 (C#) as primary, with potential Node.js alternative

### 4.2 Core Dependencies

#### YAML Processing

- **YamlDotNet** (v14.x): YAML parsing and serialization
- **Alternative**: NYaml (if needed for better performance)

#### Schema Validation

- **JsonSchema.Net** (v6.x): JSON Schema validation
- **YamlSchema**: Custom schema wrapper

#### CLI Framework

- **System.CommandLine**: Modern CLI parsing (.NET 8 built-in approach)
- **Alternative**: Spectre.Console (for rich console output)

#### File System

- **System.IO**: Built-in file operations
- **Glob**: Wildcard file pattern matching

#### Execution & Shell

- **System.Diagnostics.Process**: Shell command execution
- **Hosting Infrastructure** (if needed): For PowerShell execution

#### Configuration

- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.DependencyInjection**: Dependency injection

#### Logging

- **Serilog**: Structured logging
- **Serilog.Sinks.Console**: Console output
- **Serilog.Sinks.File**: File logging

#### Testing

- **xUnit**: Unit testing
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library

#### Additional Libraries

- **Spectre.Console**: Rich console formatting
- **Newtonsoft.Json / System.Text.Json**: JSON handling
- **Polly**: Resilience/retry policies (for remote template loading)

### 4.3 Project Structure (Solution Layout)

```md
AzDevopsLocalRunner/
├── src/
│   ├── AzDevopsLocalRunner.Core/
│   │   ├── Abstractions/
│   │   │   ├── IYamlParser.cs
│   │   │   ├── ITemplateResolver.cs
│   │   │   ├── IVariableProcessor.cs
│   │   │   ├── IExecutionEngine.cs
│   │   │   └── ...
│   │   ├── Models/
│   │   │   ├── Pipeline.cs
│   │   │   ├── Stage.cs
│   │   │   ├── Job.cs
│   │   │   ├── Step.cs
│   │   │   ├── Variable.cs
│   │   │   └── ...
│   │   ├── Parsing/
│   │   │   ├── YamlParser.cs
│   │   │   ├── SchemaValidator.cs
│   │   │   └── ErrorReporter.cs
│   │   ├── Templates/
│   │   │   ├── TemplateResolver.cs
│   │   │   ├── TemplateLoader.cs
│   │   │   └── ParameterBinder.cs
│   │   ├── Variables/
│   │   │   ├── VariableProcessor.cs
│   │   │   ├── VariableSubstitutor.cs
│   │   │   └── ScopeManager.cs
│   │   ├── Execution/
│   │   │   ├── ExecutionEngine.cs
│   │   │   ├── StageExecutor.cs
│   │   │   ├── JobExecutor.cs
│   │   │   ├── StepExecutor.cs
│   │   │   └── ShellExecutor.cs
│   │   ├── Diagnostics/
│   │   │   ├── Logger.cs
│   │   │   ├── MetricsCollector.cs
│   │   │   └── TraceBuilder.cs
│   │   └── Services/
│   │       ├── MockVariableGroupService.cs
│   │       ├── MockServiceConnectionService.cs
│   │       └── ...
│   ├── AzDevopsLocalRunner.Cli/
│   │   ├── Program.cs
│   │   ├── CommandHandler.cs
│   │   └── OutputFormatter.cs
│   └── AzDevopsLocalRunner.Tasks/
│       ├── TaskRegistry.cs
│       ├── Built-in tasks/
│       │   ├── PowerShellTask.cs
│       │   ├── BashTask.cs
│       │   ├── ScriptTask.cs
│       │   └── ...
│       └── CustomTaskProvider.cs
├── tests/
│   ├── AzDevopsLocalRunner.Tests/
│   │   ├── Parsing/
│   │   ├── Templates/
│   │   ├── Variables/
│   │   ├── Execution/
│   │   └── Integration/
│   └── AzDevopsLocalRunner.TestData/
│       ├── Pipelines/
│       ├── Variables/
│       ├── Templates/
│       └── Fixtures/
├── docs/
│   ├── Architecture.md
│   ├── Installation.md
│   ├── Usage.md
│   ├── Configuration.md
│   ├── Extensibility.md
│   └── Troubleshooting.md
└── AzDevopsLocalRunner.sln
```

---

## 5. Data Flow Diagrams (Text-Based)

### 5.1 Validation Flow

```md
Input YAML File(s)
        │
        v
┌──────────────────┐
│ File Discovery   │ → Validate file exists, readable
└────────┬─────────┘
         │
         v
┌──────────────────┐
│ YAML Parse       │ → Syntax check, build AST
└────────┬─────────┘
         │
    ┌────┴─────┐
    │           │
    v           v
ERROR       ┌────────────────┐
(Report)    │ Schema Validate │ → Type/structure check
            └────────┬───────┘
                     │
                ┌────┴─────┐
                │           │
              ERROR      ┌───────────┐
            (Report)     │ Validated │ → Output: Success
                         │  YAML AST │
                         └───────────┘
```

**Data Items Flowing**:

- File paths → File content → YAML tokens → AST nodes → Validation result

### 5.2 Template Resolution Flow

```md
Parsed Pipeline AST
        │
        v
┌─────────────────────────┐
│ Detect Template Refs    │ → Find "- template: ..." blocks
│ (Scan AST)              │
└────────┬────────────────┘
         │
    ┌────┴────────┬──────────┐
    │             │          │
 Local         Remote    Parameters
 Templates     URLs      Binding
    │             │          │
    v             v          v
┌──────────┐  ┌──────────┐  ┌────────────┐
│Load File │  │HTTP GET  │  │Bind Params │
│(Cache)   │  │(Cached)  │  │To Template │
└────┬─────┘  └────┬─────┘  └────┬───────┘
     │             │             │
     └─────────────┴─────────────┘
                  │
                  v
        ┌─────────────────────┐
        │ Recursive Resolve   │ → If template contains templates
        │ (Depth-first)       │
        └──────────┬──────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
        v                     v
    More Templates         No More
        │                 Templates
        v                     │
      Loop                    v
                    ┌─────────────────┐
                    │ Merge Resolved  │
                    │ Templates Into  │
                    │ Pipeline        │
                    └─────────────────┘
```

**Data Items Flowing**:

- Pipeline AST → Template references → File/URL paths → Template YAML → Bound parameters → Merged AST

### 5.3 Variable Processing Flow

```md
Pipeline AST + Variable Inputs
        │
        v
┌──────────────────────────┐
│ Collect Variable Sources │ → .variables files, inline vars, groups
└────────┬─────────────────┘
         │
    ┌────┴────┬─────────┬────────────┐
    │         │         │            │
    v         v         v            v
 File 1    File 2   Inline    Mock Groups
   │         │       Vars       │
   └────┬────┴────┬────┴────────┘
        v         v
    ┌───────────────────────┐
    │ Build Variable Map    │ → Hierarchy: Global → Stage → Job → Step
    │ (Scope Resolution)    │
    └────────┬──────────────┘
             │
             v
    ┌─────────────────────┐
    │ Find All $(var)     │ → Regex scan pipeline
    │ References in AST   │
    └────────┬────────────┘
             │
             v
    ┌──────────────────────┐
    │ For Each Reference:  │
    │  1. Resolve scope    │
    │  2. Lookup in map    │
    │  3. Replace in AST   │
    └────────┬─────────────┘
             │
             v
    ┌──────────────────────┐
    │ Substituted AST      │ → All variables expanded
    │ + Used Variables Log │
    └──────────────────────┘
```

**Data Items Flowing**:

- Sources (files, inline, groups) → Variable map (scoped) → References found → Substitutions made → Output AST

### 5.4 Execution Flow

```md
Substituted Pipeline AST
        │
        v
┌─────────────────────┐
│ IR/AST → Execution  │ → Build execution graph with dependencies
│ Graph Builder       │
└────────┬────────────┘
         │
         v
┌─────────────────────┐
│ Validate Graph      │ → Check dependencies, cycles, conditions
│ (Semantic Check)    │
└────────┬────────────┘
         │
    ┌────┴─────┐
    │           │
  ERROR     ┌─────────────────┐
(Report)    │ Execute Stages  │ → sequential (default)
            └────────┬────────┘
                     │
         ┌───────────┴───────────┐
         │ For Each Stage:       │
         └───────────┬───────────┘
                     │
                     v
         ┌─────────────────────┐
         │ Execute Jobs        │ → Parallel respecting max-parallel
         │ (Respect Deps)      │
         └────────┬────────────┘
                  │
        ┌─────────┴─────────┐
        │ For Each Job:     │
        └────────┬──────────┘
                 │
                 v
        ┌──────────────────┐
        │ Execute Steps    │ → Sequential by default
        │ (Each step)      │
        └────────┬─────────┘
                 │
         ┌───────┴────────┐
         │ For Each Step: │
         └───────┬────────┘
                 │
                 v
        ┌──────────────────────┐
        │ Evaluate Condition   │ → Skip if condition = false
        │ if present           │
        └────────┬─────────────┘
                 │
          ┌──────┴──────┐
          │             │
       Condition     ┌─────────────────┐
      = false        │ Execute Step:   │
        │            │ - Setup context │
        │            │ - Run shell cmd │
        │            │ - Capture output│
        │            │ - Collect logs  │
        │            └─────────────────┘
        │                     │
        └─────────────┬───────┘
                      │
                      v
        ┌──────────────────────┐
        │ Record Step Result:  │ → Success/Failure + output
        │ - Status             │
        │ - Duration           │
        │ - Output/Logs        │
        └──────────────────────┘
                     │
                     v
        ┌────────────────────────────┐
        │ All Steps Complete?        │
        │ → Aggregate Job Result     │
        └──────────────────────────┘
                     │
                     v
        ┌────────────────────────────┐
        │ All Jobs Complete?         │
        │ → Aggregate Stage Result   │
        └──────────────────────────┘
                     │
                     v
        ┌────────────────────────────┐
        │ All Stages Complete?       │
        │ → Final Pipeline Result    │
        └──────────────────────────┘
                     │
                     v
        ┌────────────────────────────┐
        │ Collect All Diagnostics    │ → Logs, metrics, traces
        └────────────────────────────┘
```

**Data Items Flowing**:

- AST → Execution graph → Step executions → Results → Aggregated results → Final report

### 5.5 End-to-End Processing Pipeline

```md
┌─────────────────┐
│  User Input     │ (CLI args)
│  - File path    │
│  - Options      │
└────────┬────────┘
         │
         v
┌─────────────────────────┐
│ Configuration Loading   │
│ - Mock services config  │
│ - Execution settings    │
└────────┬────────────────┘
         │
         v
┌─────────────────────────┐
│ FILE RESOLUTION PHASE   │
│ → Discover YAML files   │
└────────┬────────────────┘
         │
         v
┌─────────────────────────┐
│ PARSING PHASE           │
│ → Validate YAML syntax  │
│ → Schema validation     │
└────────┬────────────────┘
         │
┌────────┴────────┐
│                 │ Errors?
│ No              │
v                 v
┌──────────────┐  ERROR REPORT
│ RESOLUTION   │  + Exit
│ PHASE        │
│ → Templates  │
│ → Variables  │
└────────┬─────┘
         │
┌────────┴────────┐
│                 │ Errors?
│ No              │
v                 v
┌──────────────┐  ERROR REPORT
│ BUILD PHASE  │  + Exit
│ → IR/AST     │
│ → Validate   │
│   semantic   │
└────────┬─────┘
         │
┌────────┴────────┐
│                 │ Errors?
│ No              │
v                 v
┌──────────────┐  ERROR REPORT
│ EXECUTION    │  + Exit
│ PHASE        │
│ → Execute    │
│ → Collect    │
│   results    │
└────────┬─────┘
         │
         v
┌──────────────────┐
│ FORMAT & OUTPUT  │
│ → Build report   │
│ → Write output   │
└──────────────────┘
         │
         v
    ┌─────────────┐
    │  Exit Code  │
    │ 0=success   │
    │ N=error     │
    └─────────────┘
```

---

## 6. Integration Points Between Components

### 6.1 Integration Matrix

| Component A | Component B | Integration Point | Data Exchanged |
|---|---|---|---|
| CLI Entry Point | File Resolver | Invokes | File paths, options |
| File Resolver | YAML Parser | Loads files for | File content |
| YAML Parser | Schema Manager | Validates | AST nodes |
| Schema Manager | Error Reporter | Reports errors | Validation errors |
| YAML Parser | Template Resolver | Passes parsed AST | Parsed structure |
| Template Resolver | File Resolver | Resolves remote | Template file paths |
| Template Resolver | YAML Parser | Parses template | Template content |
| Template Resolver | Variable Processor | Passes merged AST | Expanded AST |
| Variable Processor | Substitution Engine | Substitutes | Reference map |
| Variable Processor | IR Builder | Passes variables | Substituted AST |
| IR Builder | Execution Engine | Creates execution plan | IR graph |
| Execution Engine | Shell Executor | Executes steps | Commands, context |
| Execution Engine | Diagnostics Reporter | Logs events | Execution events |
| Diagnostics Reporter | Output Formatter | Aggregates | Raw diagnostics |
| Output Formatter | CLI Entry Point | Returns formatted | Formatted report |

### 6.2 Message/Contract Definitions

#### 6.2.1 Between Parser and Schema Manager

```md
ValidationRequest {
  astNode: AstNode,
  schemaVersion: string
}

ValidationResult {
  isValid: boolean,
  errors: ValidationError[],
  warnings: string[]
}
```

#### 6.2.2 Between Template Resolver and File Resolver

```md
TemplateReference {
  path: string,
  isRemote: boolean,
  parameters: Dictionary<string, object>
}

ResolvedTemplate {
  content: string,
  source: string,
  parameters: Dictionary<string, object>
}
```

#### 6.2.3 Between Variable Processor and Substitution Engine

```md
SubstitutionRequest {
  text: string,
  variableMap: Dictionary<string, string>,
  scope: ExecutionScope
}

SubstitutionResult {
  substitutedText: string,
  usedVariables: string[],
  unresolvedReferences: string[]
}
```

#### 6.2.4 Between Execution Engine and Step Executor

```md
StepExecutionContext {
  stepDefinition: Step,
  environmentVariables: Dictionary<string, string>,
  currentWorkingDirectory: string,
  timeout: TimeSpan
}

StepExecutionResult {
  status: ExecutionStatus, // Success, Failure, Skipped
  output: string,
  exitCode: int,
  duration: TimeSpan,
  logs: LogEntry[]
}
```

---

## 7. Mock Service Architecture

### 7.1 Mock Service Layer Design

```md
┌──────────────────────────────────────┐
│   Mock Service Configuration         │
│   (config.yml / appsettings.json)    │
└────────────┬─────────────────────────┘
             │
     ┌───────┴────────┬─────────┬──────────┐
     │                │         │          │
     v                v         v          v
┌────────────┐   ┌─────────┐  ┌──────┐  ┌──────────┐
│  Variable  │   │Service  │  │Agent │  │Environment
│  Groups    │   │Connect  │  │Pools │  │Services
└────────────┘   └─────────┘  └──────┘  └──────────┘
     │                │         │          │
     └───────┬────────┴─────────┴──────────┘
             │
             v
    ┌──────────────────────┐
    │ Service Registry     │
    │ - Store configs      │
    │ - Provide access API │
    └──────────┬───────────┘
               │
        ┌──────┴──────┐
        │             │
        v             v
    During      During
   Parsing     Execution
        │         │
        v         v
 Replace   Resolve
References Values
```

### 7.2 Mock Service Implementations

#### 7.2.1 Mock Variable Groups Service

```yaml
MockVariableGroupService:
  - Groups: List<VariableGroup>
    - GroupName: string
    - Variables: Dictionary<string, string>
  
  Methods:
    - GetGroup(groupName: string): VariableGroup
    - ListGroups(): List<VariableGroup>
    - Validate(groupReference: string): bool

Example Config (YAML):
  mockServices:
    variableGroups:
      - name: "BuildSettings"
        variables:
          BuildConfiguration: "Release"
          TargetFramework: "net8.0"
      - name: "DeploymentSecrets"
        variables:
          StorageAccountKey: "mock-key-12345"
          ApiToken: "mock-token-abc123"
```

#### 7.2.2 Mock Service Connection Service

```yaml
MockServiceConnectionService:
  - Connections: List<ServiceConnection>
    - ConnectionName: string
    - Type: string (azureRM, github, docker, etc.)
    - Properties: Dictionary<string, string>

  Methods:
    - GetConnection(connectionName: string): ServiceConnection
    - ListConnections(): List<ServiceConnection>
    - Validate(connectionReference: string): bool

Example Config (YAML):
  mockServices:
    serviceConnections:
      - name: "AzureSubscription"
        type: "azureRM"
        properties:
          subscriptionId: "mock-sub-id"
          tenantId: "mock-tenant-id"
      - name: "GitHubConnection"
        type: "github"
        properties:
          token: "mock-gh-token"
```

#### 7.2.3 Mock Agent Pool Service

```yaml
MockAgentPoolService:
  - Pools: List<AgentPool>
    - PoolName: string
    - Agents: List<Agent>
    - Capabilities: Dictionary<string, string>

Example Config (YAML):
  mockServices:
    agentPools:
      - name: "Default"
        agents:
          - vmImage: "windows-latest"
            capabilities:
              - "Docker"
              - "Npm"
      - name: "LinuxAgents"
        agents:
          - vmImage: "ubuntu-latest"
            capabilities:
              - "Docker"
              - "Git"
```

#### 7.2.4 Mock Environment Service

```yaml
MockEnvironmentService:
  - Environments: List<Environment>
    - EnvironmentName: string
    - Approvers: List<string>
    - Checks: List<EnvironmentCheck>

Example Config (YAML):
  mockServices:
    environments:
      - name: "Production"
        approvers:
          - "admin@company.com"
        checks:
          - type: "ManualValidation"
          - type: "ServiceHealthCheck"
```

### 7.3 Mock Service Registry Pattern

```md
IServiceRegistry (Interface)
  ├── RegisterVariableGroupService(service)
  ├── RegisterServiceConnectionService(service)
  ├── RegisterAgentPoolService(service)
  ├── RegisterEnvironmentService(service)
  ├── GetService<T>(serviceType): T
  └── ValidateAllReferences(pipeline): ValidationResult

MockServiceRegistry (Implementation)
  ├── Services: Dictionary<Type, IService>
  ├── LoadConfiguration(configPath): void
  ├── Initialize(): void
  └── Resolve(): IServiceRegistry
```

---

## 8. Extensibility Architecture

### 8.1 Plugin/Task Extension Points

```md
┌─────────────────────────────────────┐
│  Custom Task Plugin System          │
└────────────┬────────────────────────┘
             │
     ┌───────┴────────┐
     │                │
     v                v
┌──────────────┐  ┌──────────────┐
│ Built-in     │  │ Custom Tasks │
│ Tasks        │  │ (Extensions) │
│ Registry     │  │ Registry     │
└──────┬───────┘  └──────┬───────┘
       │                 │
       └────────┬────────┘
                │
                v
        ┌──────────────┐
        │ Task         │
        │ Resolver     │
        │ (Unified)    │
        └──────┬───────┘
               │
               v
        ┌──────────────────┐
        │ Step Executor    │
        │ Instantiates &   │
        │ Runs Task        │
        └──────────────────┘
```

### 8.2 Task Interface Contract

```csharp
/// <summary>
/// Interface for implementing custom pipeline tasks
/// </summary>
public interface ITask
{
    /// <summary>
    /// Unique task identifier (e.g., "PowerShell", "Docker", "CustomBuild")
    /// </summary>
    string TaskId { get; }
    
    /// <summary>
    /// Task version (for compatibility checking)
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Execute the task with given context
    /// </summary>
    Task<TaskExecutionResult> ExecuteAsync(TaskExecutionContext context);
    
    /// <summary>
    /// Validate task inputs before execution
    /// </summary>
    ValidationResult ValidateInputs(TaskDefinition definition);
    
    /// <summary>
    /// Get schema definition for task inputs
    /// </summary>
    TaskSchema GetSchema();
}

public class TaskExecutionContext
{
    public Dictionary<string, string> Inputs { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; }
    public string WorkingDirectory { get; set; }
    public ILogger Logger { get; set; }
    public TimeSpan Timeout { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public class TaskExecutionResult
{
    public TaskStatus Status { get; set; } // Success, Failed, Skipped
    public string Output { get; set; }
    public int ExitCode { get; set; }
    public Dictionary<string, string> Outputs { get; set; }
    public TimeSpan Duration { get; set; }
}

public class TaskSchema
{
    public string TaskId { get; set; }
    public string Description { get; set; }
    public Dictionary<string, InputDefinition> Inputs { get; set; }
}

public class InputDefinition
{
    public string Name { get; set; }
    public string Type { get; set; } // string, number, boolean, etc.
    public bool Required { get; set; }
    public string Description { get; set; }
    public string DefaultValue { get; set; }
}
```

### 8.3 Built-in Task Registry

```md
TaskRegistry
├── PowerShellTask
│   └── Executes PowerShell scripts (.ps1)
├── BashTask
│   └── Executes Bash scripts (.sh)
├── CmdTask
│   └── Executes CMD commands (Windows)
├── ScriptTask
│   └── Generic script execution
├── DockerTask
│   └── Docker build/run operations
├── DownloadArtifactTask
│   └── Mock download of artifacts
└── PublishArtifactTask
    └── Mock publish of artifacts
```

### 8.4 Custom Task Development Guide (Example)

#### Example: Custom Docker Build Task

```csharp
public class CustomDockerBuildTask : ITask
{
    public string TaskId => "CustomDocker";
    public string Version => "1.0.0";
    
    public async Task<TaskExecutionResult> ExecuteAsync(TaskExecutionContext context)
    {
        var dockerfile = context.Inputs["dockerfile"];
        var imageName = context.Inputs["imageName"];
        var buildArgs = context.Inputs.GetValueOrDefault("buildArgs", "");
        
        // Validate inputs
        var validation = ValidateInputs(null);
        if (!validation.IsValid)
            return new TaskExecutionResult 
            { 
                Status = TaskStatus.Failed, 
                Output = string.Join("\n", validation.Errors) 
            };
        
        try
        {
            // Execute Docker build
            var command = $"docker build -t {imageName} -f {dockerfile} {buildArgs} .";
            var result = await ExecuteCommandAsync(command, context);
            return result;
        }
        catch (Exception ex)
        {
            return new TaskExecutionResult
            {
                Status = TaskStatus.Failed,
                Output = ex.Message
            };
        }
    }
    
    public ValidationResult ValidateInputs(TaskDefinition definition)
    {
        return new ValidationResult 
        { 
            IsValid = true 
        };
    }
    
    public TaskSchema GetSchema()
    {
        return new TaskSchema
        {
            TaskId = TaskId,
            Description = "Build Docker images",
            Inputs = new Dictionary<string, InputDefinition>
            {
                ["dockerfile"] = new InputDefinition 
                { 
                    Name = "dockerfile", 
                    Type = "string", 
                    Required = true,
                    Description = "Path to Dockerfile"
                },
                ["imageName"] = new InputDefinition 
                { 
                    Name = "imageName", 
                    Type = "string", 
                    Required = true,
                    Description = "Docker image name"
                },
                ["buildArgs"] = new InputDefinition 
                { 
                    Name = "buildArgs", 
                    Type = "string", 
                    Required = false,
                    Description = "Additional build arguments"
                }
            }
        };
    }
}
```

### 8.5 Extension Loading Mechanism

```md
┌────────────────────────────────┐
│ Extension Discovery            │
│ (Scan plugins directory)       │
└────────────┬───────────────────┘
             │
             v
┌────────────────────────────────┐
│ DLL Loading                    │
│ (Reflection-based)             │
└────────────┬───────────────────┘
             │
             v
┌────────────────────────────────┐
│ Interface Validation           │
│ (Verify ITask implementation)  │
└────────────┬───────────────────┘
             │
      ┌──────┴──────┐
      │             │
   Valid       Invalid
      │             │
      v             v
┌──────────┐   ┌─────────────┐
│ Register │   │ Log Warning │
│ Task     │   │ Skip Task   │
└──────────┘   └─────────────┘
```

---

## 9. Security & Isolation Considerations

### 9.1 Execution Isolation

```md
┌─────────────────────────────────┐
│  Step Execution Context         │
└────────────┬────────────────────┘
             │
    ┌────────┴─────────┐
    │                  │
    v                  v
┌──────────────┐  ┌──────────────┐
│ Isolated     │  │ Limited      │
│ Environment  │  │ Permissions  │
│ - Tmp dir    │  │ - No sudo    │
│ - Clean env  │  │ - Whitelist  │
└──────────────┘  └──────────────┘
```

### 9.2 Security Features

1. **Environment Variable Isolation**: Each step execution clears inherited env vars unless explicitly passed
2. **Working Directory Sandboxing**: Steps confined to designated directories
3. **Command Whitelisting**: Optional command validation
4. **Timeout Enforcement**: Prevent infinite loops
5. **Resource Limits**: Memory and CPU limits per task
6. **No Network Access**: Tasks cannot access external services (mock only)
7. **Read-Only File System**: Configurable read-only sections

---

## 10. Error Handling & Validation Strategy

### 10.1 Error Severity Levels

```md
Critical: Pipeline cannot proceed
├── YAML syntax errors
├── Missing required files
├── Invalid schema structure
├── Unresolvable dependencies
└── Task execution errors

High: Warnings that affect execution
├── Unused variables
├── Deprecated syntax
├── Ignored conditions
└── Skipped jobs

Medium: Information about execution
├── Variable substitutions
├── Template inclusions
└── Condition evaluations

Low: Diagnostic information
├── Verbose logging
├── Performance metrics
└── Debug traces
```

### 10.2 Error Reporting Format

```md
[ERROR] {Component}: {ErrorCode}
        Location: {File}:{Line}:{Column}
        Message: {Clear description}
        Suggestion: {How to fix}
        Context: {Surrounding code}

Example:
[ERROR] TemplateResolver: TR-001
        Location: azure-pipelines.yml:15:10
        Message: Template file not found
        Suggestion: Verify path is correct: 'templates/build.yml'
        Context: - template: templates/build.yml
                           ^^^^^^^^^^^^^^^^^
```

---

## 11. Summary Table

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Language** | C# / .NET 8 | Cross-platform, strong ecosystem |
| **YAML Library** | YamlDotNet | Industry standard for .NET |
| **CLI Framework** | System.CommandLine | Modern, built-in to .NET 8 |
| **Logging** | Serilog | Structured logging, extensible |
| **Architecture Pattern** | Modular pipeline | Clear separation of concerns |
| **Task Extension** | Interface-based plugins | Type-safe, discoverable |
| **Service Mocking** | Configuration-driven | Flexible, testable |
| **Execution Model** | Sequential with parallel jobs | Matches ADO behavior |
| **Error Strategy** | Multi-level with context | Developer-friendly reporting |
| **Performance Target** | <5s startup, <1s syntax validation | Meets requirements |

---

## 12. Implementation Roadmap

### Phase 1: Foundation (Sprint 1-2)

- Core data models (Pipeline, Stage, Job, Step)
- YAML parser and schema validator
- Basic CLI entry point
- File discovery and loading

### Phase 2: Template & Variables (Sprint 3-4)

- Template resolver with recursion
- Variable processor and substitution
- Mock services setup
- Semantic validation

### Phase 3: Execution (Sprint 5-6)

- Execution engine (stages, jobs, steps)
- Shell execution wrapper
- Step-level condition evaluation
- Output capture

### Phase 4: Diagnostics & Reporting (Sprint 7)

- Logging infrastructure
- Metrics collection
- Report formatting
- Trace generation

### Phase 5: Extension & Polish (Sprint 8)

- Task plugin system
- Custom task loading
- Documentation
- Cross-platform testing

---

*Document prepared for technical architecture review and implementation planning.*
