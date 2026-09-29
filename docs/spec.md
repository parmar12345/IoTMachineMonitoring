@'
# Real-Time IoT Machine Monitoring Dashboard

## 1. Project Overview

A real-time IoT machine monitoring system that receives telemetry from industrial machines, processes the data, stores historical measurements, and displays live machine health and trends on a web dashboard.

## 2. Main Goal

Build a production-oriented system capable of monitoring a large number of machines with:

- Real-time telemetry ingestion
- MQTT-based device communication
- High-throughput data processing
- Time-series storage
- Redis-based caching
- Real-time browser updates
- Machine health monitoring
- Historical trend analysis
- Scalable architecture

## 3. System Flow

Machine / Simulator
        ↓
MQTT Broker
        ↓
Ingestion Worker
        ↓
Processing
        ↓
TimescaleDB
        ↓
API
        ↓
Web Dashboard

Redis will be used for caching and fast-access data.

## 4. Core Components

### API

ASP.NET Core Web API responsible for:

- Machine information
- Current machine status
- Historical telemetry queries
- Dashboard data
- Health and monitoring endpoints

### IngestionWorker

Background .NET worker responsible for:

- Consuming MQTT telemetry
- Validating messages
- Processing telemetry
- Writing measurements to TimescaleDB
- Updating real-time state/cache

### EdgeGateway

Responsible for communication between machine/edge devices and the central system.

### Simulator

Generates realistic machine telemetry for development and load testing.

### Shared

Contains common:

- DTOs
- Contracts
- Message models
- Constants
- Shared utilities

## 5. Infrastructure

### MQTT

Mosquitto is used as the MQTT broker.

Default port: 1883

### Database

PostgreSQL with TimescaleDB is used for:

- Machine metadata
- Telemetry measurements
- Historical time-series queries

Default port: 5432

### Cache

Redis is used for:

- Current machine state
- Frequently accessed dashboard data
- Fast temporary data

Default port: 6379

## 6. Example Telemetry

A machine can publish telemetry containing:

- Machine ID
- Timestamp
- Temperature
- Pressure
- Vibration
- RPM
- Power consumption
- Machine status

Example:

{
  "machineId": "MACHINE-001",
  "timestamp": "2026-09-28T10:00:00Z",
  "temperature": 72.5,
  "pressure": 4.2,
  "vibration": 1.8,
  "rpm": 1450,
  "power": 12.4,
  "status": "Running"
}

## 7. Initial Scale Target

1,000 machines × 10 metrics × 1 measurement/second

Target throughput: 10,000 telemetry points/second.

## 8. Real-Time Requirement

Telemetry should move from MQTT publication to the browser dashboard with a target p95 latency below 2 seconds.

## 9. Development Principles

- Separation of responsibilities
- Dependency injection
- Async programming
- Structured logging
- Environment-based configuration
- Docker-based infrastructure
- Automated testing
- Observability
- Scalability
- Maintainable code

## 10. Version 1 Scope

1. MQTT telemetry ingestion
2. Telemetry validation
3. Time-series storage
4. Redis current-state caching
5. REST API
6. Real-time dashboard updates
7. Machine monitoring
8. Simulator/load testing
9. Basic observability
10. Dockerized infrastructure
'@ | Set-Content docs/spec.md