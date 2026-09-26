#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UPSTREAM="$ROOT/artifacts/laya-upstream"
TAG=v0.3.20
EXPECTED_COMMIT_PREFIX=23a1752

export COMPOSE_PROJECT_NAME=typeddecisions-laya
export LAYA_CACHE_VOLUME=typeddecisions-laya-model-cache
export LAYA_BIND_ADDRESS=127.0.0.1
export LAYA_PORT=8000
export LAYA_DEVICE=cpu
export LAYA_TORCH_INDEX=cpu
export LAYA_PRELOAD=1
export LAYA_MODELS=multilingual
export LAYA_THREADS=4
export OMP_NUM_THREADS=4
export HF_HUB_OFFLINE="${HF_HUB_OFFLINE:-0}"

usage() {
  echo "Uso: $0 setup|up|down|status|smoke" >&2
  exit 2
}

ensure_checkout() {
  if [[ ! -d "$UPSTREAM/.git" ]]; then
    mkdir -p "$ROOT/artifacts"
    git clone --depth 1 --branch "$TAG" --single-branch https://github.com/NandhaKishorM/laya.git "$UPSTREAM"
  fi
  local commit
  commit="$(git -C "$UPSTREAM" rev-parse HEAD)"
  if [[ "$commit" != "$EXPECTED_COMMIT_PREFIX"* ]]; then
    echo "El checkout de Laya no coincide con $TAG ($EXPECTED_COMMIT_PREFIX): $commit" >&2
    exit 1
  fi
  printf '%s\n' "$commit" > "$ROOT/artifacts/laya-upstream-commit.txt"
}

ensure_docker() {
  if docker info >/dev/null 2>&1; then return; fi
  if [[ "$(uname)" == Darwin && -d /Applications/Docker.app ]]; then
    open /Applications/Docker.app
  fi
  for _ in $(seq 1 60); do
    if docker info >/dev/null 2>&1; then return; fi
    sleep 2
  done
  echo "Docker Desktop no está listo. Inícialo y repite el comando." >&2
  exit 1
}

compose() {
  (cd "$UPSTREAM" && docker compose -f compose.yaml -f compose.http.yaml "$@")
}

health() {
  curl --fail --silent --show-error "http://127.0.0.1:$LAYA_PORT/health"
  echo
}

case "${1:-}" in
  setup)
    ensure_docker
    ensure_checkout
    compose up --build -d --wait laya-serve
    health
    ;;
  up)
    ensure_docker
    ensure_checkout
    compose up -d --wait laya-serve
    health
    ;;
  down)
    ensure_checkout
    compose down
    echo "Los pesos permanecen en el volumen $LAYA_CACHE_VOLUME."
    ;;
  status)
    ensure_checkout
    compose ps
    health
    ;;
  smoke)
    ensure_docker
    ensure_checkout
    compose up -d --wait laya-serve
    health
    curl --fail --silent --show-error "http://127.0.0.1:$LAYA_PORT/v1/systemone" \
      -H 'content-type: application/json' \
      --data-binary @"$ROOT/scripts/laya-smoke.json" \
      | python3 -c 'import json,sys; r=json.load(sys.stdin); assert r["routing"]["model"] == "multilingual", r; a=r["answers"]; assert a["route"]["type"] == "choice" and a["risk"]["type"] == "noul" and a["impact"]["type"] == "score", r; assert "usage" in r; print("Laya HTTP OK:", r["model"], r["routing"]["model"])'
    dotnet run --project "$ROOT/samples/TypedDecisions.Console" -- support --laya --no-color
    export HF_HUB_OFFLINE=1
    compose up -d --force-recreate --wait laya-serve
    health
    curl --fail --silent --show-error "http://127.0.0.1:$LAYA_PORT/v1/systemone" \
      -H 'content-type: application/json' \
      --data-binary @"$ROOT/scripts/laya-smoke.json" \
      | python3 -c 'import json,sys; r=json.load(sys.stdin); assert r["routing"]["model"] == "multilingual"; print("Laya sin red OK:", r["model"])'
    ;;
  *) usage ;;
esac
