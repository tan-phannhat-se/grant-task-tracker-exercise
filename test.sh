#!/usr/bin/env bash
# Runs every check in the repository. Exits non-zero if anything fails.
set -euo pipefail
cd "$(dirname "$0")"

echo "== Backend format =="
dotnet format backend/GrantTracker.sln --verify-no-changes   # style from .editorconfig

echo "== Backend line length (max 120) =="
long_lines=$(grep -rnE --include='*.cs' --exclude-dir=bin --exclude-dir=obj '^.{121,}$' backend || true)
if [ -n "$long_lines" ]; then echo "$long_lines"; exit 1; fi

echo "== Backend tests =="
dotnet test backend --nologo

echo "== Frontend format =="
cd frontend
if [ ! -d node_modules ]; then npm ci --no-fund --no-audit; fi
npm run format:check   # Prettier

echo "== Frontend tests =="
npx tsc -b   # type-check: Vitest runs TypeScript without checking types
npm test
