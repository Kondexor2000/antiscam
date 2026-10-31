# AntiScam

A Python engine for detecting phishing and scam messages. It provides a FastAPI service, hybrid risk scoring based on machine learning and rules, and link analysis.

## Requirements

- Python 3.10 or newer
- pip

## Install and run

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install -r requirements-dev.txt
python -m pip install -e .
python train.py
uvicorn antiscam.api:app --reload
```

The API is available at `http://localhost:8000`. The main scanning endpoint is `POST /scan`; analysis explanations are available at `POST /ai/explain`.

## Tests

Run the Python test suite with:

```powershell
python -m pytest
```

See [docs/demo.md](docs/demo.md) for test details and manual API checks.

## Docker and CI

```powershell
docker build -f ci/python/Dockerfile -t antiscam-api .
docker run --rm -p 8000:8000 antiscam-api
```

The Jenkins pipeline is in `ci/python/Jenkinsfile`.
