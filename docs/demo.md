# Testowanie projektu Python

Instrukcje dotyczą wyłącznie silnika AntiScam w Pythonie (FastAPI). Komendy uruchamiaj z katalogu głównego repozytorium.

## Przygotowanie środowiska

Wymagany jest Python 3.10 lub nowszy. W PowerShell utwórz i aktywuj środowisko, a następnie zainstaluj zależności:

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install -r requirements-dev.txt
python -m pip install -e .
```

## Pełny zestaw testów

Uruchom wszystkie testy jednostkowe i integracyjne:

```powershell
python -m pytest
```

Aby otrzymać raport pokrycia:

```powershell
python -m pytest --cov=antiscam --cov-report=term-missing
```

Testy jednostkowe sprawdzają wykrywanie kodów BLIK, ocenę ryzyka, normalizację tekstu, linki, model ML, konfigurację i pozostałe moduły silnika. Testy integracyjne sprawdzają API FastAPI: odpowiedzi `/` i `/scan`, klasyfikację wiadomości bezpiecznych i ryzykownych, obsługę brakujących danych, linki zaufane i podejrzane, literówki domen oraz endpoint `/ai/explain`.

Możesz uruchomić wybraną grupę:

```powershell
python -m pytest tests\unit
python -m pytest tests\integration
```

## Ręczny test API Python

Uruchom API:

```powershell
uvicorn antiscam.api:app --reload
```

W drugim oknie PowerShell wyślij bezpieczną wiadomość:

```powershell
Invoke-RestMethod -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"text":"Cześć, spotkamy się jutro."}'
```

Odpowiedź powinna wskazywać `LOW RISK`. Sprawdź też wiadomość oszukańczą:

```powershell
Invoke-RestMethod -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"text":"Wyślij BLIK 123456 natychmiast i potwierdź kod"}'
```

Odpowiedź powinna wskazywać `HIGH RISK` i zawierać powody oceny.

## Demo 10: Python API i bazowy wynik ML

Uruchom API:

```powershell
uvicorn antiscam.api:app --reload
```

W drugim oknie PowerShell wyślij wiadomość z kodem BLIK:

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: wysoki wynik ryzyka. W polu `reasons` widoczny jest też bazowy wynik `ML intent score`, wyliczony przez hybrydowy pipeline TF-IDF + Naive Bayes.

## Demo 11: Hybryda ML i twarde reguły

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Konto zablokowane, kliknij https://g00gle.com/login i potwierdz kod BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: `HIGH RISK`. Model ML nadaje bazowy wynik intencji, a reguły BLIK, podejrzanego linku i typosquattingu działają jako twarde modyfikatory wyniku.

## Demo 12: Normalizacja obfuskacji

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"B L I K 123456 k-o-d natychmiast"}'
```

Oczekiwany wynik: `HIGH RISK`. `normalization.py` łączy rozstrzelone litery, usuwa proste znaki wstawione w słowa i przekazuje oczyszczony tekst do `engine.py`.

## Demo 13: Bezpieczne wycinanie domen i literówki

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Nie loguj sie przez https://google.com.evil.example ani https://g00gle.com/login"}'
```

Oczekiwany wynik: `HIGH RISK`. `links.py` używa `tldextract`, więc `google.com.evil.example` nie jest traktowane jak zaufane `google.com`, a `g00gle.com` trafia do `Typosquatting links` dzięki odległości Levenshteina.

## Demo 14: Python AI explain

```powershell
Invoke-WebRequest -Uri http://localhost:8000/ai/explain `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: raport AI/NLP z `blocked_after_scan`, `block_explanation`, `scan_reasons` i zaleceniem bezpiecznej reakcji.
