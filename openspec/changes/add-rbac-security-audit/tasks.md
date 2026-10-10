# Tasks

## 1. Authorization and audit data

- [ ] 1.1 Add role mapping configuration and named authorization policies and verify Development and Production deny/allow matrices
- [ ] 1.2 Add AuditEvents migration and append-only service and verify metadata allowlisting excludes secret, prompt and diff values

## 2. Enforcement

- [ ] 2.1 Apply Viewer/Reviewer/Admin policies to pages and server operations and verify direct invocation cannot bypass hidden UI controls
- [ ] 2.2 Implement organization and repository SensitiveContentPolicy before snapshots/prompts and verify private-key and configured-path fixtures are excluded
- [ ] 2.3 Add Production startup validation for TLS bypass and role mapping and verify unsafe configurations fail with actionable messages
- [ ] 2.4 Implement secret rotation workflow and verify the old credential remains active until a new credential passes its connection test

## 3. Acceptance

- [ ] 3.1 Add security integration tests for unauthorized configuration, review, cancellation and publication attempts
- [ ] 3.2 Review logs and SQL fixtures for sensitive-value leakage and verify the full test suite and deployment checks pass

## 4. RAG integration

- [ ] 4.1 Enforce authorization and SensitiveContentPolicy at both index and retrieval boundaries and verify tenant-crossing, secret-file, embedding opt-out and cache-leak integration tests
