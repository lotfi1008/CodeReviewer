# Tasks

## 1. Telemetry foundation

- [ ] 1.1 Add configurable OpenTelemetry packages, ActivitySource and Meter and verify the application runs with exporters disabled
- [ ] 1.2 Propagate CorrelationId through webhook, queue, ReviewRun, model and publication and verify one end-to-end trace joins all stages

## 2. Metrics and health

- [ ] 2.1 Instrument review, LLM, queue and source-control operations with bounded labels and verify cardinality tests reject identifiers as metric labels
- [ ] 2.2 Add live, ready and protected dependency health endpoints and verify SQL failure affects readiness but not liveness
- [ ] 2.3 Add central telemetry redaction and verify synthetic secrets, prompts and diffs never reach captured logs or spans

## 3. Operations

- [ ] 3.1 Add exporter, sampling and health configuration documentation and verify invalid endpoints do not prevent startup when exporter is disabled
- [ ] 3.2 Define dashboard and alert queries for latency, retry, queue depth and failure rate and verify them against generated pilot telemetry
- [ ] 3.3 Run load and regression tests and verify instrumentation overhead stays within the documented pilot budget

## 4. RAG integration

- [ ] 4.1 Add bounded index/retrieval latency, coverage, cache and token metrics and verify captured telemetry contains no query, source snippet, path, embedding or high-cardinality label
