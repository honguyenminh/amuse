#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API="$ROOT/src/Amuse.Api"
MODULES="$ROOT/src/Amuse.Modules"

export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5432;Database=amuse_development;Username=postgres;Password=postgres}"

contexts=(
  IdentityDbContext
  TenancyDbContext
  ListenerDbContext
  PlatformDbContext
  AuditDbContext
)

for context in "${contexts[@]}"; do
  echo "Applying migrations for ${context}..."
  dotnet ef database update \
    --project "$MODULES" \
    --startup-project "$API" \
    --context "$context"
done

echo "All bounded-context migrations applied."
