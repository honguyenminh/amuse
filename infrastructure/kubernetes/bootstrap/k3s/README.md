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

**NOTES:**

In order to access the server UI you have the following options:

1. `kubectl port-forward service/argocd-server -n argocd 8080:443`

and then open the browser on http://localhost:8080 and accept the certificate

2. enable ingress in the values file `server.ingress.enabled` and either
  - Add the annotation for ssl passthrough: https://argo-cd.readthedocs.io/en/stable/operator-manual/ingress/#option-1-ssl-passthrough
  - Set the `configs.params."server.insecure"` in the values file and terminate SSL at your ingress: https://argo-cd.readthedocs.io/en/stable/operator-manual/ingress/#option-2-multiple-ingress-objects-and-hosts


After reaching the UI the first time you can login with username: admin and the random password generated during the installation. You can find the password by running:

```
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d
```

(You should delete the initial secret afterwards as suggested by the Getting Started Guide: https://argo-cd.readthedocs.io/en/stable/getting_started/#4-login-using-the-cli)

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

## Argo CD: migrations before API

Kubernetes has **no** native “Deployment depends on Job”. Ordering is done with **Argo CD sync waves**:

| Wave | Resources |
|------|-----------|
| `-5` | Secrets (`secrets-argo-patch.yaml`) |
| `0` | Postgres, MinIO, RabbitMQ, Gateway, HTTPRoutes |
| `5` | `amuse-migrate`, `minio-init` Jobs (Argo waits for Job **Succeeded**) |
| `10` | API, workers, frontends |

The API Deployment does not reference the migrate Job in its spec — Argo applies wave 10 only after wave 5 Jobs complete.

### “Missing” Jobs or Gateway in Argo

**Jobs (`amuse-migrate`, `minio-init`)** — If `ttlSecondsAfterFinished` deleted a completed Job, Argo shows **Missing** until the next sync recreates it. Manifests now omit TTL and use `Replace=true` so sync re-runs migrations when needed.

**Gateway (`amuse-gateway`)** — Usually not created because prerequisites failed:

```bash
kubectl get gatewayclass                    # expect traefik
kubectl apply -f overlays/dev/reference-grant-wildcard-tls.yaml
kubectl get secret wildcard-tls-secret -n kube-system
kubectl -n amuse describe gateway amuse-gateway   # after sync
argocd app sync amuse-dev --force   # or your Application name
```

If the Gateway never appears, check Argo **Sync** errors (invalid `certificateRefs`, missing Gateway API CRDs, wrong `gatewayClassName`).

### Manual migration (break-glass)

```bash
kubectl -n amuse delete job amuse-migrate --ignore-not-found
kubectl -n amuse apply -k overlays/dev   # from amuse-deploy clone, or sync in Argo
kubectl -n amuse wait --for=condition=complete job/amuse-migrate --timeout=300s
kubectl -n amuse rollout restart deployment/amuse-api
```
