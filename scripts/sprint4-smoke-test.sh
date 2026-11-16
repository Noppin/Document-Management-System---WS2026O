#!/usr/bin/env bash
# Sprint-4 MUST-HAVE smoke test:
# HelloWorld.pdf -> MinIO -> OCR -> RabbitMQ indexing queue -> Elasticsearch -> search API.
set -uo pipefail

cd "$(dirname "$0")/.."

API_URL="${API_URL:-http://localhost/api}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8180}"
TOKEN_URL="$KEYCLOAK_URL/realms/dms/protocol/openid-connect/token"
PDF="${PDF:-http/HelloWorld.pdf}"
WAIT_SECONDS="${WAIT_SECONDS:-120}"

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

if [ ! -f "$PDF" ]; then
  bad "HelloWorld PDF not found: $PDF"
  summary
fi
ok "HelloWorld PDF found"

running="$(docker compose ps --status running --services 2>/dev/null)"
for service in postgres minio rabbitmq elasticsearch api ocr-worker indexing-worker frontend keycloak; do
  if printf '%s\n' "$running" | grep -qx "$service"; then
    ok "$service is running"
  else
    bad "$service is not running"
  fi
done

TOKEN="$(curl -s --max-time 15 \
  --request POST \
  --data-urlencode "grant_type=password" \
  --data-urlencode "client_id=frontend" \
  --data-urlencode "username=alice" \
  --data-urlencode "password=password" \
  "$TOKEN_URL" 2>/dev/null | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"

if [ -z "$TOKEN" ]; then
  bad "could not obtain Keycloak access token"
  summary
fi
ok "obtained access token"

response="$(curl -s --max-time 60 --fail-with-body \
  --request POST \
  --header "Authorization: Bearer $TOKEN" \
  --form "file=@${PDF};type=application/pdf" \
  "$API_URL/documents" 2>/dev/null)"

DOCUMENT_ID="$(printf '%s' "$response" | sed -n 's/.*"id":"\([0-9a-f-]\{36\}\)".*/\1/p')"

if [ -z "$DOCUMENT_ID" ]; then
  bad "upload did not return a document id: $response"
  summary
fi
ok "uploaded HelloWorld.pdf as $DOCUMENT_ID"

waited=0
SEARCH_RESPONSE="[]"
while [ "$waited" -lt "$WAIT_SECONDS" ]; do
  SEARCH_RESPONSE="$(curl -s --max-time 15 \
    --get \
    --header "Authorization: Bearer $TOKEN" \
    --data-urlencode "query=Hello" \
    "$API_URL/search" 2>/dev/null)"

  # Match the id of THIS upload, so HelloWorld.pdf copies from earlier runs cannot make the test pass.
  if printf '%s' "$SEARCH_RESPONSE" | grep -Fq "{\"id\":\"$DOCUMENT_ID\",\"fileName\":\"HelloWorld.pdf\"}"; then
    ok "search for Hello returned the uploaded HelloWorld.pdf ($DOCUMENT_ID)"
    break
  fi

  sleep 3
  waited=$((waited + 3))
done

if [ "$waited" -ge "$WAIT_SECONDS" ]; then
  bad "search did not return the uploaded HelloWorld.pdf ($DOCUMENT_ID) within ${WAIT_SECONDS}s: $SEARCH_RESPONSE"
fi

if docker compose logs ocr-worker --tail 500 2>/dev/null | grep -F "$DOCUMENT_ID" | grep -Fq "OCR finished"; then
  ok "OCR worker processed the uploaded document"
else
  bad "OCR worker log did not contain the uploaded document"
fi

if docker compose logs indexing-worker --tail 500 2>/dev/null | grep -Fq "$DOCUMENT_ID"; then
  ok "indexing worker processed the OCR result"
else
  bad "indexing worker log did not contain the uploaded document"
fi

echo
echo "Manual UI MUST-HAVE:"
echo "  1. Open http://localhost/"
echo "  2. Upload http/HelloWorld.pdf"
echo "  3. Search for: Hello"
echo "  4. HelloWorld.pdf must be shown"

summary
