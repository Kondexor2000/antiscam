# Raport wydajności k6

Wygenerowano: 2026-10-31T17:11:50.885782+00:00

Scenariusze trwały 100 s: maksymalnie 20 req/s dla `/scan` i 10 req/s dla odczytu `/api/posts`. Pomiar wykonano lokalnie przez Docker Desktop; wyniki opisują to środowisko i nie są gwarancją pojemności produkcyjnej.

| Usługa | p50 (ms) | p95 (ms) | p99 (ms) | throughput (req/s) | błędy |
|---|---:|---:|---:|---:|---:|
| scan | 4.41 | 5.23 | 5.84 | 16.73 | 0.00% |
| blog | 2.62 | 3.34 | 3.92 | 8.29 | 0.00% |
