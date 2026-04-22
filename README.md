# TheMatrix

## Overview

TheMatrix is a simulation platform for running interconnected digital worlds.

It combines:

- cities and scenario setup
- time progression and simulation orchestration
- population, economy, resources, and supporting services
- gateway and frontend surfaces for operating those systems

## Repository Shape

- `TheMatrix.sln` contains the backend services, shared building blocks, and tools
- `frontend/matrix-web` contains the Vite + React frontend
- `docker-compose.yml` starts the local infrastructure dependencies
- `docs/` contains development, deployment, and security guides

## Quick Links

- Local environment: `docs/development/local-environment.md`
- User secrets: `docs/development/user-secrets.md`
- Database migrations: `docs/deployment/database-migrations.md`
- Trusted client IP forwarding: `docs/security/trusted-client-ip-forwarding.md`
- Internal JWT rotation: `docs/security/internal-jwt-key-rotation.md`

## Local Development

The normal local flow is:

1. Copy `.env.example` to `.env`
2. Start Postgres, Redis, and RabbitMQ with `docker compose up -d`
3. Configure local secrets for the supported entrypoint projects
4. Start the backend services from the solution
5. Start the frontend with `npm run dev` in `frontend/matrix-web`

The detailed setup guide lives in `docs/development/local-environment.md`.

## Русский

### Обзор

TheMatrix — это платформа симуляций для запуска взаимосвязанных цифровых миров.

Она объединяет:

- города и сценарии
- течение времени и orchestration симуляции
- население, экономику, ресурсы и supporting services
- gateway и frontend для управления системой

### Быстрые ссылки

- Локальное окружение: `docs/development/local-environment.md`
- User secrets: `docs/development/user-secrets.md`
- Миграции базы данных: `docs/deployment/database-migrations.md`
- Trusted client IP forwarding: `docs/security/trusted-client-ip-forwarding.md`
- Ротация internal JWT: `docs/security/internal-jwt-key-rotation.md`

### Локальная разработка

Базовый сценарий локального старта такой:

1. Скопировать `.env.example` в `.env`
2. Поднять Postgres, Redis и RabbitMQ через `docker compose up -d`
3. Настроить локальные секреты
4. Запустить backend-сервисы из solution
5. Запустить frontend через `npm run dev` в `frontend/matrix-web`

Подробности — в `docs/development/local-environment.md`.
