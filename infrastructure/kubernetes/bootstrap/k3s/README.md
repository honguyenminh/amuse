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

### 3. Traefik Gateway API entryPoints (one-time)

K3s doesnt expose the port that Gateway listeners use on **443**. Until they match, `amuse-gateway` stays `ListenersNotValid`:

```text
PortUnavailable: no matching entryPoint for port 443 and protocol "HTTPS"
```

In your `/var/lib/rancher/k3s/server/manifests/k3s-traefik-config.yaml`:

```yaml
providers:
  kubernetesGateway:
    enabled: true
ports:
  websecure:
    port: 443
    expose:
      default: true
    exposedPort: 443
    protocol: TCP
```

Confirm:

```bash
kubectl get gatewayclass                    # expect traefik
kubectl -n amuse get gateway amuse-gateway
kubectl -n amuse get httproute
```

### 4. DNS


| Host                            | Service     |
| ------------------------------- | ----------- |
| `api.skynet-beta.m8.io.vn`      | `amuse-api` |
| `app.skynet-beta.m8.io.vn`      | `consumer`  |
| `business.skynet-beta.m8.io.vn` | `business`  |


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

and then open the browser on [http://localhost:8080](http://localhost:8080) and accept the certificate

1. enable ingress in the values file `server.ingress.enabled` and either
  - Add the annotation for ssl passthrough: [https://argo-cd.readthedocs.io/en/stable/operator-manual/ingress/#option-1-ssl-passthrough](https://argo-cd.readthedocs.io/en/stable/operator-manual/ingress/#option-1-ssl-passthrough)
  - Set the `configs.params."server.insecure"` in the values file and terminate SSL at your ingress: [https://argo-cd.readthedocs.io/en/stable/operator-manual/ingress/#option-2-multiple-ingress-objects-and-hosts](https://argo-cd.readthedocs.io/en/stable/operator-manual/ingress/#option-2-multiple-ingress-objects-and-hosts)

After reaching the UI the first time you can login with username: admin and the random password generated during the installation. You can find the password by running:

```
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d
```

(You should delete the initial secret afterwards as suggested by the Getting Started Guide: [https://argo-cd.readthedocs.io/en/stable/getting_started/#4-login-using-the-cli](https://argo-cd.readthedocs.io/en/stable/getting_started/#4-login-using-the-cli))

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

## Database migrations (Argo Sync hook)

CI only bumps image tags in `amuse-deploy` — **no kubeconfig in GitHub**. Argo CD runs migrations via a **Sync hook** Job (`base/migrate/job.yaml`):

```yaml
argocd.argoproj.io/hook: Sync
argocd.argoproj.io/sync-wave: "5"
argocd.argoproj.io/hook-delete-policy: BeforeHookCreation
```

| When | What happens |
|------|----------------|
| Backend deploy bumps `amuse-migrate` tag | Argo sync → deletes prior hook Job → runs new migration → rolls apps (wave 10) |
| Idle auto-sync, no manifest diff | Hook **does not** re-run |
| Frontend-only deploy | Migrate tag unchanged → hook not recreated |

Do **not** use `Replace=true` or `HookSucceeded`-only on migrate — that was the old pattern that re-ran migrations on every reconcile.

Break-glass: force Argo to re-run the hook (uses the `amuse-migrate` image from `backend/Dockerfile.migrate`):

```bash
kubectl -n amuse delete job amuse-migrate --ignore-not-found
argocd app sync amuse-dev --force
kubectl -n amuse wait --for=condition=complete job/amuse-migrate --timeout=600s
```

## Argo CD sync waves (dev)

| Wave | Resources |
|------|-----------|
| `-5` | Secrets (`secrets-argo-patch.yaml`) |
| `0` | Postgres, MinIO, RabbitMQ, Gateway, HTTPRoutes |
| `5` | `amuse-migrate`, `minio-init` Argo **Sync hooks** |
| `10` | API, workers, frontends |

### “Missing” Jobs or Gateway in Argo

**`minio-init`** — Argo Sync hook with `hook-delete-policy: BeforeHookCreation`; recreated only when a sync runs the hook phase (not on idle auto-sync of unrelated resources).

**Gateway (`amuse-gateway`) synced but Degraded** — Common causes:


| Symptom                               | Fix                                                                                                               |
| ------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `no matching entryPoint for port 443` | Apply [traefik-helmchartconfig.yaml](traefik-helmchartconfig.yaml) (step 3 above)                                 |
| TLS / `certificateRefs` rejected      | `kubectl apply -f overlays/dev/reference-grant-wildcard-tls.yaml`; confirm `wildcard-tls-secret` in `kube-system` |
| Gateway never created                 | Argo sync errors, missing Gateway API CRDs, wrong `gatewayClassName`                                              |


```bash
kubectl apply -f overlays/dev/reference-grant-wildcard-tls.yaml
kubectl get secret wildcard-tls-secret -n kube-system
kubectl -n amuse describe gateway amuse-gateway
argocd app sync amuse-dev --force   # or your Application name
```

`**spec.listeners[0].port/protocol: Required value**` — Kustomize replaces the whole listener when overlay patches only set `hostname`/`tls` (Gateway API has no strategic-merge schema). Listener patches must include `protocol`, `port`, and `allowedRoutes`. Verify with `kubectl kustomize overlays/dev | rg -A20 'kind: Gateway'`.

### Manual migration (break-glass)

```bash
kubectl -n amuse delete job amuse-migrate --ignore-not-found
argocd app sync amuse-dev --force
kubectl -n amuse wait --for=condition=complete job/amuse-migrate --timeout=600s
kubectl -n amuse rollout restart deployment/amuse-api
```

