# AKS stage cluster bootstrap

After `terraform apply` (see `infrastructure/terraform/README.md`):

**GitOps repo:** [amuse-deploy](https://github.com/honguyenminh/amuse-deploy) — Argo CD syncs `overlays/stage`.

## 1. Configure stage overlay placeholders

In **amuse-deploy** (after clone), update:

- `overlays/stage/external-secrets/cluster-secret-store.yaml` — `vaultUrl`
- `overlays/stage/workload-identity-patch.yaml` — `azure.workload.identity/client-id` from Terraform output `amuse_workload_identity_client_id`
- Confirm postgres secret name matches `{resource_prefix}-postgres-connection-string`

Commit these changes to **amuse-deploy** (or edit locally before first sync).

## 2. TLS and DNS

Create `amuse-tls` in namespace `amuse`. Point staging hosts at AGC frontend FQDN (Terraform output `agc_frontend_fqdn`):

- `api.staging.amuse.local` (or your domain)
- `app.staging.amuse.local`
- `business.staging.amuse.local`

## 3. GHCR pull secret

```bash
kubectl create secret docker-registry ghcr-pull -n amuse \
  --docker-server=ghcr.io \
  --docker-username=GITHUB_USER \
  --docker-password=GITHUB_PAT
```

## 4. Argo CD (installed by Terraform)

```bash
git clone https://github.com/honguyenminh/amuse-deploy.git
kubectl apply -f amuse-deploy/argocd/projects/amuse-project.yaml
kubectl apply -f amuse-deploy/argocd/bootstrap/stage-application.yaml
```

Do **not** apply `dev-application.yaml` on AKS.

## 5. Smoke test

```bash
kubectl -n amuse get externalsecret
kubectl -n amuse get gateway,httproute
```
