# Tasks

## 1. Runner boundary

- [ ] 1.1 Define IBuildTestRunner protocol, execution records and preset configuration and verify only server-defined commands can be submitted
- [ ] 1.2 Add BuildExecution migrations and queue integration and verify review/publication compatibility when no execution is requested

## 2. Isolation and results

- [ ] 2.1 Implement the separate runner with workspace containment, process-tree limits and capability checks and verify it fails closed without required isolation
- [ ] 2.2 Implement default network deny, optional feed allowlist and short-lived secret injection and verify blocked-connectivity and redaction security tests
- [ ] 2.3 Stream bounded redacted output and map terminal states and verify Passed, Failed, TimedOut, Cancelled and InfrastructureError fixtures
- [ ] 2.4 Integrate build/test status into reports and ReviewDecision without converting it to AI findings and verify formatter snapshots

## 3. Acceptance

- [ ] 3.1 Add cancellation, cleanup, resource exhaustion and malicious-repository integration tests in an isolated test environment
- [ ] 3.2 Pilot no-op, build and test presets sequentially and verify rollback disables new submissions and cancels active executions safely

## 4. RAG integration

- [ ] 4.1 Keep raw build/test logs out of context index and embeddings while exposing only bounded redacted status references, and verify malicious output cannot become a prompt instruction or expand runner presets
