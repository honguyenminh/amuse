# Stage cluster host configuration

Public hostnames and browser-facing URLs for the AKS stage overlay are defined in **`cluster.env`** (single source of truth).

Kustomize generates `amuse-cluster-config` and applies [`replacements.yaml`](./replacements.yaml) to:

- Gateway listener wildcard host
- HTTPRoute hostnames (api / app / business)
- API CORS and tenancy URLs
- Frontend `NEXT_PUBLIC_API_BASE_URL`, `NEXT_PUBLIC_SITE_URL`, `MEDIA_PUBLIC_BASE_URL`

## Media URLs (R2 + CDN)

| Setting | Where | Purpose |
|---------|-------|---------|
| `MEDIA_PUBLIC_BASE_URL` | `cluster.env` → consumer ConfigMap | Cover art SSR allowlist; must match Key Vault `amuse-r2-public-base-url` |
| `Media__PublicBaseUrl` | Key Vault → `amuse-api-secrets` | Cover URLs returned by API |
| `Media__PresignBaseUrl` | Key Vault → `amuse-api-secrets` | Presigned DASH segment + upload URLs (R2 S3 API host) |
| `Media__Endpoint` | Key Vault | Server-side SDK + transcoder internal reads |

Stage does **not** proxy `/amuse-covers` on the API host (unlike dev MinIO). Covers are served from the CDN/R2 public origin; audio segments use presigned redirects to the R2 S3 API endpoint.

See [`../../../cloudflare/README.md`](../../../cloudflare/README.md) for bucket and CORS setup.

## Change hosts

Edit `cluster.env`, or regenerate:

```bash
./generate-cluster-env.sh staging.example.com media.staging.example.com
kubectl kustomize ../..   # from overlays/stage
```

After changing `MEDIA_PUBLIC_BASE_URL`, update Key Vault `amuse-r2-public-base-url` to the same value and refresh ExternalSecrets.
