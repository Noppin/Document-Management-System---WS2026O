#!/usr/bin/env bash
# Sprint-2 smoke test: React frontend (nginx), ASP.NET Core REST API,
# PostgreSQL and Keycloak running together in Docker Compose.
set -uo pipefail

cd "$(dirname "$0")/.."

FRONTEND_URL="${FRONTEND_URL:-http://localhost}"
API_URL="${API_URL:-$FRONTEND_URL/api}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8180}"
TOKEN_URL="$KEYCLOAK_URL/realms/dms/protocol/openid-connect/token"

PASS=0
FAIL=0

ok() {
	printf '  ok    %s\n' "$1"
	PASS=$((PASS + 1))
}

bad() {
	printf '  FAIL  %s\n' "$1"
	FAIL=$((FAIL + 1))
}

http_status() {
	curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$1" 2>/dev/null
}

auth_status() {
	curl -s -o /dev/null -w '%{http_code}' --max-time 10 \
		-H "Authorization: Bearer $2" "$1" 2>/dev/null
}

expect_status() {
	local label="$1" url="$2" expected="$3" actual
	actual="$(http_status "$url")"
	if [ "$actual" = "$expected" ]; then
		ok "$label -> $actual"
	else
		bad "$label -> got $actual, want $expected"
	fi
}

expect_auth_status() {
	local label="$1" url="$2" expected="$3" actual
	actual="$(auth_status "$url" "$TOKEN")"
	if [ "$actual" = "$expected" ]; then
		ok "$label -> $actual"
	else
		bad "$label -> got $actual, want $expected"
	fi
}

fetch_token() {
	curl -s --max-time 10 \
		--request POST \
		--data-urlencode "grant_type=password" \
		--data-urlencode "client_id=frontend" \
		--data-urlencode "username=alice" \
		--data-urlencode "password=password" \
		"$TOKEN_URL" 2>/dev/null |
		sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p'
}

echo "Compose services"
running="$(docker compose ps --status running --services 2>/dev/null)"
for service in postgres rabbitmq api ocr-worker frontend keycloak; do
	if printf '%s\n' "$running" | grep -qx "$service"; then
		ok "$service is running"
	else
		bad "$service is not running"
	fi
done

echo
echo "Frontend served by nginx"
expect_status "GET /" "$FRONTEND_URL/" 200

asset="$(curl -s --max-time 10 "$FRONTEND_URL/" 2>/dev/null | grep -o '/assets/[^"]*\.js' | head -n 1)"
if [ -n "$asset" ]; then
	expect_status "GET $asset" "$FRONTEND_URL$asset" 200
else
	bad "no bundled JS asset found in index.html"
fi

echo
echo "SPA routes survive a browser refresh"
expect_status "GET /documents/:id" "$FRONTEND_URL/documents/1" 200
expect_status "GET /collections/:id" "$FRONTEND_URL/collections/1" 200
expect_status "GET /upload" "$FRONTEND_URL/upload" 200
expect_status "GET unknown route" "$FRONTEND_URL/definitely-not-a-route" 200

echo
echo "Keycloak"
expect_status "GET /realms/dms" "$KEYCLOAK_URL/realms/dms" 200

echo
echo "nginx proxies the REST API"
expect_status "GET /api/health (public)" "$API_URL/health" 200
expect_status "GET /api/documents without token" "$API_URL/documents" 401
expect_status "GET /api/collections without token" "$API_URL/collections" 401

echo
echo "REST API (authenticated through nginx)"
TOKEN="$(fetch_token)"
if [ -n "$TOKEN" ]; then
	ok "obtained access token for alice"
	expect_auth_status "GET /api/documents with token" "$API_URL/documents" 200
	expect_auth_status "GET /api/collections with token" "$API_URL/collections" 200
else
	bad "could not obtain an access token from $TOKEN_URL"
fi

echo
echo "Container networking"
if docker compose exec -T frontend wget -qO- --timeout=10 http://api:8080/health 2>/dev/null | grep -q '"status"'; then
	ok "frontend container reaches http://api:8080/health"
else
	bad "frontend container cannot reach the API container"
fi

echo
echo "PostgreSQL"
if docker compose exec -T postgres pg_isready -U postgres -d document_management >/dev/null 2>&1; then
	ok "PostgreSQL accepts connections"
else
	bad "PostgreSQL is not accepting connections"
fi

echo
echo "$PASS passed, $FAIL failed"
if [ "$FAIL" -gt 0 ]; then
	exit 1
fi
