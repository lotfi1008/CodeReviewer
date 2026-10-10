# Tasks

## 1. Source and schema

- [ ] 1.1 Extend source-control providers with bounded commit-specific file reads and verify GitLab/GitHub contract tests for found, missing and oversized files
- [ ] 1.2 Define and validate the version-1 YAML schema with safe parser settings and verify unknown versions, duplicate keys, aliases and executable fields fail closed

## 2. Effective policy

- [ ] 2.1 Implement Organization/Repository/Profile precedence and verify organization sensitive exclusions cannot be weakened
- [ ] 2.2 Add PolicySnapshot migration and canonical hashing and verify historical reports retain the effective policy after repository changes
- [ ] 2.3 Integrate policy with ReviewScope, domains, solution and allowed build presets and verify current behavior remains when no file exists
- [ ] 2.4 Add Admin preview UI with source locations and validation errors and verify preview and execution use the same resolver

## 3. Acceptance

- [ ] 3.1 Add unit, provider contract and SQL integration tests for policy caching, SHA isolation and prompt safety
- [ ] 3.2 Document v1 examples and staged enforcement and verify strict validation plus the full review suite pass

## 4. RAG integration

- [ ] 4.1 Extend v1 policy with optional retrieval mode, source allowlist and bounded budgets under organization precedence and verify absent config keeps RAG off and no repository rule weakens sensitive exclusions
- [ ] 4.2 Include effective policy hash in retrieval snapshot/cache keys and verify policy or SHA changes invalidate evidence without changing historical reviews
