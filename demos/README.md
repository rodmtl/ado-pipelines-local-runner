# Azure Pipelines Local Runner - Demo Files

This directory contains demo files showcasing each functional requirement of the ADO Pipelines Local Runner Phase 1 MVP.

## FR-1: YAML Syntax Validation

**File:** `01-syntax-validation.yml`

Demonstrates a valid Azure Pipelines YAML file with proper syntax including:

- Trigger configuration
- Pull request validation
- Variables declaration
- Multiple jobs with steps
- Task references

**Test:**

```bash
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/01-syntax-validation.yml
```

Expected output: ✓ PASSED - No syntax errors

---

## FR-2: Schema Validation

**File:** `02-schema-validation.yml`

Demonstrates schema validation where the `trigger` key is required at the root level.

**Test:**

```bash
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/02-schema-validation.yml
```

Expected output: ✓ PASSED - Schema validation successful

**Test Invalid Schema (intentional):**

```bash
# Create a file without 'trigger' key and test it
echo 'jobs:\n  - job: Test\n    steps:\n      - script: echo Test' > test.yml
dotnet run --project src/AzpLocal.Cli -- --pipeline test.yml
```

Expected output: ✗ FAILED - Missing required 'trigger' key

---

## FR-3: Local Template Resolution

**Files:**

- `03-template-reference.yml` - Main pipeline that references a template
- `03-template.yml` - The template file

Demonstrates loading and validating local template references.

**Test:**

```bash
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/03-template-reference.yml
```

Expected output: ✓ PASSED - Template loaded and resolved successfully

---

## FR-4: Variable Processing

**Files:**

- `04-variables.yml` - Pipeline with variable placeholders using `$(varName)` syntax
- `04-variables-input.yml` - Variable definitions file

Demonstrates variable substitution and processing.

**Test with inline variables:**

```bash
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/04-variables.yml \
  --var BuildConfiguration=Release \
  --var BuildVersion=1.0.0 \
  --var Environment=Production
```

**Test with variable file:**

```bash
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/04-variables.yml \
  --vars demos/04-variables-input.yml
```

Expected output: ✓ PASSED - Variables substituted successfully

---

## FR-5: CLI `validate` Command

**File:** `05-cli-demo.yml`

Demonstrates the command-line interface for the validate command.

**Test 1: Basic validation:**

```bash
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/05-cli-demo.yml
```

**Test 2: Validation with output file (for FR-7 logging):**

```bash
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/05-cli-demo.yml \
  --output report.txt
cat report.txt
```

**Test 3: Strict mode (fails on warnings):**

```bash
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/05-cli-demo.yml \
  --strict
```

---

## FR-6: Structured Error Reporting

All tests above demonstrate error reporting with structured output showing:

- Severity level (Critical, Error, Warning, Info)
- Error code (e.g., YAML001, SCHEMA001)
- Descriptive message
- Category (Syntax, Schema, Template, Variable, Other)
- Source location (file, line, column)

The report includes a summary count of issues by severity level.

---

## FR-7: Logging

Logging is demonstrated through the `--output` option which writes structured validation reports to files.

**Test:**

```bash
# Run validation and capture output to file
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/01-syntax-validation.yml \
  --output validation-log.txt

# View the log
cat validation-log.txt
```

The output includes:

- Validation status (PASSED/FAILED)
- Summary of issues by severity
- Detailed list of all issues with line/column information
- Timestamp context (via file creation time)

---

## Running All Demos

**Execute all demo validations at once:**

```bash
echo "=== FR-1: YAML Syntax ==="
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/01-syntax-validation.yml

echo -e "\n=== FR-2: Schema Validation ==="
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/02-schema-validation.yml

echo -e "\n=== FR-3: Template Resolution ==="
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/03-template-reference.yml

echo -e "\n=== FR-4: Variables (File) ==="
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/04-variables.yml \
  --vars demos/04-variables-input.yml

echo -e "\n=== FR-4: Variables (Inline) ==="
dotnet run --project src/AzpLocal.Cli -- \
  --pipeline demos/04-variables.yml \
  --var BuildConfiguration=Debug \
  --var BuildVersion=2.0.0 \
  --var Environment=Development

echo -e "\n=== FR-5/FR-6/FR-7: CLI Demo ==="
dotnet run --project src/AzpLocal.Cli -- --pipeline demos/05-cli-demo.yml --output demo-report.txt
```

---

## Notes

- All demos use valid Azure Pipelines syntax
- Variable names in demos use Azure Pipelines convention: `$(variableName)`
- Template paths are relative to the repository root
- The validator checks for:
  - Valid YAML syntax (FR-1)
  - Required schema elements like `trigger` (FR-2)
  - Template file existence and loadability (FR-3)
  - Variable substitution patterns (FR-4)
