@'
# V1 Targets

| # | Target | Value | Tested By | Day |
|---|---|---|---|---|
| T1 | Throughput | 10,000 points/s (1,000 machines × 10 metrics @ 1 Hz) | Simulator load test | 14, 28 |
| T2 | Latency | p95 < 2 s, publish → browser tile | End-to-end latency test | 14, 28 |
| T3 | Ingestion Reliability | No telemetry loss during normal operation | Ingestion integration test | 14, 28 |
| T4 | Data Persistence | Telemetry stored in TimescaleDB | Database integration test | 7, 14 |
| T5 | Cache Performance | Current machine state available from Redis | Redis integration test | 7, 14 |
| T6 | API Availability | Health endpoint responds successfully | API integration test | 7, 14 |
| T7 | Container Startup | All infrastructure services start with Docker Compose | Docker Compose test | 1 |
| T8 | Build | All .NET projects build successfully | dotnet build | 1 |
| T9 | Testability | Core ingestion and API logic covered by automated tests | Automated test suite | 21, 28 |
| T10 | Observability | Structured logs and basic health checks available | Manual + integration test | 21, 28 |

## Infrastructure Targets

- MQTT broker: Mosquitto
- Database: PostgreSQL + TimescaleDB
- Cache: Redis
- Container orchestration for local development: Docker Compose

## Scale Target

Initial target:

- 1,000 machines
- 10 metrics per machine
- 1 telemetry message per metric per second
- 10,000 telemetry points per second

## Latency Target

End-to-end telemetry latency:

MQTT publish → ingestion → processing → API/realtime update → browser

Target:

**p95 < 2 seconds**

## Reliability Target

The system should process telemetry reliably during normal operation without silently dropping valid telemetry messages.

Any intentional dropping, retry, backpressure, or failure behavior must be documented.

## Change Rule

Any architectural or implementation change that breaks one of these targets must be documented with an Architecture Decision Record (ADR) in:

`docs/adr/`
'@ | Set-Content docs/targets.md