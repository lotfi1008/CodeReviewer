# Tasks

## 1. Analytics

- [ ] 1.1 Add required indexes and bounded analytics queries and verify repository authorization and date filtering against SQL fixtures
- [ ] 1.2 Add dashboard views for duration, token, status, findings and feedback and verify empty/small samples show uncertainty correctly

## 2. Notifications and export

- [ ] 2.1 Add NotificationRules/Deliveries and provider contracts and verify concurrent retries send one logical notification
- [ ] 2.2 Implement Email, Slack and Teams adapters with secret protection and verify sanitized payload contract tests
- [ ] 2.3 Add queued JSON/CSV/PDF exports with cancellation, TTL and CSV injection defense and verify oversized exports remain asynchronous

## 3. Retention

- [ ] 3.1 Add RetentionPolicies, LegalHolds and dry-run records and verify dry-run and confirmed candidate sets match
- [ ] 3.2 Implement batch purge with Audit and dependency ordering and verify held or unauthorized data is never deleted
- [ ] 3.3 Run analytics, notification, export and destructive-operation acceptance tests and verify scheduler rollback switches stop all new jobs

## 4. RAG integration

- [ ] 4.1 Include aggregate retrieval coverage, citation quality, latency and token deltas with sample sizes in authorized analytics and verify exports/notifications omit raw evidence
- [ ] 4.2 Extend dry-run, Legal Hold and purge to index chunks, evidence metadata and optional embeddings and verify removal is auditable and cannot cross repository boundaries
