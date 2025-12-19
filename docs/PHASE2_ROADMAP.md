# Phase 2.1 & Phase 3 Implementation Roadmap

**Created:** December 19, 2025  
**Status:** Planning phase  
**Updated by:** GitHub Copilot

---

## Completed ✅

| ID | Feature | Status | Tests | Code LOC |
|---|---|---|---|---|
| FR1 | Hierarchical Variable Scoping | ✅ Complete | 6 | ~50 |
| FR2 | Variable File Loading (YAML/JSON) | ✅ Complete | 8 | ~120 |
| FR3 | HTTP Template Resolution | ✅ Complete | 7 | ~240 |
| FR5 | Collision Detection | ✅ Complete | 5 | ~100 |
| FR6 | Circular Reference Detection | ✅ Complete | 6 | ~140 |
| **Phase 1** | Core Validation (Syntax/Schema/Templates) | ✅ Complete | 218 | ~2000 |
| | | | **250 total tests** | |

---

## Phase 2.1: Enhanced Features (Recommended Priority)

### FR4: Template Parameter Handling (PRIORITY 1)
**Rationale:** Required for template reusability; blocks FR7 mock services  
**Complexity:** Medium (3-4 days)  
**Dependencies:** FR1, FR2, FR3

**Detailed Requirements:**
- Parse `parameters:` section from template headers
- Support parameter definitions with:
  - Name (required)
  - Type (string, number, boolean, array)
  - Default value
  - Required flag
  - Description
- Substitute parameters during template resolution
- Validate required parameters are provided
- Support parameter type coercion

**Test Plan:**
1. `TemplateParameterParsingTests` (8 tests)
   - Parse YAML parameter definitions
   - Handle type variations
   - Validate required field enforcement
2. `TemplateParameterSubstitutionTests` (6 tests)
   - Substitute parameters with defaults
   - Override defaults with values
   - Type coercion
3. `TemplateParameterValidationTests` (5 tests)
   - Required parameter validation
   - Type mismatch detection
   - Missing parameter handling

**Estimated LOC:** 180-220  
**Estimated Tests:** 19

**Example Feature:**
```yaml
# template.yml
parameters:
  - name: buildConfiguration
    type: string
    default: Release
    required: false
  - name: targetVersion
    type: string
    required: true

variables:
  configuration: $(parameters.buildConfiguration)
  version: $(parameters.targetVersion)
```

---

### FR4b: ETag/Last-Modified Caching (PRIORITY 2 - Phase 2.1 Optional)
**Rationale:** Optimize HTTP bandwidth; reduces cache invalidation  
**Complexity:** Low (1-2 days)  
**Dependencies:** FR3

**Requirements:**
- Add HTTP header parsing for ETag and Last-Modified
- Store headers in cache entry
- Send conditional requests (If-None-Match, If-Modified-Since)
- Handle 304 Not Modified responses
- Fallback to TTL if headers absent

**Estimated LOC:** 60-80  
**Estimated Tests:** 6

---

### FR7: Mock Services Layer (PRIORITY 3)
**Rationale:** Enable local development without Azure DevOps; blocks FR8, FR9  
**Complexity:** High (4-5 days)  
**Dependencies:** FR1, FR2, FR5

**Detailed Requirements:**
- Load mock configuration from YAML file
- Mock variable groups with scope
- Mock service connections with credentials
- Mock agent pools with capabilities
- Mock environments with approvals
- Mock repositories and branches
- Provide mock API responses
- Integrate with variable processor

**Mock Service Types:**
```yaml
# azp-local.config.yaml
mocks:
  variableGroups:
    - name: BuildVars
      scope: Release
      variables:
        buildNumber: $(Build.BuildNumber)
  serviceConnections:
    - name: AzureRM
      type: AzureResourceManager
      endpoint: https://management.azure.com
  agentPools:
    - name: ubuntu-latest
      capabilities: { os: "linux", image: "ubuntu-latest" }
  environments:
    - name: Production
      approvals: [admin-user]
```

**Estimated LOC:** 300-400  
**Estimated Tests:** 25

---

### FR8: Configuration File Support (PRIORITY 4)
**Rationale:** Enable persistent local config; prerequisite for FR9  
**Complexity:** Medium (3-4 days)  
**Dependencies:** FR1, FR2, FR7

**Requirements:**
- Load `azp-local.config.yaml` from workspace root
- Support configuration sections:
  - Server settings (port, logging level)
  - Mock services config
  - Variable substitution rules
  - Template caching policies
  - Error reporting format
- Merge CLI arguments with config file (CLI precedence)
- Variable substitution in config values
- Generate config scaffold via `config init` command
- Validate schema against JSON schema

**Config File Structure:**
```yaml
# azp-local.config.yaml
version: "1.0"
server:
  port: 5000
  logLevel: info
validation:
  strict: true
  maxDepth: 20
caching:
  enabled: true
  ttl: 3600
  maxSize: 100MB
```

**Estimated LOC:** 200-250  
**Estimated Tests:** 16

---

## Phase 2.2: Command Enhancements

### FR9: Expand Command (PRIORITY 5)
**Rationale:** Users need to see fully resolved pipeline; essential for debugging  
**Complexity:** Medium (3-4 days)  
**Dependencies:** FR1, FR2, FR3, FR4, FR7

**Requirements:**
- New command: `azp-local expand --file pipeline.yml --output expanded.yml`
- Fully resolve all variables with values
- Fetch and inline all templates
- Substitute all parameters
- Output formats:
  - YAML (default)
  - JSON
  - Markdown (human-readable)
- Optional flags:
  - `--include-map`: Include resolution map showing variable sources
  - `--inline-templates`: Inline fetched templates (vs. keeping refs)
  - `--stages`: Expand only specific stages

**Example Output (with --include-map):**
```yaml
# Generated by azp-local expand on 2025-12-19T15:30:00Z
# Resolution Map:
# - buildNum: Pipeline variables (value: "1.0.2")
# - vmImage: Stage[Build] variables (value: "ubuntu-latest")

stages:
  - stage: Build
    variables:
      buildNum: 1.0.2
      vmImage: ubuntu-latest
```

**Estimated LOC:** 250-300  
**Estimated Tests:** 20

---

### FR10: Improved Error Messages (PRIORITY 6)
**Rationale:** Reduce user debugging time; enhance developer experience  
**Complexity:** High (4-5 days)  
**Dependencies:** FR5, FR6, FR8

**Requirements:**
- Error codes (e.g., VAR001, CYCLE001, PARAM001)
- Error location (file:line:column)
- Error message with context
- Suggested remediation
- Documentation links
- Multi-format support:
  - Text (console-friendly)
  - JSON (for tooling)
  - SARIF (for IDE integration)
- Severity levels: Error, Warning, Info

**Error Example (Text):**
```
❌ CYCLE001: Circular variable reference detected
  File: pipeline.yml (Line 25)
  
  Variable dependency cycle found:
    buildNum → version → buildNum
  
  Variables in cycle:
    • buildNum = "$(version)-build"  [Line 25]
    • version = "$(buildNum).0"      [Line 27]
  
  Fix: Break the cycle by:
    1. Define buildNum directly (e.g., "1.0")
    2. Use version = "1.0-build" (hardcoded)
    3. Restructure into separate variable groups

  Docs: https://docs.example.com/errors/CYCLE001
```

**Error Example (SARIF):**
```json
{
  "version": "2.1.0",
  "runs": [{
    "results": [{
      "ruleId": "CYCLE001",
      "message": { "text": "Circular variable reference" },
      "locations": [{
        "physicalLocation": {
          "artifactLocation": { "uri": "pipeline.yml" },
          "region": { "startLine": 25 }
        }
      }],
      "relatedLocations": [
        { "physicalLocation": { ... }, "message": { "text": "cycle starts here" } }
      ]
    }]
  }]
}
```

**Estimated LOC:** 350-450  
**Estimated Tests:** 30

---

## Phase 3: Execution Engine (Future)

| ID | Feature | Complexity | Tests |
|---|---|---|---|
| EX1 | Job Execution Framework | Very High | 50+ |
| EX2 | Step Execution with Logging | High | 40+ |
| EX3 | Conditional Execution | Medium | 25+ |
| EX4 | Matrix Strategy | High | 35+ |
| EX5 | Task/Script Execution | Very High | 60+ |
| EX6 | Artifact Handling | Medium | 20+ |

---

## Implementation Sequence (Recommended)

```
Week 1 (Dec 23-27):
  Mon-Tue:  FR4 (Template Parameters)
  Wed:      FR4b (ETag Caching - optional)
  Thu-Fri:  FR7 (Mock Services)

Week 2 (Dec 30 - Jan 3):
  Mon-Tue:  FR8 (Configuration Files)
  Wed:      Code review & documentation
  Thu-Fri:  FR9 (Expand Command)

Week 3 (Jan 6-10):
  Mon-Tue:  FR10 (Error Messages)
  Wed-Thu:  Integration testing
  Fri:      Release 2.0 (Phase 2 complete)
```

---

## Success Criteria

| Milestone | Criteria | Status |
|---|---|---|
| Phase 2 Complete | All 10 FRs implemented, 400+ tests passing | In progress |
| Code Quality | 90%+ coverage, SOLID compliance | ✅ Current |
| Documentation | API docs, demo files, architecture docs | ✅ Current |
| Performance | All operations < 5 seconds for typical workloads | ✅ Current |
| Usability | CLI works for 90% of common pipeline patterns | TBD |

---

## Key Metrics

| Metric | Target | Current |
|---|---|---|
| Test Coverage | 90%+ | 85%+ |
| Build Time | < 30s | ~15s |
| Test Execution | < 5s | ~2s |
| Code Review Time | < 2 hours/PR | TBD |
| Documentation Completeness | 100% | 85% |

---

## Notes

- All features maintain **backward compatibility**
- TDD approach continues throughout
- SOLID principles enforced in code review
- Performance benchmarks tracked in each release
- User feedback collected before Phase 3

---

**Next Action:** Begin FR4 implementation (Template Parameters)  
**Estimated Completion:** Phase 2 complete by January 10, 2026
