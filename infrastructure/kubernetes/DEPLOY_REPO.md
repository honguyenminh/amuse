# Deploy repo and `DEPLOY_REPO_TOKEN`

Amuse uses two repositories for GitOps:

| Repository | URL |
|------------|-----|
| Application + manifest templates | `github.com/honguyenminh/amuse` |
| Live cluster desired state | [github.com/honguyenminh/amuse-deploy](https://github.com/honguyenminh/amuse-deploy) |

## Workflows that write to amuse-deploy

| Workflow | What it commits |
|----------|-----------------|
| `sync-kubernetes-manifests.yml` | `base/` + `overlays/` structure (excludes `images-tags/`, `secrets.yaml`) |
| `backend-deploy.yml` | `overlays/*/images-tags/kustomization.yaml` only |

## Create `DEPLOY_REPO_TOKEN`

On the **amuse** repository → Settings → Secrets and variables → Actions → New repository secret:

**Name:** `DEPLOY_REPO_TOKEN`

**Value:** a fine-grained PAT or classic PAT with **Contents: Read and write** on `honguyenminh/amuse-deploy` only.

### Fine-grained PAT (recommended)

1. GitHub → Settings → Developer settings → Fine-grained tokens
2. Repository access: **Only select repositories** → `amuse-deploy`
3. Permissions: **Contents** → Read and write
4. Copy token into `DEPLOY_REPO_TOKEN` on `amuse`

### Classic PAT

Scope: `repo` (or limited to the deploy repo if using fine-grained equivalent).

## Branch protection on amuse-deploy

On **amuse-deploy** → Settings → Branches → `main`:

- Allow GitHub Actions to push (default for repos without strict rules)
- Optional: require PR review for human commits; exempt `github-actions[bot]` if your plan supports bot bypass
- Do **not** require status checks that would block the sync/deploy workflows unless those checks run on deploy repo PRs

## Branch protection on amuse

`master` no longer needs bot write access for deploy commits — cleaner git history on the app repo.

## Argo CD

Register [amuse-deploy](https://github.com/honguyenminh/amuse-deploy) in Argo CD (public repo needs no credentials).

Apply Applications from a clone of **amuse-deploy**:

```bash
git clone https://github.com/honguyenminh/amuse-deploy.git
cd amuse-deploy
kubectl apply -f argocd/projects/amuse-project.yaml
kubectl apply -f argocd/bootstrap/dev-application.yaml    # K3s only
kubectl apply -f argocd/bootstrap/stage-application.yaml  # AKS only
```
