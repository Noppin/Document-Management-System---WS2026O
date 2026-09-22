#!/usr/bin/env bash
set -euo pipefail

# Upload a PDF document to the Document Management System REST API.
# Usage:   ./scripts/upload-document.sh [path-to-pdf] [base-url]
# Example: ./scripts/upload-document.sh http/sample.pdf http://localhost:8080

FILE="${1:-http/sample.pdf}"
BASE_URL="${2:-http://localhost:8080}"

if [[ ! -f "$FILE" ]]; then
  echo "File not found: $FILE" >&2
  exit 1
fi

curl --fail-with-body \
  --request POST \
  --form "file=@${FILE};type=application/pdf" \
  "${BASE_URL}/documents"
echo
