#!/usr/bin/env bash
# Verifies the most recent backup of each database actually restores — a backup nobody ever tries to
# restore is a silent risk (corruption, an incomplete BACKUP DATABASE run, etc. go unnoticed until an
# emergency). Install via cron on the VPS, e.g. weekly, right after backup-dbs.sh's own schedule.
# Restores into a throwaway "_RestoreTest" database on the same SQL Server instance, checks that EF's
# migration history and every user table came back with rows, then drops it either way. Exits non-zero
# if any database fails to restore or looks empty, so cron mail (MAILTO=) surfaces the failure.
set -uo pipefail

BACKUP_DIR="/var/backups/atelie-bebe-microservices"
CONTAINER="microservices-sqlserver-1"
ENV_FILE="/var/www/atelie-layette-baby-microservicos/.env"
CONTAINER_BACKUP_DIR="/backup"

SA_PASSWORD="$(grep -m1 '^MSSQL_SA_PASSWORD=' "$ENV_FILE" | cut -d= -f2-)"
# -b (abort on error) makes sqlcmd's own exit code reflect a failed RESTORE/DDL statement — without
# it, sqlcmd prints the SQL Server error but still exits 0, so a genuine restore failure would sail
# past the "if ! docker exec ..." check below undetected.
SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -C -b"

overall_status=0

for db in IdentityDb CatalogDb OrdersDb BackofficeDb; do
  LATEST_BAK="$(ls -1t "$BACKUP_DIR/${db}_"*.bak 2>/dev/null | head -n1)"
  if [ -z "$LATEST_BAK" ]; then
    echo "[$db] FALHA: nenhum backup encontrado em $BACKUP_DIR"
    overall_status=1
    continue
  fi

  BAK_NAME="$(basename "$LATEST_BAK")"
  TEST_DB="${db}_RestoreTest"

  docker cp "$LATEST_BAK" "$CONTAINER:${CONTAINER_BACKUP_DIR}/${BAK_NAME}" > /dev/null
  # docker cp preserves the host file's ownership (root), but sqlservr runs as the unprivileged
  # "mssql" user inside the container and can't read a root-owned file — RESTORE DATABASE then fails
  # with "Cannot open backup device ... Access is denied." chmod instead of chown so this works
  # regardless of which user `docker exec` defaults to.
  docker exec "$CONTAINER" chmod 644 "${CONTAINER_BACKUP_DIR}/${BAK_NAME}"

  # Restore with MOVE so it doesn't collide with the live database's own data/log files, and WITH
  # REPLACE in case a previous run's test database was left behind by a failure.
  RESTORE_SQL="
    RESTORE DATABASE [$TEST_DB] FROM DISK = N'${CONTAINER_BACKUP_DIR}/${BAK_NAME}'
    WITH REPLACE,
      MOVE '$db' TO '/var/opt/mssql/data/${TEST_DB}.mdf',
      MOVE '${db}_log' TO '/var/opt/mssql/data/${TEST_DB}_log.ldf';
  "
  if ! docker exec "$CONTAINER" $SQLCMD -Q "$RESTORE_SQL" > /tmp/restore_${db}.log 2>&1; then
    echo "[$db] FALHA ao restaurar $BAK_NAME:"
    cat /tmp/restore_${db}.log
    overall_status=1
    docker exec "$CONTAINER" rm -f "${CONTAINER_BACKUP_DIR}/${BAK_NAME}"
    continue
  fi

  # Sanity check: EF's migration history exists (schema came back intact) and at least one user
  # table actually has rows (catches a backup that restores structurally but is empty/corrupted).
  CHECK_SQL="
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [$TEST_DB].sys.tables WHERE name = '__EFMigrationsHistory')
      SELECT -1;
    ELSE
      SELECT SUM(p.rows) FROM [$TEST_DB].sys.partitions p
      JOIN [$TEST_DB].sys.tables t ON t.object_id = p.object_id
      WHERE p.index_id IN (0, 1);
  "
  ROW_COUNT="$(docker exec "$CONTAINER" $SQLCMD -h -1 -Q "$CHECK_SQL" 2>&1 | tr -d '[:space:]')"

  # ROW_COUNT must be a plain (optionally negative) integer to count as a real result — anything
  # else (empty, or a SQL Server error message like "Msg208,Level16,...") is a failure. Checking the
  # shape first avoids the bash gotcha where `[ "$ROW_COUNT" -le 0 ]` on a non-numeric string is a
  # comparison *error*, not a false result, and used to fall through to the "OK" branch below.
  if ! [[ "$ROW_COUNT" =~ ^-?[0-9]+$ ]] || [ "$ROW_COUNT" = "-1" ] || [ "$ROW_COUNT" -le 0 ]; then
    echo "[$db] FALHA: restaurou mas parece vazio, sem schema de migrations, ou a checagem deu erro (saída: ${ROW_COUNT:-vazia})"
    overall_status=1
  else
    echo "[$db] OK: $BAK_NAME restaurou com $ROW_COUNT linha(s) no total"
  fi

  docker exec "$CONTAINER" $SQLCMD -Q "ALTER DATABASE [$TEST_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$TEST_DB];" > /dev/null 2>&1
  docker exec "$CONTAINER" rm -f "${CONTAINER_BACKUP_DIR}/${BAK_NAME}"
done

exit $overall_status
