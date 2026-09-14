#!/bin/sh
# Regenerates the crawler snapshots (see README.md). Cron: every 6 hours, and after each frontend
# deploy. Runs Chromium inside Microsoft's Playwright image so the host needs no browser libraries.
set -eu

REPO=/var/www/atelie-layette-baby-microservicos
DATA=/var/www/atelie-bebe-microservices
VERSION=$(node -p "require('$REPO/frontend/shell/node_modules/playwright/package.json').version")

exec docker run --rm --network host --ipc=host \
  -v "$REPO/frontend/shell:/work:ro" \
  -v "$DATA:/data" \
  -e OUT_DIR=/data/__prerender \
  -w /work \
  "mcr.microsoft.com/playwright:v$VERSION-noble" \
  node scripts/prerender.mjs
