# Frontend CI/CD

Two **independent** pipelines (consumer and business), each calling shared reusable workflows. Images: `ghcr.io/<owner>/amuse-consumer`, `ghcr.io/<owner>/amuse-business`.

## Branch model

Same promotion model as [backend CI/CD](../backend/ci-cd.md): PR/push to `master` publishes and auto-deploys dev; `staging` / `production` publish on push; PRs to those branches are CI-only.

| Branch / event | App CI | Publish | Deploy (via `backend-deploy.yml`) |
|----------------|--------|---------|-----------------------------------|
| PR → `master` | Per changed app | Per app (`:pr-<n>`, `:sha-<short>`) | Auto → `development` (that app’s image only) |
| Push → `master` | Per changed app | Per app (`:master`, `:sha-<short>`) | Auto → `development` (that app’s image only) |
| PR → `staging` / `production` | Per changed app | No | No |
| Push → `staging` / `production` | Per changed app | Per app | Manual `workflow_dispatch` |

## Path filters

| Workflow | Triggers on |
|----------|-------------|
| Frontend Consumer CI | `frontend/consumer/**`, `frontend/packages/**` |
| Frontend Business CI | `frontend/business/**`, `frontend/packages/**` |

Changes under `frontend/packages/` (e.g. `catalog-text`) run **both** pipelines because both apps depend on it.

## Workflows

### Per-app entrypoints

| Workflow | File | Role |
|----------|------|------|
| Frontend Consumer CI | [`.github/workflows/frontend-consumer-ci.yml`](../../.github/workflows/frontend-consumer-ci.yml) | Path-filtered trigger → reusable CI |
| Frontend Business CI | [`.github/workflows/frontend-business-ci.yml`](../../.github/workflows/frontend-business-ci.yml) | Path-filtered trigger → reusable CI |
| Frontend Consumer Publish | [`.github/workflows/frontend-consumer-publish.yml`](../../.github/workflows/frontend-consumer-publish.yml) | After Consumer CI, or `workflow_dispatch` |
| Frontend Business Publish | [`.github/workflows/frontend-business-publish.yml`](../../.github/workflows/frontend-business-publish.yml) | After Business CI, or `workflow_dispatch` |
| Frontend Dependency Review | [`.github/workflows/frontend-dependency-review.yml`](../../.github/workflows/frontend-dependency-review.yml) | PR only (`frontend/**`) |

### Reusable templates

| Workflow | File | Inputs |
|----------|------|--------|
| Reusable frontend app CI | [`.github/workflows/reusable-frontend-app-ci.yml`](../../.github/workflows/reusable-frontend-app-ci.yml) | `app_dir`, `dockerfile`, `run_unit_tests`, `extra_lint_script` |
| Reusable frontend app publish | [`.github/workflows/reusable-frontend-app-publish.yml`](../../.github/workflows/reusable-frontend-app-publish.yml) | `image`, `dockerfile`, `source_sha`, tags/event metadata |

Each app reads `packageManager` from its own `frontend/<app>/package.json` inside the reusable CI workflow.

### CI job chain (per app)

```
lint (+ optional unit tests in same job)
  → build
  → docker-verify
```

- **Consumer** — `tsc --noEmit`, `check:colors`, `vitest run`
- **Business** — `tsc --noEmit` only (no test script yet)

**ESLint:** `pnpm lint` is not in CI yet — outstanding violations. Add via `extra_lint_script` once clean.

### Publish

Mirrors backend publish (build → Trivy → push). One image per publish workflow.

`NEXT_PUBLIC_API_BASE_URL` baked at build time:

| Floating tag | API URL |
|--------------|---------|
| `master`, `pr-*` | `https://api.skynet-beta.m8.io.vn` |
| `staging` | `https://api.staging.amuse.local` |
| `production` | `https://api.amuse.local` |
| other / manual | `https://api.example.com` |

### Deploy

`backend-deploy.yml` bumps **only the images from the publish workflow that completed**:

| Publish workflow | Images updated in `amuse-deploy` |
|------------------|----------------------------------|
| Backend Publish | API, workers, migrate (Argo Sync hook on next sync) |
| Frontend Consumer Publish | `amuse-consumer` |
| Frontend Business Publish | `amuse-business` |

Manual deploy (`workflow_dispatch`) still bumps all images in the overlay.

## Local parity

```bash
cd frontend/consumer
pnpm install --frozen-lockfile
pnpm exec tsc --noEmit
pnpm run check:colors
pnpm test
NEXT_PUBLIC_API_BASE_URL=https://api.example.com pnpm build

cd ../business
pnpm install --frozen-lockfile
pnpm exec tsc --noEmit
NEXT_PUBLIC_API_BASE_URL=https://api.example.com pnpm build
```

Docker (from repo root):

```bash
docker build -f frontend/consumer/Dockerfile frontend
docker build -f frontend/business/Dockerfile frontend
```
