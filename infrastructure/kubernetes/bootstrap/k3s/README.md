# K3s dev cluster bootstrap

Dev runs on an existing K3s cluster with Traefik and cert-manager already configured.

**GitOps repo:** clone [amuse-deploy](https://github.com/honguyenminh/amuse-deploy) — Argo CD syncs `overlays/dev` from that repo, not from `amuse`.

## TLS: reuse existing wildcard Certificate

If cert-manager already issued a wildcard (example: `wildcard-cert` in `kube-system` → secret `wildcard-tls-secret` for `*.skynet-beta.m8.io.vn`):

### 1. Confirm the secret exists

```bash
kubectl get certificate wildcard-cert -n kube-system
kubectl get secret wildcard-tls-secret -n kube-system
```

### 2. Allow cross-namespace TLS reference (one-time)

```bash
git clone https://github.com/honguyenminh/amuse-deploy.git
kubectl apply -f amuse-deploy/overlays/dev/reference-grant-wildcard-tls.yaml
```

### 3. Verify Gateway API (Traefik)

```bash
kubectl get gatewayclass
```

### 4. DNS

| Host | Service |
|------|---------|
| `api.skynet-beta.m8.io.vn` | `amuse-api` |
| `app.skynet-beta.m8.io.vn` | `consumer` |
| `business.skynet-beta.m8.io.vn` | `business` |

### 5. GHCR pull secret

```bash
kubectl create namespace amuse --dry-run=client -o yaml | kubectl apply -f -
kubectl create secret docker-registry ghcr-pull -n amuse \
  --docker-server=ghcr.io \
  --docker-username=GITHUB_USER \
  --docker-password=GITHUB_PAT
```

### 6. Install Argo CD

```bash
kubectl create namespace argocd
helm repo add argo https://argoproj.github.io/argo-helm
helm install argocd argo/argo-cd -n argocd --wait
```

### 7. Deploy GitOps (dev only)

From the **amuse-deploy** clone:

```bash
kubectl apply -f argocd/projects/amuse-project.yaml
kubectl apply -f argocd/bootstrap/dev-application.yaml
```

Do **not** apply `stage-application.yaml` on K3s.

### 8. Smoke test

```bash
kubectl -n amuse get gateway amuse-gateway
kubectl -n amuse get pods
curl -k https://api.skynet-beta.m8.io.vn/openapi/v1.json
```
