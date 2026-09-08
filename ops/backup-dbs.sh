#!/usr/bin/env bash
# Backup of the microservices' SQLite databases + Catalog's uploads folder — install via cron on
# the VPS (replaces server/ops/backup-db.sh, which only knew about the monolith's single DB).
# Uses `sqlite3 .backup` (not a plain file copy) for each database so a backup taken while a
# service is writing is still consistent.
set -euo pipefail

DATA_ROOT="/var/lib/docker/volumes"
BACKUP_DIR="/var/backups/atelie-bebe-microservices"
KEEP_COUNT=10

mkdir -p "$BACKUP_DIR"
TIMESTAMP="$(date +%Y-%m-%d_%H%M%S)"

for svc in identity catalog orders backoffice; do
  DB_PATH="$DATA_ROOT/microservices_${svc}-data/_data/${svc}.db"
  DEST="$BACKUP_DIR/${svc}_${TIMESTAMP}.db"
  sqlite3 "$DB_PATH" ".backup '$DEST'"
  gzip "$DEST"
done

# Catalog's uploaded images live in the same named volume as catalog.db.
tar czf "$BACKUP_DIR/uploads_${TIMESTAMP}.tar.gz" -C "$DATA_ROOT/microservices_catalog-data/_data" uploads

# Keep only the KEEP_COUNT most recent backups per file type.
for prefix in identity catalog orders backoffice uploads; do
  ls -1t "$BACKUP_DIR/${prefix}_"*.gz 2>/dev/null | tail -n "+$((KEEP_COUNT + 1))" | xargs -r rm --
done

echo "Backup salvo em ${BACKUP_DIR} (timestamp ${TIMESTAMP})"
