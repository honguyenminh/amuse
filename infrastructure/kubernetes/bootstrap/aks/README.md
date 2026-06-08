# AKS stage cluster bootstrap

**Full walkthrough (zero → playback):** [`infrastructure/STAGING_BOOTSTRAP.md`](../../STAGING_BOOTSTRAP.md)

After `terraform apply` (see `infrastructure/terraform/README.md`):

**GitOps repo:** [amuse-deploy](https://github.com/honguyenminh/amuse-deploy) — Argo CD syncs `overlays/stage`.

## 1. Cloudflare R2 + CDN

Before first playback works, complete [`infrastructure/cloudflare/README.md`](../../cloudflare/README.md):

- Buckets `amuse-covers` (public custom domain) and `amuse-audio` (private)
- R2 CORS for app/business origins
- Terraform `staging.tfvars` R2 values → Key Vault (including `amuse_r2_presign_base_url`)

## 2. Configure stage overlay placeholders

In **amuse-deploy** (after clone), update:

- `overlays/stage/external-secrets/cluster-secret-store.yaml` — `vaultUrl`
- `overlays/stage/workload-identity-patch.yaml` — `azure.workload.identity/client-id` from Terraform output `amuse_workload_identity_client_id`
- `overlays/stage/config/cluster.env` — real hosts and `MEDIA_PUBLIC_BASE_URL` (CDN domain)
- Confirm postgres secret name matches `{resource_prefix}-postgres-connection-string`

Align Key Vault `amuse-r2-public-base-url` with `MEDIA_PUBLIC_BASE_URL` in `cluster.env`.

Commit these changes to **amuse-deploy** (or edit locally before first sync).

## 3. TLS and DNS

Create `amuse-tls` in namespace `amuse`. Point staging hosts at AGC frontend FQDN (Terraform output `agc_frontend_fqdn`):

- `api.<your-domain>`
- `app.<your-domain>`
- `business.<your-domain>`
- `media.<your-domain>` (R2 covers CDN — DNS per Cloudflare)

Use a **real public domain** for media CDN (not `.local`) so consumer cover-art SSR allowlist accepts the origin.

## 4. GHCR pull secret

```bash
kubectl create secret docker-registry ghcr-pull -n amuse \
  --docker-server=ghcr.io \
  --docker-username=GITHUB_USER \
  --docker-password=GITHUB_PAT
```

## 5. Argo CD (installed by Terraform)

```bash
git clone https://github.com/honguyenminh/amuse-deploy.git
kubectl apply -f amuse-deploy/argocd/projects/amuse-project.yaml
kubectl apply -f amuse-deploy/argocd/bootstrap/stage-application.yaml
```

Do **not** apply `dev-application.yaml` on AKS.

## 6. Deploy images to stage

Publishing to the `staging` branch builds GHCR images tagged `staging` and auto-updates matching tags in `amuse-deploy` (same as dev on `master`).

Before first Argo sync, ensure the **`staging` branch exists** and all six publish workflows have completed — otherwise manifests reference `:staging` images that are not in GHCR yet.

One-shot: run **Backend Deploy** (`workflow_dispatch`, environment `staging`) to bump every image tag after bootstrap.

## 7. Smoke test

```bash
kubectl -n amuse get externalsecret
kubectl -n amuse get gateway,httproute
./infrastructure/kubernetes/scripts/verify-stage-media.sh
```

Playback: log into consumer, play a track with completed DASH packaging. Network tab should show manifest on API host and segments redirected to `*.r2.cloudflarestorage.com`.
