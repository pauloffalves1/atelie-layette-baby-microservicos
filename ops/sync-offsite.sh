#!/usr/bin/env bash
# Off-site sync of the microservices' SQLite backups + uploads archive to the same rclone remote
# already configured for the monolith's backups (see server/ops/sync-offsite.sh) — just a
# different destination folder.
set -euo pipefail

BACKUP_DIR="/var/backups/atelie-bebe-microservices"
REMOTE="gdrive:atelie-bebe-microservices-backups"

rclone sync "$BACKUP_DIR" "$REMOTE" --min-age 30s
