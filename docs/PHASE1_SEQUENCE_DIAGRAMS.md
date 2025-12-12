# Phase 1 MVP - Sequence Diagrams

**Version:** 1.0  
**Date:** December 12, 2025  
**Format:** Text-based ASCII diagrams

---

## 1. Main Validation Flow

```
┌──────┐     ┌────────────┐     ┌─────────────┐     ┌──────────────┐     ┌─────────────────┐     ┌──────────────────┐     ┌────────┐
│ CLI  │     │ YamlParser │     │SyntaxValidator│    │SchemaManager │     │TemplateResolver │     │VariableProcessor │     │ Result │
└──┬───┘     └─────┬──────┘     └──────┬──────┘     └──────┬───────┘     └────────┬────────┘     └────────┬─────────┘     └───┬────┘
   │                │                   │                   │                      │                       │                   │
   │  validate()    │                   │                   │                      │                       │                   │
   │───────────────>│                   │                   │                      │                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │  parseFile()      │                   │                      │                       │                   │
   │                │──────────┐        │                   │                      │                       │                   │
   │                │          │        │                   │                      │                       │                   │
   │                │<─────────┘        │                   │                      │                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │  ParserResult     │                   │                      │                       │                   │
   │                │──────────────────>│                   │                      │                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │  validate()       │                      │                       │                   │
   │                │                   │──────────┐        │                      │                       │                   │
   │                │                   │          │        │                      │                       │                   │
   │                │                   │<─────────┘        │                      │                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │  ValidationResult │                      │                       │                   │
   │                │                   │──────────────────>│                      │                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │                   │  validateSchema()    │                       │                   │
   │                │                   │                   │─────────────┐        │                       │                   │
   │                │                   │                   │             │        │                       │                   │
   │                │                   │                   │<────────────┘        │                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │                   │  SchemaResult        │                       │                   │
   │                │                   │                   │─────────────────────>│                       │                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │                   │                      │  resolveTemplates()   │                   │
   │                │                   │                   │                      │──────────────┐        │                   │
   │                │                   │                   │                      │              │        │                   │
   │                │                   │                   │                      │<─────────────┘        │                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │                   │                      │  ResolvedPipeline     │                   │
   │                │                   │                   │                      │──────────────────────>│                   │
   │                │                   │                   │                      │                       │                   │
   │                │                   │                   │                      │                       │  processVars()    │
   │                │                   │                   │                      │                       │──────────┐        │
   │                │                   │                   │                      │                       │          │        │
   │                │                   │                   │                      │                       │<─────────┘        │
   │                │                   │                   │                      │                       │                   │
   │                │                   │                   │                      │                       │  FinalPipeline    │
   │                │                   │                   │                      │                       │──────────────────>│
   │                │                   │                   │                      │                       │                   │
   │  CommandResult │                   │                   │                      │                       │                   │
   │<───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────│
   │                │                   │                   │                      │                       │                   │
```

**Flow Steps:**

1. CLI receives `validate` command with pipeline file path
2. YamlParser reads and parses the YAML file into a PipelineDocument
3. SyntaxValidator checks basic YAML structure and ADO syntax rules
4. SchemaManager validates against ADO pipeline schema definitions
5. TemplateResolver expands any template references (local files in Phase 1)
6. VariableProcessor resolves variables and expressions
7. Result aggregated and returned to CLI for display

---

## 2. Error Handling Flow

```
┌──────────┐     ┌───────────┐     ┌─────────────────┐     ┌─────────────┐     ┌────────┐
│Component │     │ErrorContext│    │ErrorAggregator  │     │ErrorHandler │     │ Output │
└────┬─────┘     └─────┬─────┘     └────────┬────────┘     └──────┬──────┘     └───┬────┘
     │                 │                     │                     │                │
     │  Operation      │                     │                     │                │
     │────────┐        │                     │                     │                │
     │        │        │                     │                     │                │
     │<───────┘        │                     │                     │                │
     │                 │                     │                     │                │
     │  [Error Occurs] │                     │                     │                │
     │                 │                     │                     │                │
     │  createError()  │                     │                     │                │
     │────────────────>│                     │                     │                │
     │                 │                     │                     │                │
     │                 │  addError()         │                     │                │
     │                 │────────────────────>│                     │                │
     │                 │                     │                     │                │
     │                 │                     │  [Continue or Fail]│                │
     │                 │                     │────────────┐        │                │
     │                 │                     │            │        │                │
     │                 │                     │<───────────┘        │                │
     │                 │                     │                     │                │
     │                 │                     │  handleErrors()     │                │
     │                 │                     │────────────────────>│                │
     │                 │                     │                     │                │
     │                 │                     │                     │  categorize()  │
     │                 │                     │                     │───────┐        │
     │                 │                     │                     │       │        │
     │                 │                     │                     │<──────┘        │
     │                 │                     │                     │                │
     │                 │                     │                     │  format()      │
     │                 │                     │                     │───────────────>│
     │                 │                     │                     │                │
     │                 │                     │                     │  ErrorReport   │
     │<────────────────────────────────────────────────────────────────────────────│
     │                 │                     │                     │                │
```

**Error Types:**

```
ErrorType
├── ParseError              (YAML syntax errors)
│   ├── MalformedYaml
│   ├── InvalidEncoding
│   └── UnexpectedToken
│
├── ValidationError         (Schema/syntax violations)
│   ├── MissingRequired
│   ├── InvalidType
│   ├── ConstraintViolation
│   └── UnknownProperty
│
├── TemplateError          (Template resolution issues)
│   ├── TemplateNotFound
│   ├── CircularReference
│   ├── ParameterMissing
│   └── InvalidParameters
│
└── VariableError          (Variable processing issues)
    ├── UndefinedVariable
    ├── ExpressionError
    ├── TypeMismatch
    └── CircularDependency
```

**Error Handling Strategy:**

1. **Fail Fast:** Parse errors stop immediately
2. **Collect All:** Validation errors collected and reported together
3. **Context Rich:** Each error includes:
   - File path and line number
   - Error code and severity
   - Descriptive message
   - Suggested fix (when applicable)
4. **Exit Codes:**
   - 0: Success
   - 1: Validation errors
   - 2: Parse errors
   - 3: System errors

---

## 3. Template Resolution Flow (Local Files)

```
┌────────────────┐     ┌──────────┐     ┌──────────────┐     ┌─────────────┐     ┌─────────────┐
│TemplateResolver│     │FileSystem│     │ YamlParser   │     │ Validator   │     │MergedPipeline│
└───────┬────────┘     └─────┬────┘     └──────┬───────┘     └──────┬──────┘     └──────┬──────┘
        │                    │                  │                    │                   │
        │  resolveTemplates()│                  │                    │                   │
        │───────────┐        │                  │                    │                   │
        │           │        │                  │                    │                   │
        │<──────────┘        │                  │                    │                   │
        │                    │                  │                    │                   │
        │  [Scan for template: refs]            │                    │                   │
        │───────────┐        │                  │                    │                   │
        │           │        │                  │                    │                   │
        │<──────────┘        │                  │                    │                   │
        │                    │                  │                    │                   │
        │  readFile(path)    │                  │                    │                   │
        │───────────────────>│                  │                    │                   │
        │                    │                  │                    │                   │
        │  templateContent   │                  │                    │                   │
        │<───────────────────│                  │                    │                   │
        │                    │                  │                    │                   │
        │  parse(content)    │                  │                    │                   │
        │───────────────────────────────────────>│                    │                   │
        │                    │                  │                    │                   │
        │  ParsedTemplate    │                  │                    │                   │
        │<───────────────────────────────────────│                    │                   │
        │                    │                  │                    │                   │
        │  validateTemplate()│                  │                    │                   │
        │───────────────────────────────────────────────────────────>│                   │
        │                    │                  │                    │                   │
        │  ValidationResult  │                  │                    │                   │
        │<───────────────────────────────────────────────────────────│                   │
        │                    │                  │                    │                   │
        │  [Check for nested templates]          │                    │                   │
        │───────────┐        │                  │                    │                   │
        │           │        │                  │                    │                   │
        │<──────────┘        │                  │                    │                   │
        │                    │                  │                    │                   │
        │  [Recursive: resolve nested]           │                    │                   │
        │───────────┐        │                  │                    │                   │
        │           │        │                  │                    │                   │
        │<──────────┘        │                  │                    │                   │
        │                    │                  │                    │                   │
        │  mergeTemplates()  │                  │                    │                   │
        │───────────┐        │                  │                    │                   │
        │           │        │                  │                    │                   │
        │<──────────┘        │                  │                    │                   │
        │                    │                  │                    │                   │
        │  ResolvedPipeline  │                  │                    │                   │
        │───────────────────────────────────────────────────────────────────────────────>│
        │                    │                  │                    │                   │
```

**Template Resolution Steps:**

1. **Scan Pipeline:** Identify all `template:` references in the pipeline
2. **Validate Path:** Ensure template path is:
   - Relative to pipeline file
   - Local file (no remote URLs in Phase 1)
   - Within allowed directory (security check)
3. **Load Template:** Read template file from file system
4. **Parse Template:** Parse YAML into typed template object
5. **Validate Template:** Ensure template structure is valid
6. **Check Nested:** Recursively resolve any templates within the template
7. **Merge Content:** Insert template content into main pipeline
8. **Preserve Context:** Track source locations for error reporting

**Template Constraints (Phase 1):**

- ✅ Local file system only
- ✅ Relative paths
- ✅ `.yml` and `.yaml` extensions
- ✅ Maximum depth: 5 levels
- ❌ No remote repositories
- ❌ No repository resources
- ❌ No dynamic template selection

**Circular Reference Detection:**

```
Resolution Stack:
├─ main.yml
│  ├─ templates/build.yml
│  │  ├─ templates/common.yml  ✓
│  │  └─ templates/build.yml   ✗ CIRCULAR!
│  └─ templates/test.yml       ✓
```

---

## Notes

- All diagrams use ASCII art for maximum compatibility
- Flow shows happy path; errors branch to Error Handling Flow
- Phase 1 focuses on local file operations only
- Each component interaction is synchronous in MVP
- Error context is preserved through entire validation chain
