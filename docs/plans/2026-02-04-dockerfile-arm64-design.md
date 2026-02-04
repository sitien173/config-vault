# Dockerfile Arm64 Multi-Arch Refactor Design

**Goal:** Make the Dockerfile platform-aware and optimized for multi-arch builds (linux/amd64 + linux/arm64) while keeping runtime behavior unchanged.

**Background:** The current Dockerfile builds and runs on the default platform only. We want to support reliable linux/arm64 output and keep the healthcheck (`curl`) and env-based configuration intact.

**Proposed Changes:**
- Update build stage to be platform-aware:
  - `FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build`
  - Declare `ARG TARGETOS`, `TARGETARCH`, `TARGETPLATFORM`, `BUILDPLATFORM`.
- Keep multi-stage flow but add BuildKit cache mounts for NuGet to speed CI/local builds.
- Publish with a target runtime for the requested platform:
  - `-r linux-$TARGETARCH` for framework-dependent output.
- Update runtime stage to be platform-aware:
  - `FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0`.
- Keep `curl` install and `/health` healthcheck unchanged.
- Optional: add guarded buildx step to `scripts/docker-smoke.ps1` (only when env var is set).

**Build/Usage:**
- Multi-arch publish:
  - `docker buildx build --platform linux/amd64,linux/arm64 -t <registry>/<image>:<tag> --push .`
- Local arm64 test:
  - `docker buildx build --platform linux/arm64 -t configvault-api:arm64 --load .`

**Verification:**
- `docker build .` still succeeds on default platform.
- `docker buildx build --platform linux/arm64` succeeds (manual or gated in smoke script).

**Risks/Notes:**
- Buildx/QEMU required for cross-platform builds on amd64 hosts.
- If target runtime assets differ, `-r linux-$TARGETARCH` ensures correct output.
