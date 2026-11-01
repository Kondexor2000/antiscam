# Raport testów odporności

Rozpoczęto: 2026-10-31T17:02:16.0951498Z

| Scenariusz | Wynik |
|---|---|
| Uruchomienie usług i zapis wpisu kontrolnego | OK (resilience-check-resilience-1793466136-43cd23e3) |
| Utworzenie backupu SQL Server | OK |
| Wyłączenie SQL Server | OK (API zwróciło 500) |
| Restart SQL Server/API i zachowanie danych na wolumenie | OK |
| Wyłączenie i restart silnika Python | OK |
| Odtworzenie backupu i odzyskanie wpisu kontrolnego | OK |

Zakończono: 2026-10-31T17:02:52.6453144Z
