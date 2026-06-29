#!/bin/sh
set -eu

runtime_env_file="/tmp/bommy-runtime-env.yml"

yaml_escape() {
  printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

write_env() {
  key="$1"
  value="$2"
  printf '  - "%s=%s"\n' "$key" "$(yaml_escape "$value")" >> "$runtime_env_file"
}

cat > "$runtime_env_file" <<'YAML'
runtime:
  env:
YAML

write_env "MATCH_SERVER_PROVIDER" "${MATCH_SERVER_PROVIDER:-EdgegapCloud}"
write_env "BOMMY_LOCALDEV_ORCHESTRATION" "${BOMMY_LOCALDEV_ORCHESTRATION:-0}"
write_env "BOMMY_RANDOM_QUEUE_REQUIRED_PLAYERS" "${BOMMY_RANDOM_QUEUE_REQUIRED_PLAYERS:-4}"
write_env "BOMMY_SERVER_SECRET" "${BOMMY_SERVER_SECRET:-dev-local-secret}"
write_env "BOMMY_NAKAMA_HOST" "${BOMMY_NAKAMA_HOST:-127.0.0.1}"
write_env "BOMMY_NAKAMA_SCHEME" "${BOMMY_NAKAMA_SCHEME:-http}"
write_env "BOMMY_PUBLIC_NAKAMA_SCHEME" "${BOMMY_PUBLIC_NAKAMA_SCHEME:-}"
write_env "BOMMY_NAKAMA_PORT" "${BOMMY_NAKAMA_PORT:-7350}"
write_env "BOMMY_NAKAMA_SERVER_KEY" "${BOMMY_NAKAMA_SERVER_KEY:-defaultkey}"
write_env "BOMMY_NAKAMA_HTTP_KEY" "${BOMMY_NAKAMA_HTTP_KEY:-defaulthttpkey}"
write_env "BOMMY_PUBLIC_NAKAMA_HOST" "${BOMMY_PUBLIC_NAKAMA_HOST:-}"
write_env "EDGEGAP_ENABLED" "${EDGEGAP_ENABLED:-0}"
write_env "EDGEGAP_API_TOKEN" "${EDGEGAP_API_TOKEN:-}"
write_env "EDGEGAP_APP_NAME" "${EDGEGAP_APP_NAME:-bommy-server}"
write_env "EDGEGAP_VERSION_NAME" "${EDGEGAP_VERSION_NAME:-}"
write_env "EDGEGAP_DEFAULT_REGION" "${EDGEGAP_DEFAULT_REGION:-Local}"
write_env "EDGEGAP_INTERNAL_GAME_PORT" "${EDGEGAP_INTERNAL_GAME_PORT:-5000}"
write_env "EDGEGAP_PROTOCOL" "${EDGEGAP_PROTOCOL:-UDP}"
write_env "EDGEGAP_PORT_NAME" "${EDGEGAP_PORT_NAME:-gameport}"
cat >> "$runtime_env_file" <<YAML

social:
  steam:
    app_id: ${NAKAMA_SOCIAL_STEAM_APP_ID:-${SOCIAL_STEAM_APP_ID:-0}}
    publisher_key: "${NAKAMA_SOCIAL_STEAM_PUBLISHER_KEY:-${SOCIAL_STEAM_PUBLISHER_KEY:-}}"
YAML

exec /nakama/nakama --config /nakama/data/local.yml --config "$runtime_env_file" "$@"
