# Dockerfile Arm64 Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the Dockerfile platform-aware and optimized for multi-arch builds (linux/amd64 + linux/arm64) while keeping runtime behavior unchanged.

**Architecture:** Refactor the multi-stage Dockerfile to use `--platform=$BUILDPLATFORM` for build and `--platform=$TARGETPLATFORM` for runtime. Add BuildKit cache mounts for NuGet restores/publish, and publish for `linux-$TARGETARCH` with framework-dependent output so the ASP.NET runtime image remains valid.

**Tech Stack:** .NET 10, Docker (BuildKit/buildx).

**Skills:** Use @test-driven-development for the verification script, and @verification-before-completion before claiming success.

---

### Task 1: Make Dockerfile Platform-Aware + Cache NuGet

**Files:**
- Create: `scripts/dockerfile-multiarch-check.ps1`
- Modify: `Dockerfile`
- Test: `scripts/dockerfile-multiarch-check.ps1`

**Step 1: Write the failing test**

Create `scripts/dockerfile-multiarch-check.ps1`:

```powershell
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
```

**Step 2: Run test to verify it fails**

Run: `powershell -File scripts/dockerfile-multiarch-check.ps1`
Expected: FAIL with "Missing Dockerfile lines" because the Dockerfile isn?t platform-aware yet.

**Step 3: Write minimal implementation**

Update `Dockerfile` to:

```dockerfile
# syntax=docker/dockerfile:1.5

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG TARGETOS
ARG TARGETARCH
ARG TARGETPLATFORM
ARG BUILDPLATFORM
ARG NUGET_PACKAGES=/root/.nuget/packages

COPY ConfigVault.sln ./
COPY src/ConfigVault.Api/ConfigVault.Api.csproj src/ConfigVault.Api/
COPY src/ConfigVault.Core/ConfigVault.Core.csproj src/ConfigVault.Core/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/ConfigVault.Api/ConfigVault.Api.csproj

COPY src/ src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/ConfigVault.Api/ConfigVault.Api.csproj \
      -c Release \
      -r linux-$TARGETARCH \
      --self-contained false \
      -o /app/publish \
      /p:UseAppHost=false

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ConfigVault.Api.dll"]
```

**Step 4: Run test to verify it passes**

Run: `powershell -File scripts/dockerfile-multiarch-check.ps1`
Expected: PASS (no output, exit 0).

**Step 5: Commit**

```bash
git add scripts/dockerfile-multiarch-check.ps1 Dockerfile
git commit -m "feat: make Dockerfile multi-arch aware"
```
