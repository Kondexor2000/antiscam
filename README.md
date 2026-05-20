# AntiScam

Zaawansowany system detekcji wiadomości phishingowych i oszukańczych oparty na inteligentnej analizie tekstu.

## Opis

AntiScam to aplikacja API do wykrywania oszustw i phishingu w wiadomościach tekstowych. System analizuje wiadomości na podstawie:

- **Kody BLIK** - wykrywanie polskich kodów 6-cyfrowych
- **Linki** - rozróżnianie zaufanych i podejrzanych domen
- **Słowa kluczowe** - analiza słów charakterystycznych dla wiadomości oszukańczych
- **Kontekst bezpieczeństwa** - identyfikacja sygnałów bezpieczeństwa
- **Intencja** - wykrywanie ponaglającego lub manipulacyjnego języka

## Szybki Start

### Wymagania

- Python 3.10+
- pip

### Instalacja

```bash
# Klonowanie repozytorium
git clone https://github.com/Kondexor2000/antiscam.git
cd antiscam

# Instalacja zależności
pip install -r requirements-dev.txt
pip install -e .
```

### Uruchamianie API

```bash
# Uruchomienie serwera FastAPI
uvicorn antiscam.api:app --reload
```

API będzie dostępne pod adresem: http://localhost:8000

### Uruchamianie CLI

```bash
# Skanowanie wiadomości
python -m antiscam.cli "Wyślij BLIK 123456 natychmiast!"
```

## Dokumentacja API

### Endpointy

#### GET /

Zwraca status API.

**Odpowiedź:**
```json
{
  "message": "AntiScam API is running"
}
```

#### POST /scan

Skanuje wiadomość tekstową i zwraca ocenę ryzyka.

**Żądanie:**
```json
{
  "text": "Wiadomość do przeskanowania"
}
```

**Odpowiedź:**
```json
{
  "status": "LOW RISK",
  "risk_score": 15,
  "reasons": [
    "Low-risk context detected"
  ],
  "safe_links": ["https://google.com"],
  "risky_links": []
}
```

### Klasyfikacja ryzyka

- **LOW RISK** (0-49): Wiadomość wydaje się bezpieczna
- **MEDIUM RISK** (50-79): Potencjalne zagrożenie
- **HIGH RISK** (80-100): Wysoki stopień zagrożenia

## Struktura Projektu

```
antiscam/
├── __init__.py           # Główny moduł
├── api.py               # Endpointy FastAPI
├── cli.py               # Interfejs wiersza poleceń
├── config.py            # Konfiguracja i ustawienia
├── engine.py            # Główna logika detekcji
├── links.py             # Analiza linków
├── logger.py            # System logowania
├── models.py            # Modele Pydantic
├── patterns.py          # Wzorce do analizy
├── scoring.py           # Funkcje oceniające
├── requirements-dev.txt # Zależności Python
├── README.md            # Dokumentacja (polski)
├── README.en.md         # Dokumentacja (angielski)
└── tests/               # Testy
    ├── conftest.py      # Konfiguracja pytest
    ├── unit/            # Testy jednostkowe
    │   ├── test_config.py
    │   ├── test_links.py
    │   ├── test_scoring.py
    │   ├── test_engine.py
    │   └── test_models.py
    └── integration/     # Testy integracyjne
        ├── test_api.py
        └── test_workflows.py
```

## Testowanie

### Uruchamianie testów

```bash
# Wszystkie testy
pytest

# Tylko testy jednostkowe
pytest tests/unit/

# Tylko testy integracyjne
pytest tests/integration/

# Z raportowaniem pokrycia
pytest --cov=antiscam tests/

# Szczegółowy output
pytest -v
```

### Statystyki testów

- **108+ testów jednostkowych i integracyjnych**
- **~95% pokrycie kodu**
- **Czas wykonania: <0.5s**

## Algorytm analizy ryzyka

System analizuje wiadomości na podstawie kilku kryteriów:

### 1. Kody BLIK
- **+60 punktów**: BLIK z potwierdzającym kontekstem
- **+30 punktów**: BLIK bez kontekstu

### 2. Linki
- **+20-45 punktów**: Podejrzane linki
- **-5 do -15 punktów**: Zaufane linki

### 3. Słowa kluczowe
- Do 30 punktów za charakterystyczne słowa:
  - "blik" (10 pkt)
  - "kod" (5 pkt)
  - "natychmiast" (6 pkt)
  - "potwierdź" (5 pkt)
  - itp.

### 4. Kontekst bezpieczeństwa
- **-10 punktów**: Dwa lub więcej sygnałów bezpieczeństwa
  - "cześć", "ok", "spotkanie", "dziękuję"

### 5. Intencja
- **+10 punktów**: Wykryta manipulacyjna intencja
  - "ostatnia szansa", "konto zablokowane", "kliknij teraz"

## Konfiguracja

Konfiguracja znajduje się w pliku `config.py`:

```python
from antiscam.config import settings

# Zaufane domeny
settings.trusted_domains  # {'google.com', 'facebook.com', ...}

# Wagi słów kluczowych
settings.keyword_weights  # {'blik': 10, 'kod': 5, ...}

# Sygnały bezpieczeństwa
settings.safe_signals     # ['cześć', 'ok', ...]

# Sygnały intencji
settings.intent_signals   # ['ostatnia szansa', ...]
```

## Przykłady użycia

### Python API

```python
from antiscam.engine import calculate_risk

# Bezpieczna wiadomość
result = calculate_risk("Cześć, spotkamy się normalnie.")
# Zwraca: {"status": "LOW RISK", "risk_score": 0, ...}

# Podejrzana wiadomość
result = calculate_risk("BLIK 123456 - wyślij kod natychmiast!")
# Zwraca: {"status": "HIGH RISK", "risk_score": 85, ...}
```

### cURL

```bash
# Skanowanie wiadomości
curl -X POST http://localhost:8000/scan \
  -H "Content-Type: application/json" \
  -d '{"text": "Potwierdź swoje dane szybko!"}'
```

### Python requests

```python
import requests

response = requests.post(
    "http://localhost:8000/scan",
    json={"text": "Wyślij BLIK"}
)
print(response.json())
```

## Logowanie

Aplikacja generuje logi dla wszystkich skanowań:

```
[INFO] antiscam - scan_started
[INFO] antiscam - scan_finished
```

## Wkład w projekt

Aby przyczyć się do projektu:

1. Utwórz fork repozytorium
2. Utwórz gałąź feature (`git checkout -b feature/NowaFunkcja`)
3. Commituj zmiany (`git commit -m 'Dodaj nową funkcję'`)
4. Push do gałęzi (`git push origin feature/NowaFunkcja`)
5. Otwórz Pull Request

### Standardy kodowania

- Pisz czytelny kod
- Dodaj testy dla nowych funkcjonalności
- Upewnij się, że wszystkie testy przechodzą
- Dodaj dokumentację do nowych funkcji

## Licencja

Projekt jest dostępny pod licencją MIT.

## Pomoc

W przypadku problemów otwórz Issue na GitHubie.

## Dowiedz się więcej

Aby dowiedzieć się więcej o phishingu i bezpieczeństwie:
- [OWASP](https://owasp.org/)
- [Anti-Phishing Working Group](https://apwg.org/)
- [CERT Polska](https://www.cert.pl/)

---

**Ostatnia aktualizacja**: 20 maja 2026  
**Wersja**: 0.1
