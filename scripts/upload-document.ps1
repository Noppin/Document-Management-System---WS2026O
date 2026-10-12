<#
.SYNOPSIS
    Uploads a PDF document to the Document Management System REST API.
.EXAMPLE
    ./scripts/upload-document.ps1 -File http/sample.pdf -BaseUrl http://localhost/api
#>
param(
    [string]$File = "http/sample.pdf",
    [string]$BaseUrl = "http://localhost/api",
    [string]$TokenUrl = "http://localhost:8180/realms/dms/protocol/openid-connect/token"
)

if (-not (Test-Path -Path $File)) {
    Write-Error "File not found: $File"
    exit 1
}

$token = (curl.exe -s --request POST `
    --data-urlencode "grant_type=password" `
    --data-urlencode "client_id=frontend" `
    --data-urlencode "username=alice" `
    --data-urlencode "password=password" `
    $TokenUrl | ConvertFrom-Json).access_token

if (-not $token) {
    Write-Error "Could not obtain an access token from $TokenUrl"
    exit 1
}

curl.exe --fail-with-body --request POST --header "Authorization: Bearer $token" --form "file=@$File;type=application/pdf" "$BaseUrl/documents"
