#!/usr/bin/env bash
set -euo pipefail

# Upload a PDF document to the Document Management System REST API.
# Usage:   ./scripts/upload-document.sh [path-to-pdf] [base-url]
# Example: ./scripts/upload-document.sh http/sample.pdf http://localhost/api

FILE="${1:-http/sample.pdf}"
BASE_URL="${2:-http://localhost/api}"
TOKEN_URL="${TOKEN_URL:-http://localhost:8180/realms/dms/protocol/openid-connect/token}"

if [[ ! -f "$FILE" ]]; then
  echo "File not found: $FILE" >&2
  exit 1
fi

TOKEN="$(curl -s \
  --request POST \
  --data-urlencode "grant_type=password" \
  --data-urlencode "client_id=frontend" \
  --data-urlencode "username=alice" \
  --data-urlencode "password=password" \
  "$TOKEN_URL" | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"

if [[ -z "$TOKEN" ]]; then
  echo "Could not obtain an access token from $TOKEN_URL" >&2
  exit 1
fi

curl --fail-with-body \
  --request POST \
  --header "Authorization: Bearer ${TOKEN}" \
  --form "file=@${FILE};type=application/pdf" \
  "${BASE_URL}/documents"
echo
