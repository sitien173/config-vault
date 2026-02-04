$ErrorActionPreference = "Stop"

docker build -t configvault-api:dev .
docker compose -f docker-compose.yml config
