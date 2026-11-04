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

## Demo 16: Opcjonalny agent LangGraph w `/ai/explain`

Agent jest dodatkiem wymagającym klucza OpenAI. Nie zmienia wyniku endpointu `/scan`. W terminalu PowerShell zainstaluj opcjonalne zależności, ustaw konfigurację i uruchom API:

```powershell
python -m pip install -e ".[agent]"
$env:OPENAI_API_KEY = "<twój-klucz>"
$env:ANTISCAM_AI_AGENT_ENABLED = "1"
# Opcjonalnie: $env:ANTISCAM_AI_AGENT_MODEL = "gpt-4o-mini"
uvicorn antiscam.api:app --reload
```

Wyślij wiadomość z podejrzanym adresem:

```powershell
$report = Invoke-RestMethod -Uri http://localhost:8000/ai/explain `
  -Method POST `
  -ContentType "application/json" `
  -Body (@{ text = "Zaloguj się na https://g00gle.com/login aby odebrać nagrodę" } | ConvertTo-Json)

$report | Select-Object agent_used, agent_explanation, scan_status, risk_score
```

Oczekiwany wynik: `agent_used` ma wartość `True`, a `agent_explanation` zawiera wyjaśnienie po polsku. Agent powinien skorzystać z narzędzia skanera, a dla URL-i z narzędzia sprawdzającego lokalne reguły i listę CERT Polska. `scan_status` i `risk_score` pochodzą nadal z lokalnego `/scan`; agent ich nie ustala. Jeśli dodatek lub klucz API nie są dostępne albo wywołanie modelu się nie powiedzie, endpoint zwraca dotychczasowy raport deterministyczny z `agent_used = False`.

Ten tryb wysyła treść wiadomości do skonfigurowanego dostawcy modelu. Włączaj go tylko wtedy, gdy sposób przetwarzania danych przez dostawcę jest akceptowalny dla tych wiadomości.

## Testy regresyjne API

Poniższe testy ręczne sprawdzają, czy zaufany link nie obniża ryzyka typosquattingu ani potwierdzonego kodu BLIK oraz czy kod BLIK jest redagowany w odpowiedziach API.

Sprawdź, czy podejrzana domena nadal ma wysoki wynik, nawet gdy wiadomość zawiera również zaufany link:

```powershell
$result = Invoke-RestMethod -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -Body (@{ text = "Czesc, spotkanie, normalnie https://g00gle.com https://google.com" } | ConvertTo-Json)

if ($result.status -ne "HIGH RISK" -or $result.risk_score -lt 90) {
  throw "Test nieudany: typosquatting powinien zachować wynik co najmniej 90."
}
if (-not ($result.reasons -join " ").Contains("Typosquatting links")) {
  throw "Test nieudany: odpowiedź nie zawiera powodu typosquattingu."
}
"OK: typosquatting zachowuje wysoki wynik ryzyka."
```

Sprawdź, czy zaufany link nie obniża wyniku potwierdzonego kodu BLIK i czy kod nie pojawia się w powodach skanowania:

```powershell
$result = Invoke-RestMethod -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -Body (@{ text = "Czesc, spotkanie normalnie, BLIK kod 123456 https://google.com" } | ConvertTo-Json)

if ($result.risk_score -lt 60) {
  throw "Test nieudany: potwierdzony BLIK powinien zachować wynik co najmniej 60."
}
if (($result.reasons -join " ").Contains("123456")) {
  throw "Test nieudany: kod BLIK nie został zredagowany w powodach skanowania."
}
"OK: ryzyko BLIK zachowane, kod zredagowany."
```

Sprawdź redakcję kodu również w raporcie `/ai/explain`:

```powershell
$report = Invoke-RestMethod -Uri http://localhost:8000/ai/explain `
  -Method POST `
  -ContentType "application/json" `
  -Body (@{ text = "Wyslij BLIK 123456 natychmiast" } | ConvertTo-Json)

$explanation = "$($report.block_explanation) $($report.scan_reasons -join ' ') $($report.extracted_terms -join ' ')"
if ($explanation.Contains("123456")) {
  throw "Test nieudany: kod BLIK pojawia się w raporcie AI."
}
if (-not $report.blocked_after_scan) {
  throw "Test nieudany: ryzykowna wiadomość powinna zostać zablokowana."
}
"OK: raport AI blokuje ryzykowną wiadomość i nie ujawnia kodu."
```

## Demo 15: Odtworzenie i sprawdzenie raportów zbiorów danych

Ta część sprawdza statystyki opisowe CSV używanych w desk research. Nie ocenia skuteczności klasyfikatora.

Zainstaluj opcjonalną zależność i wygeneruj raporty ponownie:

```powershell
python -m pip install -e ".[analysis]"
python tools/analyze_datasets.py
```

Skrypt tworzy `reports/data-analysis/dataset_summary.csv` i `class_distribution.csv`. Sprawdź liczebność, duplikaty, brakujące wartości oraz udziały klas:

```powershell
$summary = Import-Csv reports/data-analysis/dataset_summary.csv
$classes = Import-Csv reports/data-analysis/class_distribution.csv

$sms = $summary | Where-Object dataset -eq "uci_sms_spam_collection.csv"
$websites = $summary | Where-Object dataset -eq "uci_phishing_websites.csv"
$phiusiil = $summary | Where-Object dataset -eq "uci_phiusiil_phishing_url.csv"
$smsSpam = $classes | Where-Object { $_.dataset -eq "uci_sms_spam_collection.csv" -and $_.class -eq "spam" }
$phiusiilPhishing = $classes | Where-Object { $_.dataset -eq "uci_phiusiil_phishing_url.csv" -and $_.class -eq "1" }
$phiusiilLegitimate = $classes | Where-Object { $_.dataset -eq "uci_phiusiil_phishing_url.csv" -and $_.class -eq "0" }

if ([int]$sms.rows -ne 5572 -or [int]$sms.duplicate_rows -ne 403) {
  throw "SMS: oczekiwano 5572 wierszy i 403 duplikatów."
}
if ([math]::Abs([double]$smsSpam.percentage - 13.41) -gt 0.01) {
  throw "SMS: udział spamu odbiega od oczekiwanych 13,41%."
}
if ([int]$websites.rows -ne 11055 -or [int]$websites.duplicate_rows -ne 5206) {
  throw "Phishing Websites: oczekiwano 11055 wierszy i 5206 duplikatów."
}
if ([int]$phiusiil.rows -ne 235795) {
  throw "PhiUSIIL: oczekiwano 235795 wierszy."
}
if ([math]::Abs([double]$phiusiilPhishing.percentage - 57.19) -gt 0.01 -or
    [math]::Abs([double]$phiusiilLegitimate.percentage - 42.81) -gt 0.01) {
  throw "PhiUSIIL: udziały klas odbiegają od oczekiwanych 57,19% / 42,81%."
}
if (($summary | Where-Object { [int]$_.missing_cells -ne 0 }).Count -gt 0) {
  throw "Co najmniej jeden zbiór zawiera brakujące wartości."
}

"OK: liczebności, udziały klas i brakujące wartości są zgodne z raportem."
```

Oczekiwane wartości odnoszą się do dostarczonych CSV. SMS ma 5 572 niepuste rekordy w eksporcie; UCI podaje 5 574 instancje w opisie zbioru. Różnica wynika z pominięcia pustych linii podczas eksportu. Duplikaty są pełnymi powtórzeniami wierszy i nie należy ich automatycznie usuwać bez analizy wpływu na badanie.

### Co te raporty mówią o modelu

Statystyki opisowe nie są metrykami jakości klasyfikatora. Nie wyliczają precision, recall, false positive rate ani liczby fałszywych alarmów. Obecny model w `train.py` uczy się na 16 ręcznie przygotowanych przykładach; SMS Spam Collection jest po angielsku, a tabele phishingowych URL-i zawierają gotowe cechy witryn, nie polskie wiadomości. Te zbiory mogą wspierać opis danych i osobne eksperymenty, ale nie tworzą reprezentatywnego testu dla polskiego modelu tekstowego.

Wynik `p=...` zwracany przez model nie został skalibrowany na niezależnym, reprezentatywnym zbiorze. Nie należy interpretować go jako wiarygodnego prawdopodobieństwa ani dostrajać progów na podstawie samych raportów eksploracyjnych. Do takiej oceny potrzebny jest osobny, oznaczony zbiór polskich wiadomości z rozdzielonymi zestawami treningowym i testowym oraz jawnie policzonymi precision, recall i fałszywymi alarmami.

### Ostrzeżenia środowiskowe

Testy można uruchomić poleceniem `python -m pytest`; ostrzeżenia o niezgodności wersji modelu lub zależności nie są metrykami modelu i nie powinny być mylone z wynikiem desk research. Jeżeli pojawi się ostrzeżenie o wersji zapisanej w lokalnym pliku modelu, przebuduj go w aktywnym środowisku:

```powershell
python train.py
python -m pytest
```

Pylance działa w edytorze, więc jego diagnostyka nie jest sprawdzana przez `demo.md` ani przez pytest. Upewnij się, że edytor otworzył katalog główny repozytorium i wybrał to samo środowisko Python, w którym działa `python -m pytest`. Import wykonania można niezależnie sprawdzić poleceniem:

```powershell
python -c "from antiscam.ai import calculate_risk; print('Import OK:', calculate_risk('test')['status'])"
```
