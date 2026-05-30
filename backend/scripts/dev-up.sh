#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE="${COMPOSE:-docker compose}"
if ! command -v docker >/dev/null 2>&1 && command -v podman >/dev/null 2>&1; then
  COMPOSE="podman compose"
fi

echo "Starting local backend stack (infra + migrate + API + worker)..."
echo "Frontend: run Next.js on the host (e.g. pnpm dev in frontend/business)."
echo "API:      http://localhost:5000"
echo

$COMPOSE up -d --build

echo
echo "Stack is starting. Useful URLs:"
echo "  API (OpenAPI):  http://localhost:5000/openapi/v1.json"
echo "  MinIO console:  http://localhost:9001  (amuse / amuse_dev_secret)"
echo "  RabbitMQ UI:    http://localhost:15672 (amuse / amuse_dev_secret)"
echo "  Mailpit:        http://localhost:8025  (SMTP localhost:1025)"
echo
echo "Follow logs:  $COMPOSE logs -f amuse.api amuse.worker.transcoder"
echo "Stop stack:   $COMPOSE down"
