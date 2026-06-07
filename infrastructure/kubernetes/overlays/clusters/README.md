# Per-cluster dev overlays

Use this when you run **more than one** K3s dev environment (different DNS / TLS / Argo CD apps).

Each subdirectory is a thin wrapper around `overlays/dev/` with its own `cluster.env`.

## Layout

```
clusters/
  skynet-beta/          # current default dev cluster
    kustomization.yaml
    cluster.env
  my-other-dev/         # copy skynet-beta/ and edit cluster.env
    kustomization.yaml
    cluster.env
```

Point the Argo CD `Application` `spec.source.path` at `overlays/clusters/<name>` instead of `overlays/dev`.

## Add a new dev cluster

```bash
cp -r infrastructure/kubernetes/overlays/clusters/skynet-beta \
      infrastructure/kubernetes/overlays/clusters/my-other-dev
cd infrastructure/kubernetes/overlays/dev/config
./generate-cluster-env.sh my-other.dev.example > ../../clusters/my-other-dev/cluster.env
```

Ensure DNS and a matching wildcard certificate exist for `*.my-other.dev.example`, then create a new Argo CD Application targeting `overlays/clusters/my-other-dev`.

## Single dev cluster

If you only have one dev environment, edit `overlays/dev/config/cluster.env` directly (no need for `clusters/`).
