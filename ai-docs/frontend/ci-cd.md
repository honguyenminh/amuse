# Frontend CI/CD

GitHub Actions for `frontend/**`. Images: `ghcr.io/<owner>/amuse-consumer`, `ghcr.io/<owner>/amuse-business`.

## Branch model

Same promotion model as [backend CI/CD](../backend/ci-cd.md): PR/push to `master` publishes and auto-deploys dev; `staging` / `production` publish on push; PRs to those branches are CI-only.

| Branch / event | Frontend CI | Publish | Deploy (via `backend-deploy.yml`) |
|----------------|-------------|---------|-----------------------------------|
| PR → `master` | Yes | Yes (`:pr-<n>`, `:sha-<short>`) | Auto → `development` |
| Push → `master` | Yes | Yes (`:master`, `:sha-<short>`) | Auto → `development` |
| PR → `staging` / `production` | Yes | No | No |
| Push → `staging` / `production` | Yes | Yes | Manual `workflow_dispatch` |

## Workflows

| Workflow | File | Triggers |
|----------|------|----------|
| Frontend CI | [`.github/workflows/frontend-ci.yml`](../../.github/workflows/frontend-ci.yml) | PR + push (`frontend/**`) |
| Frontend Dependency Review | [`.github/workflows/frontend-dependency-review.yml`](../../.github/workflows/frontend-dependency-review.yml) | PR only (`frontend/**`) |
| Frontend Publish | [`.github/workflows/frontend-publish.yml`](../../.github/workflows/frontend-publish.yml) | After successful **Frontend CI**, or `workflow_dispatch` |

### CI jobs (ordered with `needs` blockers)

```
lint → test → build → docker-verify
```

- **Lint** — `tsc --noEmit`; consumer also runs `check:colors`
- **Test** — consumer `vitest run` (business has no test script yet)
- **Build** — `pnpm build` for consumer and business (`NEXT_PUBLIC_API_BASE_URL=https://api.example.com` for CI only)
- **Docker verify** — builds both frontend Dockerfiles

**ESLint:** `pnpm lint` is not in CI yet — the repo has outstanding ESLint violations. Add it to the lint job once clean.

### Publish

Mirrors backend publish:

1. Build image (GHA cache)
2. Trivy scan (CRITICAL/HIGH, `scanners: vuln`, `ignore-unfixed`)
3. Push to GHCR

`NEXT_PUBLIC_API_BASE_URL` is baked at image build time per tag:

| Floating tag | API URL baked into image |
|--------------|--------------------------|
| `master`, `pr-*` | `https://api.skynet-beta.m8.io.vn` |
| `staging` | `https://api.staging.amuse.local` |
| `production` | `https://api.amuse.local` |
| other / manual | `https://api.example.com` |

`workflow_dispatch` accepts optional `image_tag` override (same as backend publish).

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
