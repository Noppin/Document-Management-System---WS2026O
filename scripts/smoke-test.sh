#!/usr/bin/env bash
# Sprint-2 smoke test: React frontend (nginx), ASP.NET Core REST API and
# PostgreSQL running together in Docker Compose.
set -uo pipefail

cd "$(dirname "$0")/.."

FRONTEND_URL="${FRONTEND_URL:-http://localhost}"
API_URL="${API_URL:-http://localhost:8080}"

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

expect_status() {
	local label="$1" url="$2" expected="$3" actual
	actual="$(http_status "$url")"
	if [ "$actual" = "$expected" ]; then
		ok "$label -> $actual"
	else
		bad "$label -> got $actual, want $expected"
	fi
}

echo "Compose services"
running="$(docker compose ps --status running --services 2>/dev/null)"
for service in postgres api frontend; do
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
echo "nginx proxies the REST API"
expect_status "GET /api/collections" "$FRONTEND_URL/api/collections" 200

echo
echo "REST API"
expect_status "GET /health" "$API_URL/health" 200
expect_status "GET /collections" "$API_URL/collections" 200
expect_status "GET /documents" "$API_URL/documents" 200

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
