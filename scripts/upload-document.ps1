<#
.SYNOPSIS
    Uploads a PDF document to the Document Management System REST API.
.EXAMPLE
    ./scripts/upload-document.ps1 -File http/sample.pdf -BaseUrl http://localhost:8080
#>
param(
    [string]$File = "http/sample.pdf",
    [string]$BaseUrl = "http://localhost:8080"
)

if (-not (Test-Path -Path $File)) {
    Write-Error "File not found: $File"
    exit 1
}

curl.exe --fail-with-body --request POST --form "file=@$File;type=application/pdf" "$BaseUrl/documents"
