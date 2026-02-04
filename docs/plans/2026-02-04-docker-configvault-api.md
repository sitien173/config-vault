# Dockerize ConfigVault.Api Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Dockerfile and Docker Compose stack to run `ConfigVault.Api` on Ubuntu with env-based configuration and a healthcheck.

**Architecture:** Use a multi-stage Dockerfile to build and publish the .NET 10 API, then run it in a slim ASP.NET runtime image. Provide a compose file that builds the image, maps host port 8083 to container port 8080, injects `ConfigVault` settings via environment variables, and defines a `/health`-based container healthcheck.

**Tech Stack:** .NET 10, Docker, Docker Compose.

**Skills:** Use @verification-before-completion before declaring the work complete. Use @requesting-code-review if you want a formal review before merge.

---

### Task 1: Add Docker Build Smoke Test + Dockerfile

**Files:**
- Create: `scripts/docker-smoke.ps1`
- Create: `Dockerfile`
- Create: `.dockerignore`
- Test: `scripts/docker-smoke.ps1`

**Step 1: Write the failing test**

Create `scripts/docker-smoke.ps1`:

```powershell
$ErrorActionPreference = "Stop"

docker build -t configvault-api:dev .
```

**Step 2: Run test to verify it fails**

Run: `powershell -File scripts/docker-smoke.ps1`
Expected: FAIL with an error like `failed to read dockerfile` because `Dockerfile` does not exist yet.

**Step 3: Write minimal implementation**

Create `Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ConfigVault.sln ./
COPY src/ConfigVault.Api/ConfigVault.Api.csproj src/ConfigVault.Api/
COPY src/ConfigVault.Core/ConfigVault.Core.csproj src/ConfigVault.Core/
RUN dotnet restore src/ConfigVault.Api/ConfigVault.Api.csproj

COPY src/ src/
RUN dotnet publish src/ConfigVault.Api/ConfigVault.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
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

Create `.dockerignore`:

```gitignore
**/bin/
**/obj/
**/.vs/
**/.idea/
**/.vscode/
**/*.user
**/*.suo
**/*.cache
**/*.log
**/TestResults/
.git
.gitignore
.worktrees/
docs/
sdks/
src/ConfigVault.Tests/
```

**Step 4: Run test to verify it passes**

Run: `powershell -File scripts/docker-smoke.ps1`
Expected: PASS with a successful Docker build (image `configvault-api:dev` created).

**Step 5: Commit**

```bash
git add scripts/docker-smoke.ps1 Dockerfile .dockerignore
git commit -m "feat: add Dockerfile for ConfigVault.Api"
```

### Task 2: Add Docker Compose Stack

**Files:**
- Modify: `scripts/docker-smoke.ps1`
- Create: `docker-compose.yml`
- Test: `scripts/docker-smoke.ps1`

**Step 1: Write the failing test**

Update `scripts/docker-smoke.ps1`:

```powershell
$ErrorActionPreference = "Stop"

docker build -t configvault-api:dev .
docker compose -f docker-compose.yml config
```

**Step 2: Run test to verify it fails**

Run: `powershell -File scripts/docker-smoke.ps1`
Expected: FAIL with an error like `docker-compose.yml not found` because the compose file does not exist yet.

**Step 3: Write minimal implementation**

Create `docker-compose.yml`:

```yaml
services:
  configvault-api:
    build:
      context: .
      dockerfile: Dockerfile
    image: configvault-api:local
    ports:
      - "8083:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConfigVault__VaultBaseUrl: http://host.docker.internal:8087
      ConfigVault__PollingIntervalSeconds: "30"
      ConfigVault__ApiKeys__0: "your-api-key"
    extra_hosts:
      - "host.docker.internal:host-gateway"
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:8080/health"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 20s
    restart: unless-stopped
```

**Step 4: Run test to verify it passes**

Run: `powershell -File scripts/docker-smoke.ps1`
Expected: PASS with `docker compose config` output and exit code 0.

**Step 5: Commit**

```bash
git add scripts/docker-smoke.ps1 docker-compose.yml
git commit -m "feat: add docker compose for ConfigVault.Api"
```

### Task 3: Manual Runtime Smoke Check (Optional but Recommended)

**Files:**
- Test: `docker-compose.yml`

**Step 1: Write the failing test**

Add a temporary note to yourself (not committed) to validate runtime:

```text
TODO: verify API responds on http://localhost:8083/health
```

**Step 2: Run test to verify it fails**

Run: `docker compose up --build -d`
Expected: Container starts but `/health` may return 503 until Vaultwarden is unlocked.

**Step 3: Write minimal implementation**

Unlock Vaultwarden and ensure `ConfigVault__VaultBaseUrl` points to the correct host/port.

**Step 4: Run test to verify it passes**

Run: `curl http://localhost:8083/health`
Expected: 200 OK with JSON body containing `"status": "healthy"`.

**Step 5: Commit**

No commit (runtime validation only).
