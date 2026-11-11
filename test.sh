#!/usr/bin/env bash
# Runs every check in the repository. Exits non-zero if anything fails.
set -euo pipefail
cd "$(dirname "$0")"

echo "== Backend format =="
dotnet format backend/GrantTracker.sln --verify-no-changes   # style from .editorconfig (D19)

echo "== Backend tests =="
dotnet test backend --nologo

echo "== Frontend format =="
cd frontend
if [ ! -d node_modules ]; then npm ci --no-fund --no-audit; fi
npm run format:check   # Prettier (D19)

echo "== Frontend tests =="
npx tsc -b   # type-check: Vitest runs TypeScript without checking types
npm test
