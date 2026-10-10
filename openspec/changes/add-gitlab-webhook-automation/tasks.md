# Tasks

## 1. Persistence and policy

- [ ] 1.1 Add versioned migrations for WebhookEvents and RepositoryAutomationSettings and verify upgrade from the current schema preserves existing reviews
- [ ] 1.2 Add policy storage and Admin UI with encrypted webhook secrets and verify authorization and round-trip integration tests

## 2. Webhook processing

- [ ] 2.1 Implement bounded GitLab webhook authentication/parsing and verify contract tests for valid, invalid, malformed, and unsupported events
- [ ] 2.2 Implement current-MR/SHA validation and transactional event deduplication and verify concurrent duplicate deliveries enqueue once
- [ ] 2.3 Map policy snapshots into ReviewJob including PublishToMergeRequest and verify disabled publication performs zero source-control writes

## 3. Operations and rollout

- [ ] 3.1 Add audit, correlation and low-cardinality metrics and verify no token or payload content appears in captured telemetry
- [ ] 3.2 Add end-to-end tests for create, reopen, update, closed MR, stale SHA and SQL failure responses
- [ ] 3.3 Document staged enablement and rollback by repository and verify strict OpenSpec plus the full test suite pass

## 4. RAG integration

- [ ] 4.1 Emit one idempotent context-index refresh/invalidation for an accepted new Head SHA when retrieval is enabled and verify duplicate, stale and disabled webhook fixtures create no stale evidence or MR write
