$ErrorActionPreference = "Stop"

$dockerfile = Get-Content Dockerfile -Raw
$required = @(
  'FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build',
  'ARG TARGETARCH',
  'ARG TARGETPLATFORM',
  'FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0',
  '--mount=type=cache'
)

$missing = $required | Where-Object { $dockerfile -notmatch [regex]::Escape($_) }
if ($missing.Count -gt 0) {
  Write-Error "Missing Dockerfile lines:`n$($missing -join "`n")"
}
