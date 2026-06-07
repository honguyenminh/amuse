# Dev cluster host configuration

Public hostnames and browser-facing URLs for the K3s dev overlay are defined in **`cluster.env`** (single source of truth).

Kustomize generates `amuse-cluster-config` and applies [`replacements.yaml`](./replacements.yaml) to:

- Gateway listener wildcard host
- HTTPRoute hostnames (api / app / business)
- API CORS, tenancy, and `Media__PublicBaseUrl` (ConfigMap env only)
- Frontend `NEXT_PUBLIC_API_BASE_URL`
- MinIO init job CORS (`MINIO_CORS_ALLOW_ORIGIN`)

`Media__PublicBaseUrl` in **Secrets** (`amuse-api-secrets`, `amuse-worker-transcoder-secrets`) is set when you bootstrap secrets — see [`../secrets.example.yaml`](../secrets.example.yaml) and [bootstrap/k3s/README.md](../../../bootstrap/k3s/README.md). GitOps does not manage dev secrets.

MinIO remains cluster-internal; media is served via `https://<API_HOST>/amuse-covers/*` and `/amuse-audio/*`.

## Change hosts for the default dev overlay

Edit `cluster.env`, or regenerate:

```bash
./generate-cluster-env.sh your.domain.example > cluster.env
kubectl kustomize ../../..   # from overlays/dev — or overlays/clusters/<name>
```

## Multiple dev clusters

See [`../clusters/README.md`](../clusters/README.md).
