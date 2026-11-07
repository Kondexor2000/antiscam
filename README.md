# AntiScam

Pythonowy silnik do wykrywania wiadomości phishingowych i oszustw. Udostępnia API FastAPI, hybrydową ocenę ryzyka opartą na modelu ML i regułach oraz analizę linków.

## Wymagania

- Python 3.10 lub nowszy
- pip

## Instalacja i uruchomienie

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install -r requirements-dev.txt
python -m pip install -e .
python train.py
uvicorn antiscam.api:app --reload
```

API domyślnie działa pod `http://localhost:8000`. Główny endpoint skanowania to `POST /scan`, a objaśniania analizy `POST /ai/explain`.

Opcjonalny agent LangGraph działa wyłącznie w `/ai/explain`; `/scan` zawsze zwraca wynik deterministycznego silnika. Zainstaluj dodatek `python -m pip install -e ".[agent]"`, uruchom lokalną usługę Ollama, pobierz model `ollama pull qwen3.5:4b`, ustaw `ANTISCAM_AI_AGENT_ENABLED=1` i uruchom API. Domyślnym dostawcą jest lokalna Ollama; model i adres usługi można zmienić przez `OLLAMA_MODEL` i `OLLAMA_URL`. Alternatywnie ustaw `ANTISCAM_AI_AGENT_PROVIDER=openai`, `OPENAI_API_KEY` oraz opcjonalnie `ANTISCAM_AI_AGENT_MODEL` (domyślnie `gpt-4o-mini`). Agent używa narzędzia lokalnego skanera i, jeśli wiadomość zawiera URL-e, sprawdza je lokalnymi regułami oraz listą ostrzeżeń CERT Polska. Brak potwierdzonego dopasowania do listy nie oznacza, że domena jest bezpieczna. Jeśli agent jest wyłączony lub dostawca nie odpowiada, raport pozostaje dostępny w trybie deterministycznym.

Przykładowe żądanie:

```powershell
Invoke-RestMethod -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"text":"Wyślij BLIK 123456 natychmiast"}'
```

## Testy

Instrukcje uruchamiania testów i ręcznej weryfikacji API znajdują się w [docs/demo.md](docs/demo.md).

```powershell
python -m pytest
```

Publiczne zbiory danych do analiz desk research są opisane w [docs/datasets.md](docs/datasets.md). Pobierzesz je do `data/` poleceniem `python tools/fetch_datasets.py` po instalacji dodatku `python -m pip install -e ".[datasets]"`.

Analizę eksploracyjną plików CSV z `data/` uruchomisz po instalacji `python -m pip install -e ".[analysis]"` poleceniem `python tools/analyze_datasets.py`. Raporty zapisują się w `reports/data-analysis/`.

## Docker i CI

Obraz API zbudujesz i uruchomisz z katalogu repozytorium:

```powershell
docker build -f ci/python/Dockerfile -t antiscam-api .
docker run --rm -p 8000:8000 antiscam-api
```

Konfiguracja pipeline Jenkins znajduje się w `ci/python/Jenkinsfile`.
