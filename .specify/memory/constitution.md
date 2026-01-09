<!--
Sync Impact Report:
- Version: 0.0.0 → 1.0.0
- Modified Principles: N/A (initial version)
- Added Sections: All (initial constitution)
- Removed Sections: None
- Templates Requiring Updates:
  ✅ plan-template.md - Reviewed, Constitution Check section aligns
  ✅ spec-template.md - Reviewed, requirements alignment verified
  ✅ tasks-template.md - Reviewed, task categorization aligns
  ✅ All command files - Reviewed, no agent-specific references found
- Follow-up TODOs: None
-->

# SpecKit Constitution

## Core Principles

### I. Specification-First Development

Every feature begins with a complete, technology-agnostic specification that defines WHAT and WHY before HOW. Specifications MUST:
- Focus exclusively on user needs and business outcomes
- Contain zero implementation details (no languages, frameworks, databases, APIs)
- Be written for non-technical stakeholders
- Define measurable success criteria
- Include testable acceptance scenarios

**Rationale**: Separating requirements from implementation enables better decision-making, reduces rework, and ensures features deliver actual user value rather than technical solutions searching for problems.

### II. Test-First Development (NON-NEGOTIABLE)

Test-Driven Development (TDD) is mandatory for all implementation work. The workflow MUST be:
1. Write tests that verify acceptance criteria
2. Get user/stakeholder approval on tests
3. Verify tests fail (Red)
4. Implement minimum code to pass tests (Green)
5. Refactor while keeping tests green (Refactor)

Integration tests are REQUIRED for:
- New library contract tests
- Contract changes (API modifications)
- Inter-service communication
- Shared schema definitions

**Rationale**: TDD ensures implementation matches specifications, catches regressions early, and provides living documentation of expected behavior. The Red-Green-Refactor cycle enforces discipline and quality.

### III. Library-First Architecture

Every feature MUST start as a standalone library. Libraries MUST be:
- Self-contained with clear boundaries
- Independently testable without external dependencies
- Documented with purpose and usage examples
- Focused on a single, well-defined purpose

No "organizational-only" libraries—every library must provide concrete functionality. Avoid creating libraries solely for grouping related code without cohesive purpose.

**Rationale**: Library-first design enforces modularity, reusability, and testability. It prevents monolithic coupling and enables independent evolution of features.

### IV. CLI Interface Requirement

Every library MUST expose its functionality via a command-line interface using a text in/out protocol:
- Input: stdin and/or command-line arguments
- Output: stdout for results, stderr for errors
- Support both JSON and human-readable formats
- Follow standard UNIX conventions (exit codes, pipe-friendly)

**Rationale**: CLI interfaces ensure debuggability, scriptability, and integration flexibility. Text protocols are universally accessible and easy to test.

### V. Versioning & Breaking Changes

All libraries and APIs MUST follow semantic versioning (MAJOR.MINOR.PATCH):
- **MAJOR**: Breaking changes (backward-incompatible modifications)
- **MINOR**: New features (backward-compatible additions)
- **PATCH**: Bug fixes and non-functional improvements

Breaking changes REQUIRE:
- Migration guide documenting upgrade path
- Deprecation warnings in prior MINOR version
- Clear communication in changelog and documentation

**Rationale**: Predictable versioning enables users to confidently upgrade and plan for breaking changes without surprises.

### VI. Observability & Transparency

All components MUST be observable and debuggable:
- Text I/O ensures inspectability at every layer
- Structured logging (JSON format) required for all operations
- Log levels: ERROR (failures), WARN (unexpected states), INFO (key operations), DEBUG (detailed traces)
- Clear error messages with actionable guidance

**Rationale**: Transparency accelerates debugging, reduces support burden, and builds user trust through visibility into system behavior.

### VII. Simplicity & YAGNI

Start simple and add complexity only when proven necessary:
- Build minimum viable features that deliver user value
- Reject speculative abstractions and premature optimization
- Three similar lines beat a premature abstraction
- Delete unused code completely—no backwards-compatibility hacks

Complexity MUST be justified against constitution principles. Every abstraction must earn its place by solving real problems, not hypothetical future needs.

**Rationale**: Simplicity reduces cognitive load, accelerates development, and minimizes maintenance burden. YAGNI (You Aren't Gonna Need It) prevents over-engineering.

### VIII. Independent User Stories

Feature specifications MUST decompose into independently testable user stories, where each story:
- Can be implemented in isolation
- Delivers standalone value (viable MVP increment)
- Has clear priority (P1, P2, P3...)
- Includes independent test scenarios

**Rationale**: Independent stories enable parallel development, incremental delivery, and flexible prioritization. Each story represents a shippable increment of value.

## Development Workflow

### Feature Development Lifecycle

1. **Specification** (`/speckit.specify`): Create technology-agnostic spec from user description
2. **Clarification** (`/speckit.clarify`): Resolve underspecified requirements (optional)
3. **Planning** (`/speckit.plan`): Research technologies, design data models, define contracts
4. **Task Generation** (`/speckit.tasks`): Break design into dependency-ordered implementation tasks
5. **Implementation** (`/speckit.implement`): Execute tasks following TDD workflow
6. **Analysis** (`/speckit.analyze`): Validate consistency across artifacts (optional)

### Quality Gates

Before proceeding to planning, specifications MUST pass:
- No implementation details present
- All mandatory sections completed
- Requirements are testable and unambiguous
- Success criteria are measurable and technology-agnostic
- Maximum 3 [NEEDS CLARIFICATION] markers (preferably 0)

Before implementation, plans MUST pass:
- Constitution Check completed and violations justified
- All NEEDS CLARIFICATION resolved via research
- Technical context fully specified
- Data models and contracts defined
- Agent context updated with new technologies

## Governance

### Amendment Process

This constitution supersedes all other practices. Amendments REQUIRE:
1. Documentation of proposed changes with rationale
2. Approval from project maintainers
3. Migration plan for existing features affected
4. Version bump following semantic versioning:
   - **MAJOR**: Backward-incompatible governance changes or principle removals
   - **MINOR**: New principles or materially expanded guidance
   - **PATCH**: Clarifications, wording fixes, non-semantic refinements

### Compliance & Review

- All code reviews MUST verify compliance with constitution principles
- Pull requests MUST include constitution compliance statement
- Any complexity introduced MUST be justified against constitutional principles
- Regular constitutional review (quarterly recommended) to ensure principles remain relevant

### Constitutional Enforcement

Use `.specify/templates/agent-file-template.md` for runtime development guidance that operationalizes these principles. This file is auto-generated from feature plans and MUST remain consistent with constitutional requirements.

**Version**: 1.0.0 | **Ratified**: 2026-01-09 | **Last Amended**: 2026-01-09
