# Tasks

## 1. Contracts and compatibility

- [ ] 1.1 Add retriever/query/evidence/coverage contracts and additive SQL migrations; verify historical ReviewRun/report reads and disabled-RAG behavior remain unchanged
- [ ] 1.2 Add commit-specific bounded file/tree reads for supported providers; verify GitLab/GitHub contract tests reject wrong SHA, oversized files and path traversal
- [ ] 1.3 Add retrieval mode, source allowlist and budgets to effective repository policy with organization caps; verify missing policy preserves current behavior and repository rules cannot weaken exclusions

## 2. Secure indexing and retrieval

- [ ] 2.1 Implement ACL and SensitiveContentPolicy checks before fetch/index and before result delivery; verify cross-repository, denied-user, secret-path and private-key fixtures yield no content
- [ ] 2.2 Implement SHA/policy-keyed chunking, incremental upsert, tombstones and leased refresh jobs; verify duplicate webhook/refresh and deleted-file tests create no stale or duplicate evidence
- [ ] 2.3 Implement bounded lexical path/symbol/keyword ranking, deduplication and on-demand fallback; verify labeled cross-file/test/document fixtures and deterministic budget limits
- [ ] 2.4 Integrate optional Roslyn symbol hints and separately authorized Work Item evidence only when available; verify absence of either dependency does not block review

## 3. Review grounding and resilience

- [ ] 3.1 Add data-only evidence envelope and citation/provenance validation to Review; verify prompt-injection and nonexistent-line fixtures cannot change policy, publication or claim valid evidence
- [ ] 3.2 Persist selected evidence metadata and complete/partial/unavailable coverage without raw secrets; verify SHA/hash round trips, redaction and historical migration tests
- [ ] 3.3 Enforce time/token/file quotas, cancellation and unavailable fallback; verify retrieval outage leaves Diff review operational and creates zero MR/PR writes when PublishToMergeRequest is false

## 4. Evaluation, rollout and optional semantic mode

- [ ] 4.1 Build a versioned 20-MR pilot dataset with relevant-evidence labels, cross-file and negative cases; verify immutable inputs and reviewer provenance
- [ ] 4.2 Report Recall@k, Precision@k, grounded citation rate, false positives, acceptance, latency and token deltas for RAG off versus lexical on identical snapshots; verify sample sizes and missing labels are explicit
- [ ] 4.3 Add semantic/hybrid adapter behind separate flag and external-embedding opt-in; verify no external embedding call in default mode and activation gate documents quality, privacy, cost and latency results
- [ ] 4.4 Add low-cardinality retrieval/index telemetry and retention purge; verify logs contain no snippets and Legal Hold, deletion and rollback acceptance tests pass
