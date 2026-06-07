# Amuse Kubernetes manifests (source templates)

Manifest **structure** for Amuse on Kubernetes. **Live cluster state** is in the GitOps deploy repo:

**[github.com/honguyenminh/amuse-deploy](https://github.com/honguyenminh/amuse-deploy)**

| Repo | Role |
|------|------|
| **amuse** (`infrastructure/kubernetes/`) | Source templates — edit here in PRs |
| **amuse-deploy** | What Argo CD syncs; CI bumps image tags here |

## Layout

| Path | Purpose |
|------|---------|
| `base/` | Shared Deployments, Services, Gateway API routes, RabbitMQ |
| `overlays/dev/` | K3s: Traefik, in-cluster Postgres/MinIO; hosts from `config/cluster.env` |
| `overlays/clusters/*/` | Optional per-cluster wrapper (own `cluster.env`, points Argo CD at a second dev env) |
| `overlays/stage/` | AKS: AGC, External Secrets, R2 media |
| `overlays/*/images-tags/` | Image tag component (local defaults; **live tags in amuse-deploy**) |

Changes merged to `master` under `infrastructure/kubernetes/**` are synced to `amuse-deploy` by [`.github/workflows/sync-kubernetes-manifests.yml`](../../.github/workflows/sync-kubernetes-manifests.yml).

Image releases are deployed by [`.github/workflows/backend-deploy.yml`](../../.github/workflows/backend-deploy.yml), which commits tag bumps to `amuse-deploy`. Argo CD syncs the cluster and runs the `amuse-migrate` **Sync hook** before app rollouts (no `KUBE_CONFIG` in CI).

See [DEPLOY_REPO.md](DEPLOY_REPO.md) for `DEPLOY_REPO_TOKEN`.

## Local validation

```bash
kubectl kustomize infrastructure/kubernetes/overlays/dev
kubectl kustomize infrastructure/kubernetes/overlays/stage
```

## Bootstrap (Argo CD, TLS, clusters)

Clone **amuse-deploy** and follow:

- K3s dev: [bootstrap/k3s/README.md](bootstrap/k3s/README.md)
- AKS stage: [bootstrap/aks/README.md](bootstrap/aks/README.md)

Argo CD `Application` CRs live only in **amuse-deploy** (`argocd/`).

## GitHub secret required

Add `DEPLOY_REPO_TOKEN` on the **amuse** repo — see [DEPLOY_REPO.md](DEPLOY_REPO.md).
