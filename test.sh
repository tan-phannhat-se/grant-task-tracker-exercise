#!/usr/bin/env bash
# Runs every test in the repository. Exits non-zero if anything fails.
set -euo pipefail
cd "$(dirname "$0")"

echo "== Backend tests =="
dotnet test backend --nologo

echo "== Frontend tests =="
cd frontend
if [ ! -d node_modules ]; then npm ci --no-fund --no-audit; fi
npx tsc -b   # type-check: Vitest runs TypeScript without checking types
npm test
