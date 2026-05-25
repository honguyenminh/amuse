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

echo "Migrating: ${ConnectionStrings__DefaultConnection}"
echo "If you see history-table errors on first run, ignore them when followed by 'Done.'"
echo

dotnet build "$API" -v q

for context in "${contexts[@]}"; do
  echo "==> ${context}"
  dotnet ef database update \
    --project "$MODULES" \
    --startup-project "$API" \
    --context "$context" \
    --no-build
  echo "    OK"
  echo
done

echo "All bounded-context migrations applied successfully."
