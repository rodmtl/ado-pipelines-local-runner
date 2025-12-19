# Phase 2: Core Features - Detailed Specifications

**Document Version:** 2.0  
**Created:** December 17, 2025  
**Status:** Ready for Implementation  
**Sprint Duration:** 4 weeks (Sprints 3-4)  
**Team Size:** 2 Developers, 1 QA Engineer, 1 DevOps Consultant  

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Objectives & Scope](#objectives--scope)
3. [Functional Requirements](#functional-requirements)
4. [Non-Functional Requirements](#non-functional-requirements)
5. [Architecture & Design](#architecture--design)
6. [Component Specifications](#component-specifications)
7. [Interface Contracts](#interface-contracts)
8. [Data Models](#data-models)
9. [TDD Strategy](#tdd-strategy)
10. [SOLID Principles Application](#solid-principles-application)
11. [Acceptance Criteria](#acceptance-criteria)
12. [Risk Management](#risk-management)
13. [Implementation Plan](#implementation-plan)

---

## Executive Summary

Phase 2 builds upon the Phase 1 MVP foundation to deliver advanced features for variable scoping, template expansion with remote resolution, and mock services integration. This phase focuses on enabling developers to work with complex, production-like pipelines locally.

### Key Deliverables

- **Hierarchical Variable Scoping** - Global, stage, job, and step-level variable resolution
- **HTTP Template Resolution** - Remote template fetching with intelligent caching and retry logic
- **Variable Collision Detection** - Identify and warn on variable conflicts
- **Circular Reference Detection** - Prevent infinite loops in variable/template dependencies
- **Expand Command** - Emit fully resolved, substituted YAML
- **Mock Services Layer** - Simulate variable groups, service connections, and agent pools
- **Configuration File Support** - YAML-based configuration for reusable settings

### Success Metrics

| Metric | Target |
|---|---|
| Variable Resolution Accuracy | 100% scope compliance |
| HTTP Template Cache Hit Rate | 95%+ |
| Circular Dependency Detection | 100% of cycles |
| Collision Detection | All conflicts identified |
| Configuration File Coverage | 90%+ of options |

---

## Objectives & Scope

### Primary Objectives

1. **Variable Management** - Implement hierarchical scoping and scope-aware substitution
2. **Remote Resolution** - Fetch templates from HTTP with caching and resilience
3. **Dependency Analysis** - Detect circular references and collisions
4. **Configuration Management** - Load configuration from YAML files
5. **Mock Services** - Simulate Azure DevOps services locally
6. **YAML Emission** - Generate fully expanded YAML for inspection

### Scope Boundaries

#### In Scope ✓

- Variable file loading (YAML/JSON)
- Hierarchical scope resolution
- HTTP template fetching with caching
- Collision and circular reference detection
- Configuration file parsing and validation
- Mock variable groups, service connections, agent pools
- `expand` command implementation
- Error handling and reporting

#### Out of Scope ✗

- Execution simulation (Phase 3)
- Linting rules (Phase 4)
- GUI interface (Post-v1)
- Custom task execution (Phase 3)
- Secret rotation and management
- Audit logging

---

## Functional Requirements

### FR1: Hierarchical Variable Scoping

**Description:** Support variable resolution across multiple scopes with proper precedence.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR1.1 | Define and enforce scope hierarchy: Global > Stage > Job > Step | P0 |
| FR1.2 | Resolve variables using scope hierarchy (search from innermost to outermost) | P0 |
| FR1.3 | Support variable overrides at each scope level | P0 |
| FR1.4 | Prevent scope leakage (step vars not visible at stage level) | P0 |
| FR1.5 | Document scope precedence in error messages | P1 |

**Behavior Specification:**

```yaml
# Global scope (file-level)
variables:
  - name: globalVar
    value: "global-value"

stages:
  - stage: Build
    # Stage scope
    variables:
      - name: stageVar
        value: "stage-value"
    jobs:
      - job: BuildJob
        # Job scope (can override globalVar)
        variables:
          - name: globalVar
            value: "job-override"
          - name: jobVar
            value: "job-value"
        steps:
          - script: echo $(globalVar)  # Resolves to "job-override"
          - script: echo $(stageVar)   # Resolves to "stage-value"
          - script: echo $(jobVar)     # Resolves to "job-value"
          # stepVar not available here
          - script: |
              # Step scope
              stepVar=step-value
              echo $stepVar              # Only in this step
```

**Resolution Algorithm:**

```text
function ResolveVariable(name, currentScope):
  if name found in currentScope:
    return value from currentScope
  
  for each scope in precedence order (innermost to outermost):
    if name found in scope:
      return value from scope
  
  if not found anywhere:
    if allowUnresolved flag:
      return ${name} (unresolved marker)
    else:
      throw VariableNotResolvedException(name, currentScope)
```

---

### FR2: Variable File Loading

**Description:** Load variables from external YAML/JSON files with multiple file support.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR2.1 | Load variables from YAML files | P0 |
| FR2.2 | Load variables from JSON files | P0 |
| FR2.3 | Support multiple variable files with merging | P0 |
| FR2.4 | Environment variable substitution in file paths (e.g., `vars/${ENVIRONMENT}.yml`) | P1 |
| FR2.5 | Validate variable file structure | P1 |

**File Format Support:**

```yaml
# YAML format
variables:
  - name: buildConfiguration
    value: Release
  - name: targetPlatform
    value: x64

# JSON format
{
  "variables": [
    { "name": "buildConfiguration", "value": "Release" },
    { "name": "targetPlatform", "value": "x64" }
  ]
}
```

---

### FR3: HTTP Template Resolution

**Description:** Fetch templates from HTTP URLs with intelligent caching and retry logic.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR3.1 | Fetch templates from HTTP/HTTPS URLs | P0 |
| FR3.2 | Cache templates with configurable TTL (default 1 hour) | P0 |
| FR3.3 | Validate ETag and Last-Modified headers for cache revalidation | P0 |
| FR3.4 | Implement exponential backoff retry (100ms → 6.4s, max 3 retries) | P0 |
| FR3.5 | Support proxy configuration | P1 |
| FR3.6 | Timeout after 30 seconds per request | P1 |
| FR3.7 | Circuit breaker pattern for persistent failures | P2 |

**Cache Strategy:**

```
Request Template from URL
  ↓
Check Cache:
  - If TTL expired or not cached → Fetch
  - If TTL valid → Use cached version
  - If ETag available → Validate on server
    - If 304 Not Modified → Use cached
    - If 200 OK → Update cache and use
```

**Retry Policy:**

```
Attempt 1: Fail → Wait 100ms
Attempt 2: Fail → Wait 400ms
Attempt 3: Fail → Wait 1600ms
Attempt 4: Fail → Return error
```

---

### FR4: Template Parameter Handling

**Description:** Support template parameters during resolution.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR4.1 | Parse template parameters from `parameters:` section | P1 |
| FR4.2 | Substitute parameter values in template | P1 |
| FR4.3 | Support default parameter values | P1 |
| FR4.4 | Validate required parameters are provided | P1 |

---

### FR5: Collision Detection

**Description:** Identify variable name conflicts across scopes.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR5.1 | Detect variables with same name defined at multiple scopes | P0 |
| FR5.2 | Warn when variable is overridden at inner scope | P0 |
| FR5.3 | Report collision details (location, scope levels, values) | P0 |
| FR5.4 | Support `--fail-on-collision` flag | P1 |

**Error Example:**

```
⚠ Warning: Variable Collision Detected

Variable: buildNumber
Locations:
  - Global scope (azure-pipelines.yml:5): "$(Build.BuildNumber)"
  - Stage scope (azure-pipelines.yml:15): "123"
  - Job scope (azure-pipelines.yml:42): "124"

Resolution: Using job scope value "124"
Hint: Consider renaming variables to avoid confusion
```

---

### FR6: Circular Reference Detection

**Description:** Identify circular dependencies in variables and templates.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR6.1 | Detect circular variable references (A → B → A) | P0 |
| FR6.2 | Detect circular template includes (Template1 → Template2 → Template1) | P0 |
| FR6.3 | Report cycle path and affected entities | P0 |
| FR6.4 | Fail validation on circular references detected | P0 |
| FR6.5 | Set max depth limit to prevent infinite recursion | P1 |

**Detection Algorithm:**

```
function DetectCycles(graph):
  visited = Set()
  recursionStack = Set()
  cycles = List()
  
  for each node in graph:
    if node not in visited:
      path = DFS(node, visited, recursionStack)
      if path contains cycle:
        cycles.Add(path)
  
  return cycles

function DFS(node, visited, recursionStack):
  visited.Add(node)
  recursionStack.Add(node)
  
  for each dependency in node.Dependencies:
    if dependency not in visited:
      path = DFS(dependency, visited, recursionStack)
      if cycle found in path:
        return path
    else if dependency in recursionStack:
      return BuildCyclePath(dependency, node)
  
  recursionStack.Remove(node)
  return null
```

---

### FR7: Mock Services Layer

**Description:** Simulate Azure DevOps services for local execution context.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR7.1 | Mock variable groups with scoped access | P0 |
| FR7.2 | Mock service connections with credentials | P1 |
| FR7.3 | Mock agent pools with available VM images | P1 |
| FR7.4 | Mock environments with approvers and checks | P1 |
| FR7.5 | Inject mock services into variable resolution | P0 |

**Mock Service Configuration:**

```yaml
mockServices:
  variableGroups:
    - name: "common-vars"
      scope: "Release"
      variables:
        - name: artifactPath
          value: "$(Build.ArtifactStagingDirectory)"
        - name: dropLocation
          isSecret: true
          value: "https://example.blob.core.windows.net"
  
  serviceConnections:
    - name: "azure-subscription"
      type: "azureRM"
      data:
        subscriptionId: "12345678-1234-1234-1234-123456789012"
        tenantId: "abcdefgh-abcd-abcd-abcd-abcdefghijkl"
        clientId: "87654321-4321-4321-4321-210987654321"
  
  agentPools:
    - name: "Azure Pipelines"
      vmImages:
        - "ubuntu-latest"
        - "windows-latest"
        - "macos-latest"
  
  environments:
    - name: "Production"
      approvers:
        - "user@example.com"
      checks: []
```

---

### FR8: Configuration File Support

**Description:** Load configuration from `azp-local.config.yaml` file.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR8.1 | Parse `azp-local.config.yaml` from current directory or specified path | P0 |
| FR8.2 | Merge CLI arguments with configuration file (CLI takes precedence) | P0 |
| FR8.3 | Validate configuration file structure against schema | P1 |
| FR8.4 | Support environment variable substitution in config paths | P1 |
| FR8.5 | Generate scaffold config via `config init` command | P1 |

**Configuration Schema:**

```yaml
# azp-local.config.yaml
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
  # See FR7 for structure

execution:
  agent: "ubuntu-latest"
  timeout: "30m"
  continueOnError: false

output:
  format: "table"
  reportPath: "reports/validate.json"
  color: true

cache:
  schemaTtl: "24h"
  templateTtl: "1h"
  location: ".azp-local/cache"
```

---

### FR9: Expand Command

**Description:** Emit fully expanded YAML with all templates and variables resolved.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR9.1 | Resolve all templates recursively | P0 |
| FR9.2 | Substitute all variables using scope hierarchy | P0 |
| FR9.3 | Preserve YAML structure and formatting | P1 |
| FR9.4 | Optionally preserve comments during expansion | P1 |
| FR9.5 | Support `--emit` flag to write to file | P0 |
| FR9.6 | Include variable resolution map in output | P1 |

**Command Usage:**

```bash
azp-local expand \
  --pipeline azure-pipelines.yml \
  --vars vars/common.yml \
  --emit expanded-pipeline.yml

azp-local expand \
  --pipeline azure-pipelines.yml \
  --preserve-comments \
  --output json > expanded.json
```

---

### FR10: Improved Error Messages

**Description:** Provide clear, actionable error messages with remediation hints.

**Requirements:**

| ID | Requirement | Priority |
|---|---|---|
| FR10.1 | Include error code, message, location, and hint | P0 |
| FR10.2 | Report affected variable/template names | P0 |
| FR10.3 | Suggest remediation steps | P0 |
| FR10.4 | Link to documentation | P1 |
| FR10.5 | Support multiple output formats (text, JSON, SARIF) | P1 |

**Error Format Example:**

```json
{
  "code": "CIRCULAR_REFERENCE",
  "message": "Circular variable reference detected",
  "severity": "error",
  "location": {
    "file": "azure-pipelines.yml",
    "line": 42,
    "column": 5
  },
  "details": {
    "cycle": ["varA", "varB", "varC", "varA"],
    "affectedVariables": ["varA", "varB", "varC"]
  },
  "hint": "Remove the circular dependency by renaming one of: varA, varB, or varC",
  "docsUrl": "https://docs.example.com/errors/CIRCULAR_REFERENCE"
}
```

---

## Non-Functional Requirements

### NFR1: Performance

| Requirement | Target | Validation |
|---|---|---|
| Variable resolution latency | < 100ms per variable | Benchmark suite |
| HTTP template fetch time | < 5 seconds per template | Network simulation tests |
| Cache lookup time | < 10ms per lookup | Profiling |
| Expand command execution | < 5 seconds (1000-step pipeline) | Performance tests |

### NFR2: Reliability

| Requirement | Target | Validation |
|---|---|---|
| Network failure recovery | 3 retries with exponential backoff | Integration tests |
| Cache reliability | Detect and invalidate stale entries | Functional tests |
| Circular reference detection | 100% accuracy on cycles | Comprehensive test suite |

### NFR3: Scalability

| Requirement | Target | Validation |
|---|---|---|
| Max variable count per scope | 10,000+ variables | Load tests |
| Max template nesting depth | 50+ levels (default limit 20) | Recursive template tests |
| Memory usage (1000 variables) | < 50 MB | Memory profiling |

### NFR4: Maintainability

| Requirement | Target | Validation |
|---|---|---|
| Code coverage | ≥ 85% | Coverage reports |
| SOLID principles adherence | No violations | Code review |
| Component cohesion | High cohesion, low coupling | Architectural analysis |

### NFR5: Usability

| Requirement | Target | Validation |
|---|---|---|
| Error message clarity | Actionable hints for 100% of errors | User testing |
| Configuration discoverability | Available options documented | Help text and docs |
| Command performance feedback | User sees progress for long operations | UX testing |

---

## Architecture & Design

### System Architecture Diagram

```text
┌─────────────────────────────────────────────────────────┐
│                    CLI Interface                         │
│           (expand, validate, config, etc)                │
└──────────────────┬──────────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
┌───────▼────────┐   ┌───────▼────────┐
│ Configuration  │   │ YAML Parser     │
│ Manager        │   │ (from Phase 1)  │
└───────┬────────┘   └───────┬────────┘
        │                     │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────────────┐
        │  Variable Processor          │
        │  - Scope Hierarchy           │
        │  - Collision Detection       │
        │  - Substitution Engine       │
        └─────────┬────────┬───────────┘
                  │        │
        ┌─────────▼─┐   ┌──▼──────────┐
        │ Scope     │   │ Circular    │
        │ Manager   │   │ Reference   │
        │           │   │ Detector    │
        └──────────┬┘   └──┬──────────┘
                   │       │
        ┌──────────┴───────┴──────┐
        │                         │
    ┌───▼────┐          ┌────────▼────┐
    │ Template│          │ Mock        │
    │ Resolver│          │ Services    │
    └───┬────┘          │ Layer       │
        │               └─────────────┘
        │
    ┌───▼──────────────┐
    │ HTTP Client      │
    │ - Caching        │
    │ - Retry Logic    │
    │ - Circuit Breaker│
    └──────────────────┘
```

### Component Interaction Flow

```text
User Input (CLI)
    ↓
Configuration Manager
    ├─ Load config file
    ├─ Merge CLI args
    └─ Validate config
    ↓
Variable Processor Initialize
    ├─ Load variable files
    ├─ Load inline variables
    ├─ Load mock variables
    └─ Build scope hierarchy
    ↓
Template Resolver
    ├─ Identify templates
    ├─ Fetch (local/HTTP)
    ├─ Cache management
    └─ Recursive resolution
    ↓
Circular Reference Detector
    ├─ Build dependency graph
    ├─ Detect cycles
    └─ Fail if cycles found
    ↓
Variable Substitution
    ├─ Find $(var) patterns
    ├─ Resolve with hierarchy
    ├─ Detect collisions
    └─ Substitute values
    ↓
Output
    └─ Emit expanded YAML
```

---

## Component Specifications

### Component 1: Variable Processor

**Purpose:** Manage variable loading, scoping, and substitution.

**Responsibilities:**

- Load variables from files (YAML/JSON)
- Build and maintain scope hierarchy
- Resolve variables using scope precedence
- Substitute variables in YAML content
- Detect collisions
- Mask secrets in output

**Interface:**

```csharp
public interface IVariableProcessor
{
    /// <summary>
    /// Add variables from a file (YAML/JSON)
    /// </summary>
    Task LoadVariablesAsync(string filePath, VariableScope scope);

    /// <summary>
    /// Add inline variable
    /// </summary>
    void AddVariable(string name, string value, VariableScope scope);

    /// <summary>
    /// Resolve a variable name using scope hierarchy
    /// </summary>
    VariableResolutionResult ResolveVariable(
        string name, 
        VariableScope currentScope,
        IDictionary<string, string> context);

    /// <summary>
    /// Substitute all variables in text using scope hierarchy
    /// </summary>
    string SubstituteVariables(
        string content,
        VariableScope currentScope,
        IDictionary<string, string> context);

    /// <summary>
    /// Get all variables defined in a scope
    /// </summary>
    IReadOnlyDictionary<string, string> GetScopeVariables(VariableScope scope);

    /// <summary>
    /// Detect variable collisions across scopes
    /// </summary>
    IEnumerable<VariableCollision> DetectCollisions();

    /// <summary>
    /// Clear all loaded variables
    /// </summary>
    void Clear();
}
```

**Data Structures:**

```csharp
public enum VariableScope
{
    Global,
    Stage,
    Job,
    Step
}

public record VariableResolutionResult
{
    public bool Found { get; init; }
    public string Value { get; init; }
    public VariableScope ResolvedFrom { get; init; }
    public IReadOnlyList<string> SearchPath { get; init; }
    public DateTime ResolutionTime { get; init; }
}

public record VariableCollision
{
    public string Name { get; init; }
    public IReadOnlyDictionary<VariableScope, string> Values { get; init; }
    public SourceLocation PrimaryLocation { get; init; }
    public IReadOnlyList<SourceLocation> ConflictLocations { get; init; }
}
```

---

### Component 2: Template Resolver

**Purpose:** Resolve template references (local and HTTP) with intelligent caching.

**Responsibilities:**

- Identify template references in YAML
- Load templates from local files or HTTP URLs
- Validate ETag and Last-Modified headers
- Cache templates with configurable TTL
- Implement retry logic with exponential backoff
- Recursively resolve nested templates

**Interface:**

```csharp
public interface ITemplateResolver
{
    /// <summary>
    /// Resolve a template reference (URL or file path)
    /// </summary>
    Task<TemplateResolutionResult> ResolveAsync(
        string templateReference,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve all templates in YAML content recursively
    /// </summary>
    Task<ExpandedTemplateResult> ResolveAllAsync(
        string yamlContent,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear cache (optionally selective)
    /// </summary>
    void ClearCache(string templateReference = null);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    CacheStatistics GetCacheStats();
}

public record TemplateResolutionContext
{
    public string BaseUri { get; init; }
    public TimeSpan? Timeout { get; init; }
    public int MaxRetries { get; init; }
    public int MaxDepth { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; }
}

public record TemplateResolutionResult
{
    public bool Success { get; init; }
    public string Content { get; init; }
    public SourceLocation SourceLocation { get; init; }
    public TemplateCacheStatus CacheStatus { get; init; }
    public IReadOnlyList<string> NestedTemplates { get; init; }
}

public enum TemplateCacheStatus
{
    CacheHit,
    CacheMiss,
    CacheRevalidated,
    NotCached
}
```

---

### Component 3: Circular Reference Detector

**Purpose:** Identify circular dependencies in variables and templates.

**Responsibilities:**

- Build dependency graph
- Detect cycles using DFS algorithm
- Report cycle path and affected nodes
- Set and enforce maximum depth limit
- Handle both variable and template cycles

**Interface:**

```csharp
public interface ICircularReferenceDetector
{
    /// <summary>
    /// Detect circular variable references
    /// </summary>
    CircularReferenceAnalysis AnalyzeVariables(
        IVariableProcessor variableProcessor);

    /// <summary>
    /// Detect circular template references
    /// </summary>
    Task<CircularReferenceAnalysis> AnalyzeTemplatesAsync(
        ITemplateResolver templateResolver,
        string rootTemplate);

    /// <summary>
    /// Check if a specific path contains a cycle
    /// </summary>
    bool ContainsCycle(IEnumerable<string> path);

    /// <summary>
    /// Get all detected cycles
    /// </summary>
    IReadOnlyList<DetectedCycle> GetDetectedCycles();
}

public record CircularReferenceAnalysis
{
    public bool HasCycles { get; init; }
    public IReadOnlyList<DetectedCycle> Cycles { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> DependencyGraph { get; init; }
}

public record DetectedCycle
{
    public IReadOnlyList<string> Path { get; init; }
    public string CycleType { get; init; } // "Variable" or "Template"
    public IReadOnlyList<SourceLocation> Locations { get; init; }
}
```

---

### Component 4: Collision Detector

**Purpose:** Identify variable name conflicts across scopes.

**Responsibilities:**

- Compare variable names across scope levels
- Report collision details
- Suggest remediation
- Track collision severity

**Interface:**

```csharp
public interface ICollisionDetector
{
    /// <summary>
    /// Analyze variables for collisions
    /// </summary>
    CollisionAnalysisResult Analyze(IVariableProcessor variableProcessor);

    /// <summary>
    /// Check if a collision exists for a variable name
    /// </summary>
    bool HasCollision(string variableName);

    /// <summary>
    /// Get all collisions
    /// </summary>
    IReadOnlyList<VariableCollision> GetCollisions();
}

public record CollisionAnalysisResult
{
    public bool HasCollisions { get; init; }
    public IReadOnlyList<VariableCollision> Collisions { get; init; }
    public CollisionSeverity Severity { get; init; }
}

public enum CollisionSeverity
{
    Info,
    Warning,
    Error
}
```

---

### Component 5: Configuration Manager

**Purpose:** Load and manage configuration from files and CLI.

**Responsibilities:**

- Parse `azp-local.config.yaml`
- Merge CLI arguments with configuration
- Validate configuration structure
- Support environment variable substitution
- Generate configuration scaffolds

**Interface:**

```csharp
public interface IConfigurationManager
{
    /// <summary>
    /// Load configuration from file
    /// </summary>
    Task<ConfigurationSettings> LoadAsync(string configFilePath);

    /// <summary>
    /// Merge CLI arguments with configuration (CLI takes precedence)
    /// </summary>
    ConfigurationSettings Merge(
        ConfigurationSettings fileConfig,
        CLIArgumentsPackage cliArgs);

    /// <summary>
    /// Validate configuration structure
    /// </summary>
    ValidationResult Validate(ConfigurationSettings settings);

    /// <summary>
    /// Generate scaffold configuration
    /// </summary>
    string GenerateScaffold(ConfigurationTemplate template);
}

public record ConfigurationSettings
{
    public PipelineConfiguration Pipeline { get; init; }
    public VariablesConfiguration Variables { get; init; }
    public MockServicesConfiguration MockServices { get; init; }
    public ExecutionConfiguration Execution { get; init; }
    public OutputConfiguration Output { get; init; }
    public CacheConfiguration Cache { get; init; }
}
```

---

### Component 6: Mock Services Layer

**Purpose:** Simulate Azure DevOps services for local execution context.

**Responsibilities:**

- Load mock service configurations
- Provide mock variable groups
- Inject mocked services into variable resolution
- Support service connection simulation
- Manage agent pool specifications

**Interface:**

```csharp
public interface IMockServicesProvider
{
    /// <summary>
    /// Get mock variable group by name
    /// </summary>
    MockVariableGroup GetVariableGroup(string name);

    /// <summary>
    /// Get all available variable groups
    /// </summary>
    IReadOnlyList<MockVariableGroup> GetAllVariableGroups();

    /// <summary>
    /// Get mock service connection by name
    /// </summary>
    MockServiceConnection GetServiceConnection(string name);

    /// <summary>
    /// Get mock agent pool
    /// </summary>
    MockAgentPool GetAgentPool(string name);

    /// <summary>
    /// Get mock environment
    /// </summary>
    MockEnvironment GetEnvironment(string name);

    /// <summary>
    /// Inject mock services into variable processor
    /// </summary>
    void InjectIntoVariableProcessor(IVariableProcessor processor);
}

public record MockVariableGroup
{
    public string Name { get; init; }
    public string Scope { get; init; }
    public IReadOnlyDictionary<string, MockVariable> Variables { get; init; }
}

public record MockVariable
{
    public string Name { get; init; }
    public string Value { get; init; }
    public bool IsSecret { get; init; }
}
```

---

## Interface Contracts

### IVariableProcessor Contract

**Method: ResolveVariable**

```csharp
public VariableResolutionResult ResolveVariable(
    string name, 
    VariableScope currentScope,
    IDictionary<string, string> context)
```

**Preconditions:**

- `name` is not null or empty
- `currentScope` is valid enum value
- `context` can be null (treated as empty)

**Postconditions:**

- Returns `VariableResolutionResult` object
- If found, `Found` is true and `Value` contains resolved value
- If not found, `Found` is false
- `SearchPath` contains scopes checked in order

**Example:**

```csharp
var result = variableProcessor.ResolveVariable(
    "buildConfiguration", 
    VariableScope.Step,
    new Dictionary<string, string> 
    { 
        { "ENVIRONMENT", "Production" }
    });

if (result.Found)
{
    Console.WriteLine($"Value: {result.Value}");
    Console.WriteLine($"From: {result.ResolvedFrom}");
}
```

---

## Data Models

### Variable Scope Hierarchy

```text
┌─────────────────────────────────────┐
│  Global Scope                       │
│  ├─ var1: "global-value"            │
│  └─ var2: "$(system.teamProject)"   │
│                                      │
│  ┌───────────────────────────────┐  │
│  │ Stage: Build                  │  │
│  │ ├─ var1: "build-override"     │  │ (overrides global)
│  │ └─ var3: "stage-value"        │  │ (new at stage)
│  │                               │  │
│  │  ┌──────────────────────────┐ │  │
│  │  │ Job: BuildJob            │ │  │
│  │  │ ├─ var1: "job-override"  │ │  │ (overrides stage)
│  │  │ └─ var4: "job-value"     │ │  │ (new at job)
│  │  │                          │ │  │
│  │  │  ┌────────────────────┐  │ │  │
│  │  │  │ Step 1             │  │ │  │
│  │  │  │ var5: step-value   │  │ │  │ (new at step)
│  │  │  └────────────────────┘  │ │  │
│  │  │                          │ │  │
│  │  │  ┌────────────────────┐  │ │  │
│  │  │  │ Step 2             │  │ │  │
│  │  │  │ (no new variables) │  │ │  │
│  │  │  └────────────────────┘  │ │  │
│  │  └──────────────────────────┘ │  │
│  └───────────────────────────────┘  │
│                                      │
│  ┌───────────────────────────────┐  │
│  │ Stage: Deploy                 │  │
│  │ ├─ var1: "deploy-override"    │  │ (different override)
│  │ └─ var6: "deploy-value"       │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

### Resolution Example

```text
In Step context, resolving "var1":
  Check Step scope → Not found
  Check Job scope → Found "job-override"
  Return "job-override"

In Step context, resolving "var6":
  Check Step scope → Not found
  Check Job scope → Not found
  Check Stage scope → Not found (we're in Build stage, var6 is in Deploy)
  Check Global scope → Not found
  Not found → Return error or unresolved marker
```

---

## TDD Strategy

### Test Structure

```text
tests/
├── Unit/
│   ├── Processor/
│   │   ├── VariableProcessorTests.cs
│   │   ├── VariableScopeTests.cs
│   │   ├── VariableSubstitutionTests.cs
│   │   ├── VariableResolutionTests.cs
│   │   └── VariableCollisionTests.cs
│   ├── Templates/
│   │   ├── TemplateResolverTests.cs
│   │   ├── TemplateHTTPTests.cs
│   │   ├── TemplateCachingTests.cs
│   │   ├── TemplateRetryTests.cs
│   │   └── NestedTemplateTests.cs
│   ├── Dependencies/
│   │   ├── CircularReferenceDetectorTests.cs
│   │   └── CollisionDetectorTests.cs
│   ├── Configuration/
│   │   ├── ConfigurationManagerTests.cs
│   │   └── ConfigurationValidationTests.cs
│   └── MockServices/
│       ├── MockVariableGroupTests.cs
│       └── MockServiceConnectionTests.cs
├── Integration/
│   ├── VariableAndTemplateIntegrationTests.cs
│   ├── CircularReferenceIntegrationTests.cs
│   ├── ConfigurationIntegrationTests.cs
│   └── ExpandCommandTests.cs
└── E2E/
    ├── ComplexPipelineExpansionTests.cs
    ├── MockServicesIntegrationTests.cs
    └── RealWorldPipelineTests.cs
```

### TDD Phases

**Phase 1: Unit Tests (70% of tests)**

```csharp
// Example: Variable Resolution
[Fact]
public void ResolveVariable_WithGlobalVariable_ReturnsGlobalValue()
{
    // Arrange
    var processor = new VariableProcessor();
    processor.AddVariable("buildConfig", "Release", VariableScope.Global);

    // Act
    var result = processor.ResolveVariable("buildConfig", VariableScope.Step, null);

    // Assert
    Assert.True(result.Found);
    Assert.Equal("Release", result.Value);
    Assert.Equal(VariableScope.Global, result.ResolvedFrom);
}

[Fact]
public void ResolveVariable_WithScopeOverride_ReturnsInnerScopeValue()
{
    // Arrange
    var processor = new VariableProcessor();
    processor.AddVariable("var", "global", VariableScope.Global);
    processor.AddVariable("var", "job-level", VariableScope.Job);

    // Act
    var result = processor.ResolveVariable("var", VariableScope.Step, null);

    // Assert
    Assert.True(result.Found);
    Assert.Equal("job-level", result.Value);
    Assert.Equal(VariableScope.Job, result.ResolvedFrom);
}

[Fact]
public void ResolveVariable_NotFound_ReturnsFalse()
{
    // Arrange
    var processor = new VariableProcessor();

    // Act
    var result = processor.ResolveVariable("nonexistent", VariableScope.Step, null);

    // Assert
    Assert.False(result.Found);
}

[Fact]
public void DetectCollisions_WithDuplicateNames_ReportsCollision()
{
    // Arrange
    var processor = new VariableProcessor();
    processor.AddVariable("buildNum", "global", VariableScope.Global);
    processor.AddVariable("buildNum", "job-level", VariableScope.Job);

    // Act
    var collisions = processor.DetectCollisions();

    // Assert
    Assert.Single(collisions);
    Assert.Equal("buildNum", collisions.First().Name);
    Assert.Contains(VariableScope.Global, collisions.First().Values.Keys);
    Assert.Contains(VariableScope.Job, collisions.First().Values.Keys);
}
```

**Phase 2: Integration Tests (20% of tests)**

```csharp
[Fact]
public async Task ExpandCommand_WithVariablesAndTemplates_SubstitutesCorrectly()
{
    // Arrange
    var config = ConfigurationFactory.CreateDefault();
    var processor = new VariableProcessor();
    var resolver = new TemplateResolver(new MockHttpClient());
    
    // Act
    var expanded = await ExpandAsync(
        "pipeline-with-templates.yml",
        processor,
        resolver);

    // Assert
    Assert.DoesNotContain("$(", expanded);  // No unresolved variables
    Assert.DoesNotContain("template:", expanded);  // Templates expanded
}

[Fact]
public async Task CircularReference_InVariablesAndTemplates_DetectedEarly()
{
    // Arrange - Templates: A → B → A; Variables: X → Y → X
    var detector = new CircularReferenceDetector();
    var processor = new VariableProcessor();
    processor.AddVariable("varX", "$(varY)", VariableScope.Global);
    processor.AddVariable("varY", "$(varX)", VariableScope.Global);

    // Act & Assert
    var analysis = detector.AnalyzeVariables(processor);
    Assert.True(analysis.HasCycles);
}
```

**Phase 3: End-to-End Tests (10% of tests)**

```csharp
[Fact]
public async Task RealWorldPipeline_MultiStageWithMultipleVarFiles_Expands()
{
    // Arrange - Real-world scenario with multiple stages, jobs, and variable files
    var pipelineYaml = File.ReadAllText("fixtures/complex-pipeline.yml");
    var varFiles = new[] 
    { 
        "fixtures/vars/common.yml",
        "fixtures/vars/prod.yml" 
    };

    // Act
    var result = await RunExpandCommand(pipelineYaml, varFiles);

    // Assert
    Assert.True(result.Success);
    ValidateExpandedPipeline(result.ExpandedYaml);
}
```

---

## SOLID Principles Application

### S - Single Responsibility Principle

**Applied to:**

- `IVariableProcessor` - Only manages variables, not templates
- `ITemplateResolver` - Only resolves templates, not variables
- `ICircularReferenceDetector` - Only detects cycles, not resolves
- `IConfigurationManager` - Only manages configuration, not execution

**Example:**

```csharp
// ❌ Bad: Multiple responsibilities
public class PipelineExpander
{
    public void Expand()
    {
        // Loads variables
        LoadVariables();
        // Resolves templates
        ResolveTemplates();
        // Detects circular refs
        DetectCircularReferences();
        // Substitutes
        SubstituteVariables();
        // Emits output
        EmitOutput();
    }
}

// ✓ Good: Single responsibility
public class PipelineExpander
{
    private readonly IVariableProcessor _varProcessor;
    private readonly ITemplateResolver _templateResolver;
    private readonly ICircularReferenceDetector _cycleDetector;

    public async Task<ExpandedPipelineResult> ExpandAsync()
    {
        // Delegate to specialized components
        var vars = await _varProcessor.LoadAsync(...);
        var templates = await _templateResolver.ResolveAllAsync(...);
        var analysis = _cycleDetector.Analyze(...);
        return new ExpandedPipelineResult { ... };
    }
}
```

---

### O - Open/Closed Principle

**Applied to:**

- Plugin architecture for template loaders (local, HTTP, custom)
- Variable substitution strategies (built-in patterns, custom patterns)
- Error formatter extensions (text, JSON, SARIF)

**Example:**

```csharp
// ✓ Open for extension, closed for modification
public interface ITemplateLoader
{
    bool CanHandle(string reference);
    Task<string> LoadAsync(string reference);
}

// Local file loader
public class LocalFileTemplateLoader : ITemplateLoader
{
    public bool CanHandle(string reference) => 
        Path.IsPathRooted(reference) || reference.StartsWith("./");
    
    public Task<string> LoadAsync(string reference) => 
        File.ReadAllTextAsync(reference);
}

// HTTP loader
public class HttpTemplateLoader : ITemplateLoader
{
    public bool CanHandle(string reference) => 
        Uri.TryCreate(reference, UriKind.Absolute, out _);
    
    public Task<string> LoadAsync(string reference) => 
        _httpClient.GetStringAsync(reference);
}

// Future: Custom database loader (doesn't require modification)
public class DatabaseTemplateLoader : ITemplateLoader { ... }
```

---

### L - Liskov Substitution Principle

**Applied to:**

- All error types inherit from `ParseError`
- All variable types substitutable in same context
- All template loaders substitutable in resolver

**Example:**

```csharp
// ✓ LSP: All errors can be treated as ParseError
IEnumerable<ParseError> errors = new ParseError[] 
{
    new VariableError { ... },           // Subtype
    new TemplateError { ... },           // Subtype
    new CircularReferenceError { ... }   // Subtype
};

// Can be used interchangeably
foreach (var error in errors)
{
    Console.WriteLine(error.Message);
    Console.WriteLine(error.Hint);
}

// Client doesn't need to know specific types
void ReportErrors(IEnumerable<ParseError> errors) 
{
    foreach (var error in errors)
    {
        _logger.Error(error.Code, error.Message);
    }
}
```

---

### I - Interface Segregation Principle

**Applied to:**

- Separate `IVariableProcessor` from `IVariableResolver`
- Separate read operations (`GetVariable`) from write operations (`AddVariable`)
- Configuration interfaces segregated by concern

**Example:**

```csharp
// ❌ Bad: Too many unrelated methods
public interface IVariableService
{
    void AddVariable(string name, string value);
    VariableResolutionResult ResolveVariable(string name);
    void SaveToFile(string path);
    Dictionary<string, string> GetScopeStatistics();
    void ClearCache();
}

// ✓ Good: Segregated interfaces
public interface IVariableWriter
{
    void AddVariable(string name, string value, VariableScope scope);
    void LoadFromFile(string filePath, VariableScope scope);
}

public interface IVariableReader
{
    VariableResolutionResult ResolveVariable(string name, VariableScope scope);
    IReadOnlyDictionary<string, string> GetScopeVariables(VariableScope scope);
}

public interface IVariableStorage
{
    Task SaveAsync(string filePath);
    Task LoadAsync(string filePath);
}

public interface IVariableAnalytics
{
    Dictionary<string, string> GetStatistics();
    IEnumerable<VariableCollision> GetCollisions();
}

// Client uses only what it needs
public class VariableExpander
{
    public VariableExpander(IVariableReader reader, IVariableWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }
}
```

---

### D - Dependency Inversion Principle

**Applied to:**

- Depend on interfaces, not concrete implementations
- Inject dependencies through constructor
- Configuration through DI container

**Example:**

```csharp
// ✓ Good: Depends on abstractions
public class PipelineExpander
{
    private readonly IVariableProcessor _varProcessor;
    private readonly ITemplateResolver _templateResolver;
    private readonly ICircularReferenceDetector _cycleDetector;
    private readonly ILogger _logger;

    public PipelineExpander(
        IVariableProcessor varProcessor,
        ITemplateResolver templateResolver,
        ICircularReferenceDetector cycleDetector,
        ILogger logger)
    {
        _varProcessor = varProcessor;
        _templateResolver = templateResolver;
        _cycleDetector = cycleDetector;
        _logger = logger;
    }

    public async Task<ExpandedPipelineResult> ExpandAsync(string yaml)
    {
        try
        {
            // Uses abstractions, not concrete types
            var templates = await _templateResolver.ResolveAllAsync(yaml);
            var expanded = _varProcessor.SubstituteVariables(templates);
            var analysis = _cycleDetector.Analyze(_varProcessor);
            
            if (analysis.HasCycles)
            {
                throw new CircularReferenceException(analysis);
            }

            return new ExpandedPipelineResult { Content = expanded };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Expansion failed");
            throw;
        }
    }
}

// DI Registration
services.AddScoped<IVariableProcessor, VariableProcessor>();
services.AddScoped<ITemplateResolver, TemplateResolver>();
services.AddScoped<ICircularReferenceDetector, CircularReferenceDetector>();
services.AddScoped<ITemplateLoader, LocalFileTemplateLoader>();
services.AddScoped<ITemplateLoader, HttpTemplateLoader>();
services.AddScoped<PipelineExpander>();
```

---

## Acceptance Criteria

### AC1: Variable Scoping

```gherkin
Scenario: Resolve variable from job scope
  Given a pipeline with global variable "buildNum" = "1"
    And a job with variable "buildNum" = "2"
  When I resolve "buildNum" in job context
  Then the result should be "2" (job override)
    And resolution source should be "Job" scope

Scenario: Resolve variable from global scope
  Given a pipeline with global variable "environment" = "prod"
    And no stage/job/step override
  When I resolve "environment" in step context
  Then the result should be "prod"
    And resolution source should be "Global" scope

Scenario: Variable not found
  Given a pipeline with no "undefined" variable
  When I resolve "undefined" in any context
  Then result should indicate not found
    And should suggest available variables
```

### AC2: HTTP Template Resolution

```gherkin
Scenario: Fetch template from HTTP URL
  Given an HTTP URL pointing to a template
    And the URL is accessible
  When I resolve the template reference
  Then the template content should be fetched
    And cached for future use
    And resolution time should be < 5 seconds

Scenario: Cache valid template
  Given a template cached with valid ETag
    And the ETag is still valid on server
  When I resolve the same template again
  Then cache should be used (no HTTP request)
    And resolution time should be < 10ms

Scenario: Retry on transient failure
  Given an HTTP request that fails temporarily (500, 503)
  When I resolve the template
  Then should retry with exponential backoff
    And succeed on retry if available
    And report error after 3 failed attempts
```

### AC3: Collision Detection

```gherkin
Scenario: Detect variable collision
  Given global variable "var" = "global"
    And stage variable "var" = "stage"
    And job variable "var" = "job"
  When I detect collisions
  Then should report collision for "var"
    And show all three definitions with locations
    And indicate which value is used (job-level)

Scenario: No collision for different names
  Given global variable "varA" = "value"
    And job variable "varB" = "value"
  When I detect collisions
  Then should report no collisions
```

### AC4: Circular Reference Detection

```gherkin
Scenario: Detect variable circular reference
  Given variables: A → B → C → A
  When I analyze variables
  Then should detect cycle
    And report path: [A, B, C, A]
    And fail validation

Scenario: Detect template circular reference
  Given templates: Template1 → Template2 → Template1
  When I resolve templates
  Then should detect cycle
    And fail before infinite recursion
```

### AC5: Mock Services Integration

```gherkin
Scenario: Load mock variable groups
  Given mock configuration with variable group "common"
    And variable group contains "buildNum" = "123"
  When I load mock services
  Then variable group should be available
    And resolving "buildNum" should return "123"

Scenario: Inject mocks into resolution
  Given mock services configured
    And variable processor initialized
  When I inject mock services
  Then variables from mock groups should be resolvable
    And should follow same scope hierarchy
```

### AC6: Expand Command

```gherkin
Scenario: Expand simple pipeline
  Given a pipeline with variables and local templates
  When I run "expand --pipeline pipeline.yml"
  Then should output fully expanded YAML
    And all $(var) should be substituted
    And all template references should be resolved
    And should not contain "template:" references

Scenario: Expand with multiple variable files
  Given multiple variable files (common.yml, prod.yml)
  When I run "expand --vars vars/common.yml --vars vars/prod.yml"
  Then should merge variables (later files override)
    And should resolve all variables correctly
```

---

## Risk Management

### Risk 1: Performance Degradation with Large Pipelines

**Severity:** MEDIUM

**Probability:** MEDIUM

**Impact:** Users experience slow validation

**Mitigation:**

- Implement caching at multiple levels
- Use async/await for I/O operations
- Set max depth limits (20 levels for templates)
- Profile regularly with benchmark suite

**Contingency:**

- Provide "fast mode" that skips deep analysis
- Report bottlenecks to user

---

### Risk 2: HTTP Template Timeouts

**Severity:** HIGH

**Probability:** MEDIUM

**Impact:** Validation hangs indefinitely

**Mitigation:**

- Set 30-second timeout per HTTP request
- Implement circuit breaker after 3 failures
- Use --offline mode for air-gapped environments
- Cache templates aggressively

**Contingency:**

- Fallback to last known good version
- Clearly indicate timeout vs. network error

---

### Risk 3: Variable Collision Confusion

**Severity:** MEDIUM

**Probability:** LOW

**Impact:** Users unclear which value is used

**Mitigation:**

- Report collisions with explicit precedence
- Warn by default on collision
- Provide --fail-on-collision flag
- Include line numbers and file paths

**Contingency:**

- Suggest renaming to avoid confusion

---

## Implementation Plan

### Sprint 3: Core Variable Processing (Week 1-2)

**Tasks:**

1. **Implement VariableProcessor** (3 days)
   - Variable loading from YAML/JSON
   - Scope hierarchy management
   - Variable substitution engine
   - Unit tests (80+ test cases)

2. **Implement Collision Detector** (2 days)
   - Collision identification
   - Detailed reporting
   - Unit tests (20+ cases)

3. **Configure DI Container** (1 day)
   - Register interfaces/implementations
   - Configure logging

**Deliverables:**

- IVariableProcessor implementation
- 80+ passing unit tests
- Technical spike on performance

---

### Sprint 3: Template Resolution (Week 2-3)

**Tasks:**

1. **Implement HTTP Template Loader** (3 days)
   - HTTP client with retries
   - ETag/Last-Modified caching
   - Timeout handling
   - Unit tests (40+ cases)

2. **Implement Local Template Loader** (1 day)
   - File system loading
   - Path validation

3. **Implement Cache Manager** (2 days)
   - In-memory cache with TTL
   - Cache statistics
   - Cache invalidation

**Deliverables:**

- ITemplateResolver implementation
- ITemplateLoader interface with 2 implementations
- 60+ passing tests
- HTTP timeout handling verified

---

### Sprint 4: Dependencies & Configuration (Week 3-4)

**Tasks:**

1. **Implement Circular Reference Detector** (2 days)
   - DFS-based cycle detection
   - Dependency graph building
   - Unit tests (25+ cases)

2. **Implement Configuration Manager** (2 days)
   - YAML configuration parsing
   - CLI argument merging
   - Validation
   - Unit tests (20+ cases)

3. **Implement Mock Services Layer** (2 days)
   - Mock variable groups
   - Service connection mocking
   - Integration with VariableProcessor
   - Unit tests (15+ cases)

**Deliverables:**

- ICircularReferenceDetector implementation
- IConfigurationManager implementation
- IMockServicesProvider implementation
- 60+ passing tests

---

### Sprint 4: Expand Command & Integration (Week 4)

**Tasks:**

1. **Implement Expand Command** (2 days)
   - YAML emission
   - Integration testing
   - Error handling

2. **Integration Tests** (2 days)
   - Variable + Template integration
   - Circular reference scenarios
   - Mock services integration
   - Real-world pipelines

3. **Documentation & Code Review** (1 day)
   - Architecture documentation
   - API documentation
   - Code review preparation

**Deliverables:**

- Expand command implementation
- 30+ integration tests
- All code reviewed and approved
- Phase 2 complete and tested

---

## Success Criteria

### Functional Criteria

- [ ] All 7 core components implemented and tested
- [ ] 85%+ code coverage achieved
- [ ] 250+ test cases passing (unit + integration)
- [ ] All FR requirements met and verified
- [ ] Expand command fully functional
- [ ] Configuration file parsing working
- [ ] Mock services integrated

### Quality Criteria

- [ ] SOLID principles applied (code review verified)
- [ ] Zero critical bugs
- [ ] Performance targets met
- [ ] All NFRS verified through testing
- [ ] Error messages clear and actionable

### Documentation Criteria

- [ ] All public APIs documented
- [ ] Architecture diagrams included
- [ ] Example configurations provided
- [ ] Troubleshooting guide written
- [ ] Phase 2 complete specification signed off

---

## Conclusion

Phase 2 builds critical infrastructure for variable and template management, establishing the foundation for the execution engine in Phase 3. The focus on SOLID principles and comprehensive testing ensures maintainability and reliability.

**Next Steps:**

1. Review and approve specifications
2. Create test fixtures and scenarios
3. Begin Sprint 3 implementation
4. Establish daily stand-ups
5. Schedule code reviews
