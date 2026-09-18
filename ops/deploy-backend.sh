#!/usr/bin/env bash
#
# Deploy dos serviços de backend sem derrubar o Gateway.
#
# Uso (no VPS, dentro do diretório do repositório):
#   ops/deploy-backend.sh orders catalog
#
# Por que este script existe: o procedimento anterior terminava com
# "docker compose restart gateway", porque o Docker dá um IP novo ao container recriado e o YARP
# seguia tentando as conexões antigas, devolvendo 502. O restart resolvia — e derrubava junto as
# requisições em voo: em 2026-09-18 o Nginx registrou "Connection reset by peer" para uma cliente
# navegando em /loja no exato segundo de um deploy. O Gateway agora recicla as conexões do pool a
# cada 30s (PooledConnectionLifetime, em Program.cs), então ele se recupera sozinho e ninguém
# precisa ser reiniciado na frente do tráfego. Este script só espera essa recuperação acontecer.
#
# `--no-deps` é proposital: um healthcheck lento do rabbitmq/sqlserver não pode impedir a subida
# dos serviços que estão sendo atualizados.
set -euo pipefail

cd "$(dirname "$0")/.."

if [ "$#" -eq 0 ]; then
  echo "uso: ops/deploy-backend.sh <serviço>... (ex.: orders catalog identity)" >&2
  exit 1
fi

GATEWAY_URL="${GATEWAY_URL:-http://127.0.0.1:5100}"
PROBE_PATH="${PROBE_PATH:-/api/products?page=1&pageSize=1}"
# Folga sobre os 30s de PooledConnectionLifetime, mais o tempo de a aplicação subir e migrar o banco.
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-120}"

echo "==> Construindo: $*"
docker compose build "$@"

echo "==> Recriando (sem tocar no gateway): $*"
docker compose up -d --no-deps "$@"

echo "==> Esperando o gateway voltar a responder em $PROBE_PATH"
deadline=$(( $(date +%s) + TIMEOUT_SECONDS ))
until [ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$GATEWAY_URL$PROBE_PATH" || true)" = "200" ]; do
  if [ "$(date +%s)" -ge "$deadline" ]; then
    echo "!! O gateway não voltou a responder 200 em ${TIMEOUT_SECONDS}s." >&2
    echo "   Verifique 'docker compose logs --tail 50 $*' antes de reiniciar qualquer coisa." >&2
    exit 1
  fi
  sleep 3
done

echo "==> Serviços no ar:"
docker compose ps --format 'table {{.Name}}\t{{.Status}}'
echo "==> Deploy concluído sem reiniciar o gateway."
