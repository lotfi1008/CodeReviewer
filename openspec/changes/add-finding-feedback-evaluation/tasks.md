# Tasks

## 1. Finding identity and feedback

- [ ] 1.1 Add FindingUid migration and chunked backfill and verify historical finding text and review reports remain unchanged
- [ ] 1.2 Add append-only FindingFeedback persistence and authorization and verify superseding feedback preserves full history
- [ ] 1.3 Add feedback controls to ReviewReport and verify submission changes no source-control state

## 2. Evaluation

- [ ] 2.1 Add immutable EvaluationDataset, item and run models and verify dataset versioning and snapshot round trips
- [ ] 2.2 Implement documented precision, acceptance and false-positive calculations and verify fixtures cover Unknown and conflicting reviewers
- [ ] 2.3 Add evaluation execution/report UI with model/profile/prompt snapshots and verify repeated runs remain independently comparable

## 3. Acceptance

- [ ] 3.1 Add SQL, authorization and UI integration tests for migrated and new findings
- [ ] 3.2 Create the 20-MR pilot evaluation procedure and verify reports expose sample size and the 80 percent acceptance target without claiming it prematurely

## 4. RAG integration

- [ ] 4.1 Add evidence-relevance and citation-validity labels to versioned evaluation items and verify unlabeled evidence stays Unknown and historical FindingFeedback remains unchanged
- [ ] 4.2 Compare RAG-off, lexical and optional semantic runs on identical snapshots and verify retrieval quality, false positives, acceptance, latency and tokens include denominators and uncertainty
