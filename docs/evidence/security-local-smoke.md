# Raport testów bezpieczeństwa API

Wykonano: 2026-10-31T17:11:38.0433207Z

Środowisko: izolowany Docker Compose (`antiscam-evidence`, Python API na porcie 8010).

| Scenariusz | Wynik | Odpowiedź |
|---|---|---|
| Obfuskowany BLIK 1 | PASS | HIGH RISK, score 100 |
| Obfuskowany BLIK 2 | PASS | HIGH RISK, score 100 |
| Homoglify w prośbie o kod | PASS | HIGH RISK, score 94 |
| Domena z literówką | PASS | HIGH RISK, score 100 |
| Domena z podrobioną subdomeną | PASS | MEDIUM RISK, score 70 |
| Domena PayPal z podrobionym hostem | PASS | HIGH RISK, score 92 |
| HTML/JS i link phishingowy | PASS | HIGH RISK, score 100 |
| Izolacja stanu między żądaniami 1 | PASS | HIGH RISK, score 100 |
| Izolacja stanu między żądaniami 2 | PASS | HIGH RISK, score 100 |
| Izolacja stanu między żądaniami 3 | PASS | MEDIUM RISK, score 70 |

Zakres: regresje z `tests/integration/test_penetration.py`; sprawdza obfuskację BLIK, linki podszywające się pod domeny, HTML/JS i izolację kolejnych żądań.
