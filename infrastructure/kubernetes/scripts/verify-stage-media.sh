#!/usr/bin/env bash
# Smoke-check AKS stage: External Secrets, gateway, and API media configuration.
set -euo pipefail

NS="${AMUSE_NAMESPACE:-amuse}"
API_DEPLOY="${AMUSE_API_DEPLOY:-amuse-api}"

echo "== ExternalSecrets (${NS}) =="
kubectl -n "$NS" get externalsecret -o custom-columns=NAME:.metadata.name,READY:.status.conditions[0].reason,STATUS:.status.conditions[0].status 2>/dev/null || kubectl -n "$NS" get externalsecret

echo
echo "== Gateway / HTTPRoutes =="
kubectl -n "$NS" get gateway,httproute

echo
echo "== Frontend media env (consumer) =="
kubectl -n "$NS" get configmap amuse-frontend-env -o jsonpath='{.data.MEDIA_PUBLIC_BASE_URL}{"\n"}' 2>/dev/null || echo "(MEDIA_PUBLIC_BASE_URL not set)"

echo
echo "== API Media__* env (from ${API_DEPLOY} pod) =="
POD="$(kubectl -n "$NS" get pods -l app.kubernetes.io/name=amuse-api -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)"
if [ -z "$POD" ]; then
  echo "No amuse-api pod found."
  exit 1
fi

kubectl -n "$NS" exec "$POD" -- printenv 2>/dev/null | rg '^Media__' | sort || {
  echo "Could not read Media__ env from pod."
  exit 1
}

echo
echo "== Transcoder pod =="
kubectl -n "$NS" get pods -l app.kubernetes.io/name=amuse-worker-transcoder -o wide 2>/dev/null || echo "(no transcoder deployment)"

echo
echo "OK — compare Media__PublicBaseUrl with cluster.env MEDIA_PUBLIC_BASE_URL and Key Vault."
echo "See infrastructure/cloudflare/README.md for R2 CORS and CDN setup."
