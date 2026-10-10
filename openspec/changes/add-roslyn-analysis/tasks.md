# Tasks

## 1. Analysis contracts and workspace

- [ ] 1.1 Add Roslyn dependencies and ICodeAnalysisProvider DTO contracts and verify the web project builds without enabling analysis
- [ ] 1.2 Implement bounded commit workspace and solution selection from effective policy and verify cleanup, cancellation and path containment tests

## 2. Semantic analysis

- [ ] 2.1 Map changed spans to symbols, types, nullable context, invocations and references and verify focused Roslyn fixture tests
- [ ] 2.2 Implement versioned deterministic rules for blocking async, nullable and cancellation signatures and verify each Finding includes reproducible evidence
- [ ] 2.3 Record complete/partial/unavailable coverage and verify uncompilable or missing-solution repositories never report semantic success
- [ ] 2.4 Merge Roslyn context and findings into AiReviewService with conservative deduplication and verify shared AI/Roslyn defects publish once

## 3. Rollout

- [ ] 3.1 Add feature flags, budget configuration and report-only mode and verify a pilot repository can enable one rule independently
- [ ] 3.2 Run security, performance and full regression tests and verify disabling Roslyn restores current review behavior

## 4. RAG integration

- [ ] 4.1 Export SHA-scoped symbol/reference hints to the optional retriever and verify partial compilation, sensitive exclusions and disabled Roslyn fall back to lexical retrieval without overstating coverage
