# Tasks

## 1. Schema and compatibility

- [ ] 1.1 Add migration ledger and lease/retry/dead-letter columns and verify an existing ReviewJobs database upgrades without payload loss
- [ ] 1.2 Backfill LogicalKey and indexes with duplicate-safe handling and verify filtered uniqueness under concurrent inserts

## 2. Queue protocol

- [ ] 2.1 Implement atomic Claim, heartbeat and lease-owner guarded transitions and verify two database clients cannot own one Job
- [ ] 2.2 Implement transient classification, jittered backoff, MaxAttempts and dead-letter transitions and verify deterministic clock-based tests
- [ ] 2.3 Update ReviewWorker to cancel on lease loss and remove startup-wide Running reset, verifying crash recovery with two workers
- [ ] 2.4 Make enqueue and terminal review updates idempotent while preserving PublishToMergeRequest, verifying duplicate logical requests return one RunId

## 3. Acceptance

- [ ] 3.1 Add SQL integration tests for cancellation, lease expiry, retry, dead letter and graceful shutdown
- [ ] 3.2 Run multi-worker stress and existing publication tests and verify no concurrent execution or duplicate external write occurs

## 4. RAG integration

- [ ] 4.1 Support leased, idempotent index-refresh jobs keyed by repository/SHA/policy and verify two workers, retries and cancellation cannot publish duplicate or stale evidence
