# Phase 1 MVP - Architectural Specification

## ADO Pipelines Local Runner

**Version:** 1.0  
**Date:** December 12, 2025  
**Scope:** Foundation components for YAML validation and expansion

---

## 1. Component Overview

Phase 1 delivers core validation infrastructure with six primary components:

```
┌──────────────────────────────────────────────────────────────┐
│                        CLI Handler                            │
│                    (Entry Point Layer)                        │
└────────────┬─────────────────────────────────────────────────┘
             │
             ├───────────┬────────────┬────────────┬───────────┐
             │           │            │            │           │
             v           v            v            v           v
      ┌──────────┐ ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐
      │  YAML    │ │ Syntax  │ │ Schema  │ │ Template │ │ Variable │
      │  Parser  │ │Validator│ │ Manager │ │ Resolver │ │Processor │
      └──────────┘ └─────────┘ └─────────┘ └──────────┘ └──────────┘
           │           │            │            │           │
           └───────────┴────────────┴────────────┴───────────┘
                                   │
                                   v
                        ┌─────────────────────┐
                        │   Pipeline Model    │
                        │  (Validated AST)    │
                        └─────────────────────┘
```

---

## 2. Component Specifications

### 2.1 CLI Handler

**Responsibility:** Command routing, argument parsing, error presentation

#### Interfaces

```typescript
interface ICliHandler {
    execute(args: string[]): Promise<CommandResult>;
    registerCommand(command: ICommand): void;
}

interface ICommand {
    name: string;
    description: string;
    execute(context: ICommandContext): Promise<CommandResult>;
}

interface ICommandContext {
    args: ParsedArguments;
    fileSystem: IFileSystem;
    logger: ILogger;
}
```

#### SOLID Application

- **SRP**: Delegates command logic to specific command implementations
- **OCP**: New commands added without modifying handler
- **LSP**: All commands implement ICommand contract
- **ISP**: Minimal command interface, context provides dependencies
- **DIP**: Depends on abstractions (ICommand, IFileSystem, ILogger)

#### Design Patterns

- **Command Pattern**: Each CLI command as separate executable object
- **Factory Pattern**: CommandFactory creates appropriate ICommand instances
- **Strategy Pattern**: Different output formats (JSON, text, table)

---

### 2.2 YAML Parser

**Responsibility:** Parse YAML to typed objects, preserve structure

#### Interfaces

```typescript
interface IYamlParser {
    parse<T>(content: string): ParserResult<T>;
    parseFile<T>(path: string): Promise<ParserResult<T>>;
}

interface ParserResult<T> {
    success: boolean;
    data?: T;
    errors: ParseError[];
    sourceMap: SourceMap;
}

interface SourceMap {
    getLineNumber(path: string): number;
    getOriginalLocation(line: number): SourceLocation;
}
```

#### SOLID Application

- **SRP**: Focused solely on YAML-to-object transformation
- **OCP**: Extensible via custom type converters
- **LSP**: Consistent behavior across all parse operations
- **ISP**: Single focused interface, source map separate
- **DIP**: Returns abstract ParserResult, not YAML library specifics

#### Design Patterns

- **Builder Pattern**: Constructs complex ParserResult with fluent API
- **Adapter Pattern**: Wraps underlying YAML library (js-yaml/yaml)

---

### 2.3 Syntax Validator

**Responsibility:** Basic YAML structural validation, ADO-specific syntax rules

#### Interfaces

```typescript
interface ISyntaxValidator {
    validate(content: PipelineDocument): ValidationResult;
    addRule(rule: IValidationRule): void;
}

interface IValidationRule {
    name: string;
    severity: Severity;
    validate(node: any, context: ValidationContext): ValidationError[];
}

interface ValidationResult {
    isValid: boolean;
    errors: ValidationError[];
    warnings: ValidationError[];
}
```

#### SOLID Application

- **SRP**: Validates syntax only, not semantics or schema
- **OCP**: New rules added via addRule, no modification
- **LSP**: All rules follow IValidationRule contract
- **ISP**: Rules don't need full document context
- **DIP**: Validator depends on rule abstraction

#### Design Patterns

- **Strategy Pattern**: Swappable validation rules
- **Chain of Responsibility**: Rules applied sequentially
- **Composite Pattern**: Complex rules composed of simple ones

---

### 2.4 Schema Manager

**Responsibility:** Load ADO schema, provide type definitions, enable IntelliSense

#### Interfaces

```typescript
interface ISchemaManager {
    loadSchema(version: string): Promise<Schema>;
    validateAgainstSchema(document: any, schemaPath: string): SchemaValidationResult;
    getTypeDefinition(typeName: string): TypeDefinition;
}

interface Schema {
    version: string;
    definitions: Map<string, TypeDefinition>;
    validate(data: any): boolean;
}

interface TypeDefinition {
    name: string;
    properties: Map<string, PropertyDefinition>;
    required: string[];
}
```

#### SOLID Application

- **SRP**: Schema loading and validation only
- **OCP**: Support for multiple schema versions via strategy
- **LSP**: All schemas implement Schema interface
- **ISP**: Separate methods for load vs validate vs query
- **DIP**: Returns abstract Schema, not JSON Schema library types

#### Design Patterns

- **Singleton Pattern**: Single schema instance per version
- **Facade Pattern**: Simplifies JSON Schema library complexity
- **Strategy Pattern**: Different validators per schema version

---

### 2.5 Template Resolver

**Responsibility:** Resolve local template references, parameter substitution (Phase 1: local only)

#### Interfaces

```typescript
interface ITemplateResolver {
    resolve(reference: TemplateReference): Promise<ResolvedTemplate>;
    registerResolver(type: string, resolver: IResolver): void;
}

interface IResolver {
    canResolve(reference: TemplateReference): boolean;
    resolve(reference: TemplateReference): Promise<string>;
}

interface TemplateReference {
    path: string;
    parameters: Map<string, any>;
    repository?: string; // Phase 2
}

interface ResolvedTemplate {
    content: string;
    parameters: Map<string, any>;
    dependencies: TemplateReference[];
}
```

#### SOLID Application

- **SRP**: Template location and content resolution only
- **OCP**: New resolver types (GitHub, ADO) added via registration
- **LSP**: All resolvers follow IResolver contract
- **ISP**: IResolver minimal - only resolve capability
- **DIP**: Depends on IResolver abstraction, not file system

#### Design Patterns

- **Strategy Pattern**: Different resolvers for local/remote
- **Factory Pattern**: Create appropriate resolver based on reference type
- **Decorator Pattern**: Add caching to resolvers

---

### 2.6 Variable Processor

**Responsibility:** Simple variable substitution, inline definitions (Phase 1: no variable groups)

#### Interfaces

```typescript
interface IVariableProcessor {
    process(content: string, variables: VariableSet): ProcessResult;
    extractVariables(content: string): string[];
}

interface VariableSet {
    get(name: string): any;
    set(name: string, value: any): void;
    merge(other: VariableSet): VariableSet;
}

interface ProcessResult {
    content: string;
    unresolvedVariables: string[];
}
```

#### SOLID Application

- **SRP**: Variable extraction and substitution only
- **OCP**: Extensible via custom variable sources (Phase 2)
- **LSP**: Consistent behavior regardless of variable source
- **ISP**: Minimal interface - process and extract
- **DIP**: Depends on VariableSet abstraction

#### Design Patterns

- **Strategy Pattern**: Different substitution strategies (${{ }} vs $())
- **Visitor Pattern**: Traverse AST to find variables
- **Template Method**: Define substitution algorithm skeleton

---

## 3. Dependency Injection Architecture

### 3.1 Container Configuration

```typescript
class ServiceContainer {
    // Core services
    register<IYamlParser>(YamlParser);
    register<ISyntaxValidator>(SyntaxValidator);
    register<ISchemaManager>(SchemaManager);
    register<ITemplateResolver>(TemplateResolver);
    register<IVariableProcessor>(VariableProcessor);
    
    // Infrastructure
    register<IFileSystem>(FileSystemService);
    register<ILogger>(ConsoleLogger);
    register<ICache>(MemoryCache);
}
```

### 3.2 Component Dependencies

```md
CLI Handler
  └─→ IFileSystem
  └─→ ILogger
  └─→ CommandFactory
       └─→ IYamlParser
       └─→ ISyntaxValidator
       └─→ ISchemaManager
       └─→ ITemplateResolver
       └─→ IVariableProcessor

Template Resolver
  └─→ IFileSystem
  └─→ IYamlParser
  └─→ ICache

Variable Processor
  └─→ ILogger

Schema Manager
  └─→ ICache
  └─→ IFileSystem
```

---

## 4. Text-Based Component Diagram

```md
                    ┌─────────────────────────┐
                    │     CLI Handler         │
                    │  - Parse arguments      │
                    │  - Route commands       │
                    │  - Format output        │
                    └────────┬────────────────┘
                             │
                ┌────────────┼────────────┐
                │            │            │
        ┌───────▼──────┐ ┌──▼──────┐ ┌──▼──────────┐
        │ ValidateCmd  │ │ExpandCmd│ │  LintCmd    │
        │              │ │         │ │             │
        └───┬──────────┘ └──┬──────┘ └──┬──────────┘
            │               │            │
            └───────────────┼────────────┘
                            │
                ┌───────────┴───────────┐
                │                       │
        ┌───────▼──────┐        ┌──────▼──────┐
        │ YAML Parser  │        │   Schema    │
        │              │        │   Manager   │
        └───────┬──────┘        └──────┬──────┘
                │                      │
                └──────────┬───────────┘
                           │
                ┌──────────▼───────────┐
                │  Syntax Validator    │
                │  - Rules engine      │
                │  - Error collection  │
                └──────────┬───────────┘
                           │
                ┌──────────▼───────────┐
                │  Template Resolver   │
                │  - Local files only  │
                │  - Parameter subst   │
                └──────────┬───────────┘
                           │
                ┌──────────▼───────────┐
                │  Variable Processor  │
                │  - Inline vars       │
                │  - Simple subst      │
                └──────────┬───────────┘
                           │
                ┌──────────▼───────────┐
                │   Pipeline Model     │
                │   (Validated AST)    │
                └──────────────────────┘
```

---

## 5. Key Design Patterns Summary

### 5.1 Factory Pattern

**Usage:** Create commands, resolvers, validators  
**Benefit:** Centralized object creation, easy testing

```typescript
class CommandFactory {
    create(commandName: string): ICommand {
        switch(commandName) {
            case 'validate': return new ValidateCommand(deps);
            case 'expand': return new ExpandCommand(deps);
            case 'lint': return new LintCommand(deps);
        }
    }
}
```

### 5.2 Strategy Pattern

**Usage:** Validation rules, resolvers, output formatters  
**Benefit:** Swap algorithms at runtime, extend without modification

```typescript
class OutputFormatter {
    constructor(private strategy: IFormatStrategy) {}
    
    format(result: CommandResult): string {
        return this.strategy.format(result);
    }
}
```

### 5.3 Builder Pattern

**Usage:** Construct complex validation results, pipeline models  
**Benefit:** Fluent API, immutable objects, clear construction

```typescript
const result = ValidationResultBuilder
    .create()
    .addError(error1)
    .addWarning(warning1)
    .withMetadata(metadata)
    .build();
```

### 5.4 Adapter Pattern

**Usage:** Wrap third-party libraries (js-yaml, ajv)  
**Benefit:** Isolate dependencies, consistent interface

### 5.5 Composite Pattern

**Usage:** Complex validation rules, nested template resolution  
**Benefit:** Uniform treatment of simple/complex structures

---

## 6. Component Interaction Flow

### Validate Command Flow

```md
1. CLI Handler receives 'validate pipeline.yml'
2. CommandFactory creates ValidateCommand
3. ValidateCommand:
   a. IYamlParser.parseFile('pipeline.yml')
   b. ISyntaxValidator.validate(parsedYaml)
   c. ISchemaManager.validateAgainstSchema(parsedYaml)
   d. ITemplateResolver.resolve(templates)
   e. IVariableProcessor.process(content, vars)
4. Return ValidationResult to CLI Handler
5. IOutputFormatter.format(result)
6. Display to user
```

### Template Expansion Flow

```md
1. ITemplateResolver.resolve(templateRef)
2. IResolver.canResolve(templateRef) → LocalFileResolver
3. LocalFileResolver.resolve():
   a. IFileSystem.readFile(path)
   b. IYamlParser.parse(content)
   c. IVariableProcessor.process(params)
4. Recursively resolve nested templates
5. Return ResolvedTemplate with merged content
```

---

## 7. Phase 1 Constraints & Limitations

### In Scope

- ✅ Local YAML file parsing
- ✅ Basic syntax validation
- ✅ Schema validation against ADO spec
- ✅ Local template resolution
- ✅ Inline variable substitution
- ✅ CLI commands: validate, expand, lint

### Out of Scope (Future Phases)

- ❌ Remote template resolution (GitHub, ADO repos)
- ❌ Variable group mocking
- ❌ Service connection simulation
- ❌ Task execution
- ❌ Condition evaluation
- ❌ Advanced expressions (${{ }} with functions)

---

## 8. Technology Stack

### Core

- **Language:** TypeScript 5.3+
- **Runtime:** Node.js 20 LTS
- **YAML Parser:** js-yaml 4.1+
- **Schema Validation:** ajv 8.12+
- **CLI Framework:** commander 11.1+

### Development

- **Testing:** Jest 29+
- **DI Container:** tsyringe 4.8+
- **Build:** esbuild / webpack
- **Linting:** ESLint 8+

---

## 9. Success Criteria

Phase 1 MVP complete when:

1. ✅ Can parse valid ADO YAML pipelines
2. ✅ Detects syntax errors with line numbers
3. ✅ Validates against ADO schema
4. ✅ Resolves local templates recursively
5. ✅ Substitutes inline variables
6. ✅ Expands pipeline to single merged YAML
7. ✅ All components follow SOLID principles
8. ✅ 80%+ unit test coverage
9. ✅ CLI installable via npm/yarn
10. ✅ Documentation complete

---

## 10. Next Steps

1. **Setup project structure** with chosen tech stack
2. **Implement core interfaces** in TypeScript
3. **Build YAML Parser** with source map support
4. **Develop Syntax Validator** with extensible rules
5. **Integrate Schema Manager** with ADO schema
6. **Create Template Resolver** for local files
7. **Build Variable Processor** for simple substitution
8. **Develop CLI Handler** with commands
9. **Write comprehensive tests** for each component
10. **Create user documentation** and examples

---

**Document Status:** Ready for implementation  
**Review Date:** December 12, 2025  
**Approved By:** Technical Lead
