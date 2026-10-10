# Tasks

## 1. Snapshot and persistence

- [ ] 1.1 Add ComparisonRun/Variant and immutable input snapshot migrations and verify historical ReviewRun queries remain compatible
- [ ] 1.2 Add comparison enqueue validation, cost estimate and confirmation and verify all variant jobs force PublishToMergeRequest false

## 2. Execution and matching

- [ ] 2.1 Execute variants from the shared snapshot through the reliable queue and verify a later MR push does not change any variant input
- [ ] 2.2 Implement exact and conservative similarity matching with Ambiguous output and verify curated duplicate/contradiction fixtures
- [ ] 2.3 Aggregate duration, token and feedback with sample sizes and verify missing feedback reports Unknown rather than a winner

## 3. UI and acceptance

- [ ] 3.1 Add comparison creation, progress and side-by-side report UI and verify repository authorization for every linked run
- [ ] 3.2 Add integration tests proving comparison cannot call any source-control write API even when input profiles publish
- [ ] 3.3 Run cost, cancellation and regression tests and verify feature-flag rollback blocks new comparisons without deleting existing reports

## 4. RAG integration

- [ ] 4.1 Freeze evidence snapshot for model/profile variants and support paired RAG-off/lexical/optional-semantic variants on the same MR/SHA/policy, verifying fair inputs, provenance and zero publication for every variant
