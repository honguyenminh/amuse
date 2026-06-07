# Backend CI/CD

GitHub Actions pipelines for the Amuse backend (`backend/**`). Private GHCR packages under `ghcr.io/honguyenminh/`.

## Branch model

| Branch / event | Role | Publish | Deploy |
|----------------|------|---------|--------|
| **PR → `master`** | QA preview before merge | Yes (`:pr-<n>`, `:sha-<short>`) | **Auto** → `development` |
| **Push → `master`** | Dev integration + QA/QC | Yes (`:master`, `:sha-<short>`) | **Auto** → `development` |
| **PR → `staging` / `production`** | Release review | No (CI only) | No |
| **Push → `staging`** | Release candidate | Yes (`:staging`, `:sha-<short>`) | Manual `workflow_dispatch` |
| **Push → `production`** | Live | Yes (`:production`, `:sha-<short>`) | Manual `workflow_dispatch` |

PRs targeting `master` publish and auto-deploy so QA can test changes before merge. PRs targeting `staging` or `production` run CI only.

Promotion:

1. Feature PR → `master` → CI green → publish `:pr-<n>` → auto-deploy `development` for QA.
2. Merge when QA approves; push to `master` publishes `:master` and re-deploys `development`.
3. FF merge or PR `master` → `staging` when release-ready.
4. FF merge or PR `staging` → `production` after sign-off.

## Workflows

| Workflow | File | Triggers |
|----------|------|----------|
| Backend CI | [`.github/workflows/backend-ci.yml`](../../.github/workflows/backend-ci.yml) | PR + push to `master`/`staging`/`production` (`backend/**`) |
| Backend CodeQL | [`.github/workflows/backend-codeql.yml`](../../.github/workflows/backend-codeql.yml) | Same + weekly Monday 06:00 UTC |
| Backend Publish | [`.github/workflows/backend-publish.yml`](../../.github/workflows/backend-publish.yml) | After successful Backend CI on push to env branches, or **PR → `master`** |
| Backend Deploy | [`.github/workflows/backend-deploy.yml`](../../.github/workflows/backend-deploy.yml) | Auto after publish (push or PR to `master`); manual for `staging`/`production` |

### CI jobs

- **Format** — `dotnet format backend.slnx --verify-no-changes`
- **Build and test** — `dotnet restore/build/test` on `backend.slnx` (Testcontainers / Docker required for integration tests)
- **Docker verify** — PRs only; builds all four Dockerfiles
- **Gitleaks** — secret scan
- **Dependency review** — PRs only; fails on high+ severity new vulnerabilities

### Images (GHCR)

- `ghcr.io/honguyenminh/amuse-api`
- `ghcr.io/honguyenminh/amuse-worker-transcoder`
- `ghcr.io/honguyenminh/amuse-worker-scheduler`
- `ghcr.io/honguyenminh/amuse-migrate`

Publish runs Trivy (CRITICAL/HIGH) before push. Tags: floating tag (`master`, `staging`, `production`, or `pr-<number>`) + immutable `sha-<7-char>`. PR builds do **not** update the `master` floating tag.

### Deploy (GitOps via amuse-deploy)

Deploy does **not** commit to the `amuse` repo. After publish, `backend-deploy.yml` bumps image tags in [honguyenminh/amuse-deploy](https://github.com/honguyenminh/amuse-deploy); Argo CD syncs the cluster.

| GitHub Environment | Cluster | Deploy repo path | Default image tag |
|--------------------|---------|------------------|-------------------|
| `development` | K3s | `overlays/dev/images-tags/` | `master` or `pr-<n>` |
| `staging` | AKS | `overlays/stage/images-tags/` | `staging` |
| `production` | — | not yet defined | `production` |

Manifest structure changes in `infrastructure/kubernetes/` are synced to `amuse-deploy` by `sync-kubernetes-manifests.yml` on merge to `master`.

Requires `DEPLOY_REPO_TOKEN` on the **amuse** repo — see [DEPLOY_REPO.md](../../infrastructure/kubernetes/DEPLOY_REPO.md).

**Migrations:** Argo CD PreSync Job `amuse-migrate` runs before app rollouts. API never migrates on startup ([local-development.md](./local-development.md)).

## Local parity

From [`backend/`](../../backend/):

```bash
dotnet format backend.slnx --verify-no-changes
dotnet restore backend.slnx
dotnet build backend.slnx -c Release --no-restore
dotnet test backend.slnx -c Release --no-build
```

Apply formatting fixes:

```bash
dotnet format backend.slnx
```

Shared MSBuild settings: [`backend/Directory.Build.props`](../../backend/Directory.Build.props) (NuGet audit, NetAnalyzers, CI vulnerability gate).

## GitHub repo settings (manual)

Enable in repository settings:

- **Secret scanning** + push protection
- **Dependabot alerts** + security updates
- **Branch protection** on `staging` and `production`: require Backend CI + review
- **`master`**: require Backend CI on PRs (direct pushes allowed for fast QA — team choice)
- **Environments:** `development`, `staging`, `production` (add required reviewers on `production` before real deploys)

## Related workflows

| Workflow | File |
|----------|------|
| Sync manifests to deploy repo | [`.github/workflows/sync-kubernetes-manifests.yml`](../../.github/workflows/sync-kubernetes-manifests.yml) |
| Frontend Publish | [`.github/workflows/frontend-publish.yml`](../../.github/workflows/frontend-publish.yml) |
