# Tasks

## 1. Provider contracts

- [ ] 1.1 Refactor publication into vendor-neutral summary/inline/status operations and verify existing GitLab publication tests remain unchanged
- [ ] 1.2 Add GitHub connection capability discovery for comments and checks and verify PAT and GitHub App permission diagnostics

## 2. GitHub behavior

- [ ] 2.1 Implement HMAC webhook validation and Delivery deduplication and verify official payload fixtures and invalid signatures
- [ ] 2.2 Implement GitHub summary and inline publication with stable markers and pagination and verify retry creates no duplicates
- [ ] 2.3 Implement Check Run with commit-status fallback and verify terminal/incomplete severity mappings
- [ ] 2.4 Enforce open PR and Head SHA guards before writes and verify a concurrent push results in zero stale comments

## 3. Acceptance

- [ ] 3.1 Add GitHub Enterprise contract tests for rate limits, renames, invalid lines and permission failure
- [ ] 3.2 Run end-to-end webhook-to-review-to-publication with publish on/off and verify opt-out produces no GitHub writes

## 4. RAG integration

- [ ] 4.1 Add bounded commit-specific GitHub file/tree reads and SHA-keyed refresh on accepted webhook events and verify permission denial, push races and publish-off produce no stale evidence or GitHub writes
