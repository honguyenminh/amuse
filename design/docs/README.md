# Amuse DA1 Documentation Index

## Documents
- implementation-plan.md: Master implementation roadmap and phased execution.
- srs.md: Full Software Requirements Specification.
- requirements-catalog.md: FR/NFR catalog with mappings and acceptance matrix.
- schema-spec.md: Multi-tenant schema and data model specification.
- api-contract.md: REST API contract outline (/api/v1).
- cdn-streaming-spec.md: Cloudflare-first streaming and CDN authorization/cache/purge spec.
- test-strategy.md: Test layers, acceptance scenarios, and NFR validation.
- risk-register.md: Risks, mitigations, contingency, and go/no-go checklist.
- wbs-milestones.md: WBS, dependencies, milestones, and critical path.

## Recommended Execution Order
1. Finalize schema migration scripts from schema-spec.md.
2. Implement auth + tenancy guard from srs.md and api-contract.md.
3. Implement catalog/upload/streaming pipeline.
4. Implement consumer and business portal MVP endpoints and UI.
5. Implement billing/ledger/settlement.
6. Integrate recommendation and observability.
7. Run performance tests and deploy to AKS.

## Notes
- All DA1 decisions in this docs package reflect approved constraints:
  - Single DB row-level multi-tenancy.
  - DASH primary with optional HLS output.
  - Mock payment and double-entry ledger.
  - Monthly settlement with valid stream >= 30s.
  - Full OpenTelemetry stack.
