#!/usr/bin/env bash
set -euo pipefail

domain="${1:-}"
if [ -z "$domain" ]; then
  echo "usage: $0 <public-base-domain>" >&2
  echo "example: $0 skynet-beta.m8.io.vn" >&2
  exit 1
fi

cat <<EOF
GATEWAY_WILDCARD_HOST=*.${domain}
API_HOST=api.${domain}
APP_HOST=app.${domain}
BUSINESS_HOST=business.${domain}
PUBLIC_API_BASE_URL=https://api.${domain}
APP_ORIGIN=https://app.${domain}
BUSINESS_ORIGIN=https://business.${domain}
MEDIA_PUBLIC_BASE_URL=https://api.${domain}
MINIO_CORS_ALLOW_ORIGIN=https://app.${domain},https://business.${domain}
EOF
