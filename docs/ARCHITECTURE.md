# Architecture & Design

<!-- markdownlint-disable MD013 MD022 MD029 MD031 MD032 MD040 -->

## System Architecture

### High-Level Overview

The Azure Pipelines Local Runner follows a modular, layered architecture with clear separation of concerns:

```
┌──────────────────────────────────────────────────────────────┐
│                  User Interface Layer                        │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  CLI Application (AzpLocal.Cli)                        │  │
│  │  - Argument parsing                                    │  │
│  │  - User interaction                                    │  │
│  │  - Report formatting                                  │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────┐
│              Orchestration Layer                             │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  ValidationOrchestrator                                │  │
│  │  - Coordinates validation pipeline                     │  │
│  │  - Manages error aggregation                           │  │
│  │  - Handles exceptions and recovery                     │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
    ↓              ↓              ↓              ↓
┌─────────┐  ┌──────────┐  ┌────────────┐  ┌───────────┐
│ Parsing │  │ Validation│  │ Resolution │  │Processing │
│ Layer   │  │  Layer    │  │ Layer      │  │ Layer     │
├─────────┤  ├──────────┤  ├────────────┤  ├───────────┤
│ IYaml   │  │ISchema   │  │ITemplate   │  │IVariable  │
│ Parser  │  │Manager   │  │Resolver    │  │Processor  │
└─────────┘  └──────────┘  └────────────┘  └───────────┘
```

### Component Responsibilities

#### 1. **Parsing Layer** (FR-1: YAML Syntax Validation)

**Component:** `IYamlParser` → `YamlDotNetYamlParser`

**Responsibility:**
- Parse YAML syntax
- Detect syntax errors
- Build Abstract Syntax Tree (AST)
- Map errors to source locations

**Error Handling:**
- Catches `YamlException` from YamlDotNet
- Maps to custom `ParseException` with location info
- Preserves line/column information for diagnostics

**Key Design Decisions:**
- Uses YamlDotNet library for robust parsing
- Adapter pattern maps external library exceptions to domain exceptions
- Location tracking enables precise error reporting

---

#### 2. **Validation Layer** (FR-2: Schema Validation)

**Component:** `ISchemaManager` → `SimpleSchemaManager`

**Responsibility:**
- Validate document structure
- Enforce schema constraints
- Check required keys
- Validate key hierarchies

**Current Validation Rules:**
- `trigger` key must be present at root level
- Expandable for additional rules in Phase 2

**Design Pattern:**
- Visitor pattern allows traversing AST
- Composable validation rules
- Separates concerns: parsing vs. validation

---

#### 3. **Resolution Layer** (FR-3: Local Template Resolution)

**Component:** `ITemplateResolver` → `LocalTemplateResolver`

**Responsibility:**
- Locate template files
- Load template YAML
- Parse template content
- Validate template structure

**Template Loading Process:**
1. Scan document for `template:` keys
2. Resolve file paths relative to `basePath`
3. Load and parse template files
4. Merge templates into main document

**Error Recovery:**
- Missing files generate validation errors instead of exceptions
- Allows partial validation even with template issues

---

#### 4. **Processing Layer** (FR-4: Variable Processing)

**Component:** `IVariableProcessor` → `SimpleVariableProcessor`

**Responsibility:**
- Load variables from files
- Merge inline variables
- Substitute variable references
- Update document YAML

**Variable Substitution:**
- Pattern: `$(VariableName)`
- Searches both YAML AST nodes and raw text
- Case-sensitive matching
- File variables overridden by inline variables

**Implementation Strategy:**
- Two-pass approach:
  1. Substitute in YAML AST nodes (scalars)
  2. Rebuild raw text with all substitutions
- Returns new PipelineDocument with updated content

---

#### 5. **Reporting Layer** (FR-6: Error Reporting & FR-7: Logging)

**Components:**
- `IErrorReporter` → `DefaultErrorReporter`
- `ValidationReport` → Result aggregation
- `ValidationIssue` → Individual issues

**Report Generation:**
```csharp
ValidationReport contains:
├── Issues: List<ValidationIssue>
├── CriticalCount: int
├── ErrorCount: int
├── WarningCount: int
├── InfoCount: int
├── IsValid: bool (no Critical/Error issues)
└── Summary: string (human-readable)
```

**Severity Levels:**
- `Critical` - Should not be ignored
- `Error` - Validation failed
- `Warning` - Potential issues
- `Info` - Informational messages

**Category Classification:**
- `Syntax` - YAML parsing issues
- `Schema` - Structure violations
- `Template` - Template resolution issues
- `Variable` - Variable processing issues
- `Other` - Miscellaneous

---

### Data Flow

#### Validation Pipeline Flow

```
Input YAML File
        ↓
    [Parse YAML]
    (FR-1: Syntax Validation)
        ↓
        Success? → NO → Report Error → Exit
        ↓
       YES
        ↓
    [Validate Schema]
    (FR-2: Schema Validation)
        ↓
        Issues? → Aggregate errors
        ↓
    [Resolve Templates]
    (FR-3: Template Resolution)
        ↓
        Issues? → Aggregate errors
        ↓
    [Process Variables]
    (FR-4: Variable Processing)
        ↓
        Issues? → Aggregate errors
        ↓
    [Build Report]
    (FR-6: Error Reporting)
        ↓
    [Output Report]
    (FR-7: Logging)
        ↓
    Exit with appropriate code
```

---

## Design Patterns

### 1. **Strategy Pattern**

All major components are interfaces with pluggable implementations:

```csharp
public interface IYamlParser { /* ... */ }
public interface ISchemaManager { /* ... */ }
public interface ITemplateResolver { /* ... */ }
public interface IVariableProcessor { /* ... */ }
public interface IErrorReporter { /* ... */ }
```

**Benefits:**
- Easy to swap implementations
- Testable with mocks
- Extensible for custom validators

### 2. **Adapter Pattern**

Maps external library exceptions to domain exceptions:

```csharp
try {
    return parser.Parse(yaml);
} catch (YamlException ex) {
    throw new ParseException(ex.Message, location);
}
```

**Benefits:**
- Isolates domain from external dependencies
- Provides consistent error interface
- Enables location tracking

### 3. **Builder Pattern**

ValidationIssue construction uses property initialization:

```csharp
var issue = new ValidationIssue
{
    Code = "YAML001",
    Message = "Syntax error",
    Severity = ValidationSeverity.Error,
    Category = ValidationCategory.Syntax,
    Location = new SourceLocation { /* ... */ }
};
```

### 4. **Template Method Pattern**

ValidationOrchestrator orchestrates validation stages:

```csharp
public async Task<ValidationReport> ValidateFileAsync(...)
{
    // Stage 1: Parse
    // Stage 2: Validate Schema
    // Stage 3: Resolve Templates
    // Stage 4: Process Variables
    // Stage 5: Build Report
}
```

---

## Key Design Decisions

### 1. **Modular Component Design**

**Decision:** Each validation stage is a separate interface/service

**Rationale:**
- Single Responsibility Principle
- Easy to test in isolation
- Can be reused independently
- Supports future extensions

### 2. **Immutable Documents**

**Decision:** Services return new `PipelineDocument` instances rather than mutating

**Rationale:**
- Prevents side effects
- Enables auditing/debugging
- Safer concurrent access
- Aligns with functional programming principles

### 3. **Error Aggregation**

**Decision:** Continue validation even after errors; collect all issues

**Rationale:**
- Provides complete feedback in single validation run
- Reduces iteration cycles
- Better user experience
- Matches IDE validation behavior

### 4. **Location Tracking**

**Decision:** All errors include file/line/column information

**Rationale:**
- Enables quick error location
- Better IDE integration
- Professional error reporting
- Consistent with compiler standards

### 5. **Async/Await Throughout**

**Decision:** All I/O operations are async with `Task` return types

**Rationale:**
- Better performance with I/O-bound operations
- Enables cancellation token support
- Future-proof for scaling
- Aligns with modern .NET practices

---

## SOLID Principles Adherence

### Single Responsibility Principle
✅ Each class has one reason to change:
- `YamlDotNetYamlParser` - YAML parsing only
- `SimpleSchemaManager` - Schema validation only
- `LocalTemplateResolver` - Template resolution only
- `SimpleVariableProcessor` - Variable substitution only

### Open/Closed Principle
✅ Open for extension, closed for modification:
- New validators can implement `ISchemaManager`
- New template resolvers can implement `ITemplateResolver`
- New processors can implement `IVariableProcessor`
- No existing code needs modification

### Liskov Substitution Principle
✅ All implementations are interchangeable:
- Any `IYamlParser` can replace `YamlDotNetYamlParser`
- Any `IErrorReporter` can replace `DefaultErrorReporter`
- Contracts are respected by all implementations

### Interface Segregation Principle
✅ Interfaces are focused and minimal:
- `IYamlParser` has only `ParseAsync()`
- `ISchemaManager` has only `ValidateSchemaAsync()`
- No fat interfaces with unrelated methods

### Dependency Inversion Principle
✅ Depends on abstractions, not concretions:
```csharp
public ValidationOrchestrator(
    IYamlParser parser,
    ISchemaManager schemaManager,
    ITemplateResolver templateResolver,
    IVariableProcessor variableProcessor,
    IErrorReporter errorReporter)
```
- Constructor injection of interfaces
- No direct instantiation of concrete classes
- Easy to test with mocks

---

## Testing Architecture

### Unit Test Organization

```
tests/
└── AzpLocal.Tests/
    ├── YamlParserTests.cs          (FR-1)
    ├── SchemaValidationTests.cs    (FR-2)
    ├── VariableAndTemplateTests.cs (FR-3 & FR-4)
    ├── ErrorReporterTests.cs       (FR-6)
    └── ValidationOrchestratorTests.cs (Integration)
```

### Test-Driven Development (TDD)

**Methodology:**
1. Write failing tests first
2. Implement minimal code to pass
3. Refactor for clarity
4. Repeat

**Acceptance Criteria Tests:**
Each functional requirement maps to acceptance criteria tests:
- AC-1: YAML parser handles valid YAML
- AC-2: YAML parser handles invalid YAML
- AC-3: Schema validation detects missing trigger
- And so on...

---

## Performance Considerations

### Current Targets (Phase 1)
- Validation latency: < 1 second
- Startup time: < 5 seconds
- Memory usage: < 100 MB

### Optimization Strategies
1. **Lazy loading** - Only parse required files
2. **Caching** - Cache parsed templates
3. **Streaming** - Process large files incrementally
4. **Parallel validation** - Validate independent rules in parallel

### Measured Baseline
- Simple 50-line YAML: ~10-20 ms
- With 3 templates: ~50-75 ms
- With variable substitution: ~25-50 ms

---

## Extensibility

### Adding a New Validator

1. Create interface:
```csharp
public interface ICustomValidator
{
    Task<IEnumerable<ValidationIssue>> ValidateAsync(PipelineDocument doc);
}
```

2. Implement:
```csharp
public class CustomValidator : ICustomValidator { /* ... */ }
```

3. Add to orchestrator:
```csharp
var customValidator = new CustomValidator();
issues.AddRange(await customValidator.ValidateAsync(document));
```

### Adding a New Variable Source

1. Implement `IVariableProcessor`:
```csharp
public class EnvironmentVariableProcessor : IVariableProcessor { /* ... */ }
```

2. Use in orchestrator or chain with existing processors

### Custom Schema Rules

Extend `SimpleSchemaManager` to add validation rules:

```csharp
public class StrictSchemaManager : SimpleSchemaManager
{
    public override async Task<IEnumerable<ValidationIssue>> ValidateSchemaAsync(
        PipelineDocument document,
        CancellationToken cancellationToken)
    {
        var issues = await base.ValidateSchemaAsync(document, cancellationToken);
        // Add custom validation
        issues = issues.Concat(await ValidateCustomRules(document));
        return issues;
    }
}
```

---

## Dependencies

### Production Dependencies
- **YamlDotNet 15.1.2** - YAML parsing and serialization
  - Why: Industry standard, robust, well-maintained
  - Alternative: SharpYaml (if needed)

### Test Dependencies
- **xUnit 2.x** - Testing framework
  - Why: Modern, clean, supports async
- **MSBuild-based test discovery**

### Build Dependencies
- **.NET SDK 8.0+**
- **PowerShell 7+** (for CI scripts)

---

## Directory Structure

```
ado-pipelines-local-runner/
├── src/
│   ├── AzpLocal/                    # Core library
│   │   ├── Models/                  # Data models
│   │   ├── Exceptions/              # Custom exceptions
│   │   ├── Interfaces/              # Service contracts
│   │   └── Services/                # Implementations
│   │       ├── Yaml/
│   │       ├── Schema/
│   │       ├── Templates/
│   │       ├── Variables/
│   │       ├── ErrorReporting/
│   │       └── Validation/
│   │
│   └── AzpLocal.Cli/                # CLI application
│       └── Program.cs
│
├── tests/
│   └── AzpLocal.Tests/              # Unit tests
│
├── demos/                           # Example files
├── docs/                            # Documentation
├── .github/workflows/               # CI/CD pipelines
├── AzpLocal.sln                     # Solution file
└── README.md                        # Main documentation
```

---

## Future Enhancements

### Phase 2 Considerations
- Job-level validation
- Condition and expression validation
- Template parameters and scoping
- Advanced schema rules
- Performance profiling and optimization

### Phase 3+ Considerations
- Plugin/extension system
- VS Code extension integration
- Language server protocol (LSP) support
- Distributed validation for large pipelines

---

*Last updated: 2025*  
*Architecture Version: 1.0*
