# Log Pusher

## Requirement
C# library that can push logs directly to Grafana Alloy (bypassing OpenTelemetry setup complexity).

## Current State
- Scraper service uses OpenTelemetry with OTLP HTTP endpoint (port 4318)
- Logs flow: C# App → OpenTelemetry → Alloy → Loki → Grafana
- SessionId correlation working for domain events

## Proposed Solution
Create `LogPusher` library:
- Direct HTTP client for Alloy OTLP endpoint
- Simplified API compared to full OpenTelemetry setup
- Maintain existing domain event structure and sessionId correlation
- Batch sending with retry logic

## Benefits
- Simpler integration (no OpenTelemetry packages needed)
- Direct control over log format and sending
- Reduced dependencies and complexity
- Custom retry and batching logic