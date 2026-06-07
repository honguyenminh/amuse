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
| Backend Dependency Review | [`.github/workflows/backend-dependency-review.yml`](../../.github/workflows/backend-dependency-review.yml) | PR only (`backend/**`) |
| Backend CodeQL | [`.github/workflows/backend-codeql.yml`](../../.github/workflows/backend-codeql.yml) | Same + weekly Monday 06:00 UTC |
| Backend Publish | [`.github/workflows/backend-publish.yml`](../../.github/workflows/backend-publish.yml) | After successful Backend CI, **PR → `master`**, or `workflow_dispatch` |
| Frontend Consumer CI | [`.github/workflows/frontend-consumer-ci.yml`](../../.github/workflows/frontend-consumer-ci.yml) | PR + push (`frontend/consumer/**`, `frontend/packages/**`) |
| Frontend Business CI | [`.github/workflows/frontend-business-ci.yml`](../../.github/workflows/frontend-business-ci.yml) | PR + push (`frontend/business/**`, `frontend/packages/**`) |
| Frontend Consumer Publish | [`.github/workflows/frontend-consumer-publish.yml`](../../.github/workflows/frontend-consumer-publish.yml) | After Consumer CI, **PR → `master`**, or `workflow_dispatch` |
| Frontend Business Publish | [`.github/workflows/frontend-business-publish.yml`](../../.github/workflows/frontend-business-publish.yml) | After Business CI, **PR → `master`**, or `workflow_dispatch` |
| Frontend Dependency Review | [`.github/workflows/frontend-dependency-review.yml`](../../.github/workflows/frontend-dependency-review.yml) | PR only (`frontend/**`) |
| Deploy | [`.github/workflows/backend-deploy.yml`](../../.github/workflows/backend-deploy.yml) | Auto after Backend or per-app Frontend Publish (push or PR to `master`); bumps only images from that publish workflow; manual for `staging`/`production` |

### CI jobs (ordered with `needs` blockers)

```
format
├── gitleaks
└── build → test → docker-verify
```

- **Format** — `dotnet format backend.slnx --verify-no-changes` (runs first)
- **Gitleaks** — secret scan; waits for format
- **Build** — `dotnet restore/build` on `backend.slnx`; waits for format
- **Test** — `dotnet test`; waits for build (Testcontainers / Docker required for integration tests)
- **Docker verify** — builds all four Dockerfiles; waits for tests

Separate workflow (PR only — GitHub API limitation):

- **Dependency review** — fails on high+ severity new dependency vulnerabilities

### Images (GHCR)

- `ghcr.io/honguyenminh/amuse-api`
- `ghcr.io/honguyenminh/amuse-worker-transcoder`
- `ghcr.io/honguyenminh/amuse-worker-scheduler`
- `ghcr.io/honguyenminh/amuse-migrate`

Publish runs Trivy (`scanners: vuln`, CRITICAL/HIGH, `ignore-unfixed`, `limit-severities-for-sarif`) before push. Release images exclude `appsettings.Development.json`; secrets come from cluster env/External Secrets. Tags: floating tag (`master`, `staging`, `production`, or `pr-<number>`) + immutable `sha-<7-char>`. PR builds do **not** update the `master` floating tag.

### Deploy (GitOps via amuse-deploy)

Deploy does **not** commit to the `amuse` repo. After publish, `backend-deploy.yml` bumps image tags in [honguyenminh/amuse-deploy](https://github.com/honguyenminh/amuse-deploy); Argo CD syncs the cluster.

| GitHub Environment | Cluster | Deploy repo path | Default image tag |
|--------------------|---------|------------------|-------------------|
| `development` | K3s | `overlays/dev/images-tags/` | `master` or `pr-<n>` |
| `staging` | AKS | `overlays/stage/images-tags/` | `staging` |
| `production` | — | not yet defined | `production` |

Manifest structure changes in `infrastructure/kubernetes/` are synced to `amuse-deploy` by `sync-kubernetes-manifests.yml` on merge to `master`.

Requires `DEPLOY_REPO_TOKEN` on the **amuse** repo — see [DEPLOY_REPO.md](../../infrastructure/kubernetes/DEPLOY_REPO.md).

**Migrations:** `amuse-migrate` is an Argo CD **Sync hook** (`hook: Sync`, `sync-wave: 5`, `hook-delete-policy: BeforeHookCreation`). Backend deploy bumps the migrate image tag in `amuse-deploy`; Argo sync runs the hook once per that change, then applies apps at wave 10. API never migrates on startup ([local-development.md](./local-development.md)).

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

## Related docs & workflows

| Item | Location |
|------|----------|
| Frontend CI/CD | [../frontend/ci-cd.md](../frontend/ci-cd.md) |
| Sync manifests to deploy repo | [`.github/workflows/sync-kubernetes-manifests.yml`](../../.github/workflows/sync-kubernetes-manifests.yml) |
