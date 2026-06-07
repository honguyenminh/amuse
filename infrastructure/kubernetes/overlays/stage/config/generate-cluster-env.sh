#!/usr/bin/env bash
set -euo pipefail

zone="${1:-}"
media_host="${2:-}"

if [ -z "$zone" ]; then
  echo "usage: $0 <zone> [media-cdn-host]" >&2
  echo "example: $0 amuse.m8.io.vn" >&2
  echo "  consumer at https://amuse.m8.io.vn (apex), api at https://api.amuse.m8.io.vn" >&2
  exit 1
fi

if [ -z "$media_host" ]; then
  media_host="media.${zone}"
fi

cat <<EOF
GATEWAY_WILDCARD_HOST=*.${zone}
API_HOST=api.${zone}
APP_HOST=${zone}
BUSINESS_HOST=business.${zone}
PUBLIC_API_BASE_URL=https://api.${zone}
APP_ORIGIN=https://${zone}
BUSINESS_ORIGIN=https://business.${zone}
MEDIA_PUBLIC_BASE_URL=https://${media_host}
MEDIA_CORS_ORIGINS=https://${zone},https://business.${zone}
EOF
