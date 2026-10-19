#!/usr/bin/env bash
# Sprint-3 OCR smoke test: document upload -> REST API -> RabbitMQ -> OCR worker log.
# No real OCR is performed in Sprint 3; the worker logs the received document.
set -uo pipefail

cd "$(dirname "$0")/.."

API_URL="${API_URL:-http://localhost/api}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8180}"
TOKEN_URL="$KEYCLOAK_URL/realms/dms/protocol/openid-connect/token"
PDF="${PDF:-http/sample.pdf}"
QUEUE_NAME="${QUEUE_NAME:-dms.document.ocr}"
WAIT_SECONDS="${WAIT_SECONDS:-60}"

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

summary() {
	echo
	echo "$PASS passed, $FAIL failed"
	if [ "$FAIL" -gt 0 ]; then
		exit 1
	fi
}

http_status() {
	curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$1" 2>/dev/null
}

compose_logs() {
	docker compose logs "$1" --tail 300 2>/dev/null
}

wait_for_log() {
	local service="$1" needle="$2" label="$3" waited=0
	while [ "$waited" -lt "$WAIT_SECONDS" ]; do
		if compose_logs "$service" | grep -Fq "$needle"; then
			ok "$label"
			return 0
		fi
		sleep 2
		waited=$((waited + 2))
	done
	bad "$label (no log entry within ${WAIT_SECONDS}s)"
	return 1
}

wait_for_document_log() {
	local service="$1" needle="$2" label="$3" waited=0
	while [ "$waited" -lt "$WAIT_SECONDS" ]; do
		if compose_logs "$service" | grep -F "$needle" | grep -Fq "$DOCUMENT_ID"; then
			ok "$label"
			return 0
		fi
		sleep 2
		waited=$((waited + 2))
	done
	bad "$label (no log entry within ${WAIT_SECONDS}s)"
	return 1
}

echo "Prerequisites"
if [ ! -f "$PDF" ]; then
	bad "sample PDF not found: $PDF"
	summary
fi
ok "sample PDF found: $PDF"

running="$(docker compose ps --status running --services 2>/dev/null)"
for service in postgres rabbitmq api ocr-worker keycloak; do
	if printf '%s\n' "$running" | grep -qx "$service"; then
		ok "$service is running"
	else
		bad "$service is not running"
	fi
done

if [ "$(http_status "$API_URL/health")" = "200" ]; then
	ok "GET /api/health -> 200"
else
	bad "GET /api/health did not answer 200"
	summary
fi

TOKEN="$(curl -s --max-time 10 \
	--request POST \
	--data-urlencode "grant_type=password" \
	--data-urlencode "client_id=frontend" \
	--data-urlencode "username=alice" \
	--data-urlencode "password=password" \
	"$TOKEN_URL" 2>/dev/null | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"

if [ -n "$TOKEN" ]; then
	ok "obtained access token for alice"
else
	bad "could not obtain an access token from $TOKEN_URL"
	summary
fi

echo
echo "Upload a PDF through the REST API"
response="$(curl -s --max-time 30 --fail-with-body \
	--request POST \
	--header "Authorization: Bearer $TOKEN" \
	--form "file=@${PDF};type=application/pdf" \
	"$API_URL/documents" 2>/dev/null)"

DOCUMENT_ID="$(printf '%s' "$response" | sed -n 's/.*"id":"\([0-9a-f-]\{36\}\)".*/\1/p')"

if [ -n "$DOCUMENT_ID" ]; then
	ok "upload returned document id $DOCUMENT_ID"
else
	bad "upload did not return a document id: $response"
	summary
fi

echo
echo "REST API published the upload event"
wait_for_document_log api "Persisted uploaded document" "api logged the persisted document"
wait_for_document_log api "Published OCR queue message" "api published the OCR queue message"

echo
echo "OCR worker consumed the message"
wait_for_log ocr-worker "OCR worker is listening" "ocr-worker is listening on the queue"
wait_for_document_log ocr-worker "received document" "ocr-worker received the document"

echo
echo "RabbitMQ queue"
if docker compose exec -T rabbitmq rabbitmqctl list_queues name 2>/dev/null | grep -q "$QUEUE_NAME"; then
	ok "queue $QUEUE_NAME exists"
else
	bad "queue $QUEUE_NAME was not found"
fi

echo
echo "Trace the document end to end:"
echo "  docker compose logs api ocr-worker | grep $DOCUMENT_ID"

summary
