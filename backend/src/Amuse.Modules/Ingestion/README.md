# Ingestion bounded context (scaffold)

This module reserves the `ingestion` Postgres schema and cross-BC contracts for a future split of audio ingest infrastructure out of Catalog.

## Current state

- `IngestionDbContext` creates the `ingestion` schema via EF migrations.
- `IIngestionCommands` is a stub; catalog handlers still own presign/complete/outbox flows.
- Production tables remain in `catalog` (`audio_master_upload_intent`, `audio_transcode_job`, `catalog_outbox_message`).

## Future work (Phase 4b full split)

1. Move ingest entities/configurations from Catalog into `Amuse.Modules/Ingestion/Persistence`.
2. Data migration from `catalog.*` to `ingestion.*` (or dual-write window).
3. Implement `IIngestionCommands` and point Catalog handlers at the contract.
4. Decide whether outbox stays catalog-scoped or moves with ingestion tables.

Do **not** add a Common outbox interceptor until that design pass completes.
