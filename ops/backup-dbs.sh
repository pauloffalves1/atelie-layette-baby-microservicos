#!/usr/bin/env bash
# Backup of the microservices' SQL Server databases + Catalog's uploads folder — install via cron
# on the VPS. Uses SQL Server's own BACKUP DATABASE (native, consistent even while a service is
# actively writing) instead of a file-level copy, so it works correctly now that all 4 services use
# a shared SQL Server instance instead of local SQLite files (see the 2026-09 migration commit).
set -euo pipefail

BACKUP_DIR="/var/backups/atelie-bebe-microservices"
KEEP_COUNT=10
CONTAINER="microservices-sqlserver-1"
ENV_FILE="/var/www/atelie-bebe-microservices/microservices/.env"
CONTAINER_BACKUP_DIR="/backup"

SA_PASSWORD="$(grep -m1 '^MSSQL_SA_PASSWORD=' "$ENV_FILE" | cut -d= -f2-)"

mkdir -p "$BACKUP_DIR"
TIMESTAMP="$(date +%Y-%m-%d_%H%M%S)"

for db in IdentityDb CatalogDb OrdersDb BackofficeDb; do
  BAK_NAME="${db}_${TIMESTAMP}.bak"
  docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q \
    "BACKUP DATABASE [$db] TO DISK = N'${CONTAINER_BACKUP_DIR}/${BAK_NAME}' WITH COMPRESSION, STATS = 100" \
    > /dev/null
  docker cp "$CONTAINER:${CONTAINER_BACKUP_DIR}/${BAK_NAME}" "$BACKUP_DIR/${BAK_NAME}"
  docker exec "$CONTAINER" rm -f "${CONTAINER_BACKUP_DIR}/${BAK_NAME}"
done

# Catalog's uploaded images live in the same named volume the old catalog.db used to — unrelated to
# the SQL Server migration, LocalFileStorageService still writes there.
tar czf "$BACKUP_DIR/uploads_${TIMESTAMP}.tar.gz" -C /var/lib/docker/volumes/microservices_catalog-data/_data uploads

# Keep only the KEEP_COUNT most recent backups per file type.
for db in IdentityDb CatalogDb OrdersDb BackofficeDb; do
  ls -1t "$BACKUP_DIR/${db}_"*.bak 2>/dev/null | tail -n "+$((KEEP_COUNT + 1))" | xargs -r rm --
done
ls -1t "$BACKUP_DIR/uploads_"*.tar.gz 2>/dev/null | tail -n "+$((KEEP_COUNT + 1))" | xargs -r rm --

echo "Backup salvo em ${BACKUP_DIR} (timestamp ${TIMESTAMP})"
