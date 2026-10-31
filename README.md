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

## Docker i CI

Obraz API zbudujesz i uruchomisz z katalogu repozytorium:

```powershell
docker build -f ci/python/Dockerfile -t antiscam-api .
docker run --rm -p 8000:8000 antiscam-api
```

Konfiguracja pipeline Jenkins znajduje się w `ci/python/Jenkinsfile`.
