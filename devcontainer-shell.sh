#!/bin/bash
docker compose -f .devcontainer/docker-compose.yml up -d

docker compose -f .devcontainer/docker-compose.yml exec vintage-mod-dev bash
