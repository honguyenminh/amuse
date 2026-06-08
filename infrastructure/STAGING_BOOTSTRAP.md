# Staging bootstrap: `amuse.m8.io.vn` on Azure + Cloudflare R2

End-to-end guide from **zero** to a working stage environment with DASH playback.

| Host | Service |
|------|---------|
| `https://amuse.m8.io.vn` | Consumer (zone apex) |
| `https://api.amuse.m8.io.vn` | API (+ DASH manifest proxy) |
| `https://business.amuse.m8.io.vn` | Business portal |
| `https://media.amuse.m8.io.vn` | Cover art (R2 public custom domain) |

Audio segments are **not** on `media.*` — the API redirects the browser to presigned URLs on `*.r2.cloudflarestorage.com`.

---

## Phase 0 — Prerequisites

### Accounts & tools

- [ ] Azure subscription with **Contributor** (or enough to create RG, AKS, Key Vault, Postgres, AGC)
- [ ] **Cloudflare** account with DNS zone `m8.io.vn` (or delegated `amuse.m8.io.vn`)
- [ ] GitHub access to `honguyenminh/amuse` and `honguyenminh/amuse-deploy`
- [ ] Local tools: `az`, `terraform` ≥ 1.11, `kubectl`, `helm`, `pnpm` (for local checks only)

### GitHub secret (amuse repo)

Before manifests sync works:

1. Create a fine-grained PAT with **Contents: Read and write** on `amuse-deploy` only.
2. Add to **amuse** → Settings → Secrets → Actions: `DEPLOY_REPO_TOKEN`

### Generate local secret values (save in a password manager)

```bash
# JWT signing key (min 32 chars)
openssl rand -base64 48

# RabbitMQ / Redis passwords
openssl rand -base64 24
```

You will paste these into `staging.tfvars` in Phase 2.

---

## Phase 1 — Cloudflare R2 (no Azure yet)

Do this in the **Cloudflare dashboard** (R2 must be enabled on your account).

### 1.1 Create buckets

R2 → Create bucket:

| Bucket name | Access |
|-------------|--------|
| `amuse-covers` | Public (custom domain in step 1.2) |
| `amuse-audio` | Private |

### 1.2 Cover CDN custom domain

On bucket **`amuse-covers`** → Settings → **Custom Domains** → Connect domain:

- Domain: `media.amuse.m8.io.vn`
- Cloudflare will add the required DNS record in zone `m8.io.vn` if the zone is on Cloudflare.

This URL becomes `Media__PublicBaseUrl` everywhere.

### 1.3 R2 API token

R2 → **Manage R2 API Tokens** → Create token:

- Permissions: **Object Read & Write** on both buckets (or account-level Admin Read & Write for simplicity while bootstrapping).
- Copy **Access Key ID**, **Secret Access Key**, and note your **Account ID**.

Your S3 API endpoint:

```text
https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

### 1.4 CORS on both buckets

For **each** bucket (`amuse-covers`, `amuse-audio`) → Settings → **CORS policy** → paste:

```json
[
  {
    "AllowedOrigins": [
      "https://amuse.m8.io.vn",
      "https://business.amuse.m8.io.vn"
    ],
    "AllowedMethods": ["GET", "HEAD", "PUT"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": [
      "ETag",
      "Accept-Ranges",
      "Content-Length",
      "Content-Range",
      "Content-Type"
    ],
    "MaxAgeSeconds": 3600
  }
]
```

Required for cover art (`crossOrigin="anonymous"`), business uploads (presigned PUT), and DASH segment fetches after API 302.

### 1.5 Checklist before Azure

- [ ] Buckets exist
- [ ] `https://media.amuse.m8.io.vn` resolves (may 404 until objects exist — that is OK)
- [ ] API token + Account ID saved
- [ ] CORS on both buckets

---

## Phase 2 — Azure platform (Terraform)

### 2.1 Configure tfvars

```bash
cd infrastructure/terraform
cp environments/staging.tfvars.example environments/staging.tfvars
```

**Images:** CI publishes to **GHCR** (`ghcr.io/honguyenminh/amuse-*`). Kubernetes pulls via the `ghcr-pull` secret. Terraform does **not** provision Azure Container Registry.

Edit `environments/staging.tfvars` — minimum fields:

| Variable | Your value |
|----------|------------|
| `subscription_id` | Azure subscription GUID |
| `resource_prefix` | e.g. `uit-amuse` |
| `location` | e.g. `japaneast` |
| `amuse_jwt_signing_key` | output of `openssl rand -base64 48` |
| `amuse_r2_endpoint` | `https://<ACCOUNT_ID>.r2.cloudflarestorage.com` |
| `amuse_r2_presign_base_url` | **same** as `amuse_r2_endpoint` |
| `amuse_r2_public_base_url` | `https://media.amuse.m8.io.vn` |
| `amuse_r2_access_key` / `amuse_r2_secret_key` | from Phase 1.3 |
| `amuse_rabbitmq_password` / `amuse_redis_password` | generated above |
| `amuse_smtp_*` | real SMTP or placeholders if email can wait |

Bump `*_version` counters when rotating write-only secrets later.

### 2.2 Login and apply

```bash
az login
az account set --subscription "<SUBSCRIPTION_ID>"

terraform init
# First bootstrap (split apply):
terraform apply -target=module.resource-group \
  -target=module.networking \
  -target=module.key_vault \
  -target=module.postgres \
  -target=module.app-gw \
  -target=module.aks \
  -var-file=environments/staging.tfvars

terraform apply -var-file=environments/staging.tfvars
```

Full apply installs **Argo CD**, **External Secrets Operator**, and wires **Workload Identity** → Key Vault.

### 2.3 Save Terraform outputs

```bash
terraform output -json > /tmp/amuse-staging-outputs.json

terraform output agc_frontend_fqdn
terraform output key_vault_uri
terraform output amuse_workload_identity_client_id
terraform output postgres_connection_string_secret_name
terraform output aks_cluster_name
```

### 2.4 kubectl context

```bash
az aks get-credentials --resource-group "$(terraform output -raw resource_group_name)" \
  --name "$(terraform output -raw aks_cluster_name)"
kubectl get nodes
```

---

## Phase 3 — TLS certificate for the gateway

The stage Gateway needs a cert covering **both**:

- `amuse.m8.io.vn` (consumer apex)
- `*.amuse.m8.io.vn` (api, business, etc.)

Wildcard `*.amuse.m8.io.vn` does **not** cover the apex.

### Option A — Reuse an existing cert

If you already have a PEM/fullchain for `amuse.m8.io.vn` + SAN `*.amuse.m8.io.vn`:

```bash
kubectl create namespace amuse --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret tls amuse-tls -n amuse \
  --cert=fullchain.pem \
  --key=privkey.pem
```

### Option B — cert-manager + Cloudflare DNS (recommended long-term)

Install cert-manager on AKS, create a `ClusterIssuer` with Cloudflare API token, and a `Certificate` in namespace `amuse` with `spec.secretName: amuse-tls` and DNS names `amuse.m8.io.vn`, `*.amuse.m8.io.vn`.

```bash
kubectl create namespace amuse --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -f amuse-certificate.yaml   # secretName: amuse-tls
kubectl wait --for=condition=Ready certificate/amuse-tls-cert -n amuse --timeout=10m
kubectl get secret amuse-tls -n amuse
```

The stage Gateway already references `amuse-tls` in both HTTPS listeners.

---

## Phase 4 — DNS (application hosts → Azure AGC)

From Terraform `agc_frontend_fqdn` (Azure-managed hostname), create **proxied or DNS-only CNAMEs** in Cloudflare:

| Name | Target |
|------|--------|
| `api.amuse` | `<agc_frontend_fqdn>` |
| `business.amuse` | `<agc_frontend_fqdn>` |
| `amuse` (apex) | `<agc_frontend_fqdn>` |

`media.amuse` is usually created automatically when you connected the R2 custom domain (Phase 1.2). Do **not** point `media.amuse` at AGC.

Wait for propagation, then:

```bash
dig +short api.amuse.m8.io.vn
dig +short amuse.m8.io.vn
```

---

## Phase 5 — GitOps (amuse-deploy + Argo CD)

### 5.1 Sync manifest templates from amuse

Merge/push `infrastructure/kubernetes/**` changes to **amuse** `master`. Workflow `sync-kubernetes-manifests.yml` rsyncs into **amuse-deploy** (needs `DEPLOY_REPO_TOKEN`).

Or manually clone and copy if the workflow is not run yet.

### 5.2 Edit amuse-deploy for your cluster

Clone **amuse-deploy** and set placeholders:

**`overlays/stage/config/cluster.env`** — copy from `cluster.env.example` and set (from Terraform outputs):

| Key | Source |
|-----|--------|
| `AMUSE_WORKLOAD_IDENTITY_CLIENT_ID` | `terraform output -raw amuse_workload_identity_client_id` |
| `KEY_VAULT_URI` | `terraform output -raw key_vault_uri` |
| `AGC_ALB_ARM_ID` | `terraform output -raw agc_alb_id` |
| `AGC_ALB_FRONTEND` | `agc_frontend_name` in tfvars (e.g. `amuse-staging-fe`) |

Kustomize replacements inject these into ServiceAccounts, ClusterSecretStore, and Gateway. Do **not** edit `workload-identity-patch.yaml` by hand.

**`overlays/stage/config/cluster.env`** — should match (already in amuse repo):

```env
GATEWAY_WILDCARD_HOST=*.amuse.m8.io.vn
APP_HOST=amuse.m8.io.vn
API_HOST=api.amuse.m8.io.vn
...
MEDIA_PUBLIC_BASE_URL=https://media.amuse.m8.io.vn
```

**Postgres secret name** in ExternalSecrets — must match `terraform output postgres_connection_string_secret_name` (default `uit-amuse-postgres-connection-string` if `resource_prefix = uit-amuse`).

Commit and push **amuse-deploy** `main`.

### 5.3 GHCR pull secret

```bash
kubectl create secret docker-registry ghcr-pull -n amuse \
  --docker-server=ghcr.io \
  --docker-username=<GITHUB_USER> \
  --docker-password=<GITHUB_PAT_WITH_read:packages>
```

### 5.4 Register Argo CD application (creates Gateway + HTTPRoutes)

**The Gateway is not created by cert-manager or Terraform** — it comes from GitOps (`overlays/stage` in amuse-deploy). Until this step runs, `kubectl get gateway -A` is empty.

```bash
git clone https://github.com/honguyenminh/amuse-deploy.git
kubectl apply -f amuse-deploy/argocd/projects/amuse-project.yaml
kubectl apply -f amuse-deploy/argocd/bootstrap/stage-application.yaml
```

After sync:

```bash
kubectl -n argocd get applications
kubectl -n amuse get gateway amuse-gateway,httproute
```

**AGC wiring:** `overlays/stage/config/cluster.env` must include `AGC_ALB_ARM_ID` (from `terraform output -raw agc_alb_id`) and `AGC_ALB_FRONTEND` (matches `agc_frontend_name` in tfvars). Terraform also provisions the ALB controller managed identity (`albController.podIdentity.clientID`).

Watch sync:

```bash
kubectl -n argocd get applications
kubectl -n amuse get externalsecret
kubectl -n amuse get gateway,httproute,pods
```

ExternalSecrets should reach `SecretSynced`. If not, check Workload Identity client-id and Key Vault RBAC.

---

## Phase 6 — Build and deploy application images

Stage manifests reference GHCR tags **`staging`** for all six images (`amuse-api`, workers, `amuse-migrate`, `amuse-consumer`, `amuse-business`). **Argo sync will fail** (ImagePullBackOff / migrate hook errors) until every one of those tags exists in the registry.

Stage images are published from the **`staging` branch** (create it if missing).

### 6.0 Create the `staging` branch (first time only)

If `staging` does not exist on GitHub yet:

```bash
git fetch origin master
git checkout -b staging origin/master
git push -u origin staging
```

**Alternative (no branch yet):** run each publish workflow via **workflow_dispatch** on **amuse** with `image_tag` = `staging` (builds from the branch you select when starting the run):

- Backend Publish
- Frontend Consumer Publish
- Frontend Business Publish

Confirm packages exist under `ghcr.io/honguyenminh/amuse-*` with a `staging` tag before expecting Argo to succeed.

### 6.1 Publish images

1. Push/merge to **`staging`** branch on **amuse**.
2. Wait for CI + publish workflows:
   - `backend-publish.yml`
   - `frontend-consumer-publish.yml`
   - `frontend-business-publish.yml`

Consumer/business images bake `NEXT_PUBLIC_API_BASE_URL=https://api.amuse.m8.io.vn` at build time.

### 6.2 Bump live tags in amuse-deploy

Pushes to **`staging`** auto-bump the matching image(s) in `amuse-deploy/overlays/stage/images-tags/` after each publish workflow (same pattern as dev).

**One-shot (all six images):** GitHub → **amuse** → Actions → **Backend Deploy** → Run workflow:

- Environment: `staging`
- (optional) image tag override: `staging`

Use this after the first bootstrap publish pass, or to re-point every workload at the same tag.

Argo auto-sync rolls the cluster and runs the **migrate Sync hook** when `amuse-deploy` changes.

---

## Phase 7 — Verification

### 7.1 Cluster smoke

```bash
./infrastructure/kubernetes/scripts/verify-stage-media.sh
```

Expect:

```text
Media__Endpoint=https://<account>.r2.cloudflarestorage.com
Media__PublicBaseUrl=https://media.amuse.m8.io.vn
Media__PresignBaseUrl=https://<account>.r2.cloudflarestorage.com
```

Consumer ConfigMap: `MEDIA_PUBLIC_BASE_URL=https://media.amuse.m8.io.vn`

### 7.2 API health

```bash
curl -sS "https://api.amuse.m8.io.vn/openapi/v1.json" | head
```

### 7.3 Gateway routes

```bash
kubectl -n amuse get gateway amuse-gateway -o yaml | rg 'hostname|reason'
kubectl -n amuse get httproute
```

Consumer route must attach to listener `https-apex` (apex host).

### 7.4 Playback

1. Open `https://business.amuse.m8.io.vn` — sign in, upload a track master (presigned PUT → R2).
2. Wait for transcoder pod to finish (`kubectl -n amuse logs -l app.kubernetes.io/name=amuse-worker-transcoder -f`).
3. Open `https://amuse.m8.io.vn` — play the track.
4. DevTools → Network:
   - `manifest.mpd` → `api.amuse.m8.io.vn` (authorized)
   - `*.m4s` → 302 → `*.r2.cloudflarestorage.com`

If segments fail with CORS, re-check Phase 1.4. If 403 on presign, check `Media__PresignBaseUrl` matches the R2 S3 endpoint.

---

## Troubleshooting

| Symptom | What to check |
|---------|----------------|
| ExternalSecret not synced | WI client-id, KV URL, federated credential subject matches `system:serviceaccount:amuse:amuse-api` |
| Gateway not Ready | AGC controller pods, `amuse-tls` secret exists, listener hostnames match cert SANs |
| Consumer 404 / wrong host | HTTPRoute `amuse-consumer` on `https-apex`, DNS apex → AGC |
| Covers broken, playback OK | `MEDIA_PUBLIC_BASE_URL` vs API cover URLs; R2 public domain on `amuse-covers` |
| `track_stream_not_ready` | Transcoder logs; R2 credentials; Postgres connectivity |
| Argo keeps old images | Run **Backend Deploy** workflow for `staging` |
| Sync fails / ImagePullBackOff on `:staging` | `staging` branch missing or publish not finished — complete **Phase 6.0–6.1**; verify GHCR has all six `staging` tags |

---

## Order summary (quick reference)

```text
1. R2 buckets + media.amuse.m8.io.vn + CORS + API token
2. terraform apply → save outputs
3. amuse-tls secret (apex + wildcard)
4. DNS: api / business / amuse apex → agc_frontend_fqdn
5. amuse-deploy placeholders → Argo stage app
6. ghcr-pull secret
7. staging branch publish → Backend Deploy (staging)
8. verify-stage-media.sh + play a track
```

---

## Related docs

- [`cloudflare/README.md`](cloudflare/README.md) — R2 details
- [`kubernetes/bootstrap/aks/README.md`](kubernetes/bootstrap/aks/README.md) — AKS notes
- [`kubernetes/DEPLOY_REPO.md`](kubernetes/DEPLOY_REPO.md) — `DEPLOY_REPO_TOKEN`
- [`terraform/README.md`](terraform/README.md) — Terraform apply order
