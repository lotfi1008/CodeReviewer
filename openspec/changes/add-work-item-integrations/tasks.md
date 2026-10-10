# Tasks

## 1. Contracts and connections

- [ ] 1.1 Add vendor-neutral WorkItem/IWorkItemProvider contracts and encrypted connection persistence and verify provider models do not leak into ReviewRun
- [ ] 1.2 Implement configured reference detection and selection UI and verify ambiguous references require user choice

## 2. Providers and context

- [ ] 2.1 Implement Jira read adapter and contract tests for fields, pagination, permissions and unavailable service
- [ ] 2.2 Implement Azure Boards read adapter with equivalent mapping and contract tests
- [ ] 2.3 Add bounded untrusted WorkItemSnapshot context to Review and verify prompt-injection fixtures cannot alter system instructions

## 3. Explicit writes

- [ ] 3.1 Implement previewed Create/Comment/Link operations with Reviewer authorization, Audit and FindingUid idempotency and verify retries do not duplicate issues
- [ ] 3.2 Add UI links and failure states that preserve completed reviews and verify provider outages remain non-destructive
- [ ] 3.3 Run end-to-end Jira and Azure test-server scenarios and verify disabling write capability leaves read-only context available

## 4. RAG integration

- [ ] 4.1 Expose only selected, authorized, bounded Work Item snapshots as provenance-tagged optional evidence and verify revoked access, updated items and injection text never leak to another repository or trigger writes
