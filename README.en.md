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

### Ollama RAG for reports

The `/reports/index`, `/reports/search`, and `/reports/ask` endpoints provide a local RAG workflow over `.csv`, `.md`, and `.txt` files in `reports/`. The index uses Ollama embeddings (`nomic-embed-text`), cosine-similarity search, and a local SQLite database at `data/reports-vector-index.sqlite`. `/reports/ask` sends retrieved chunks to the local chat model and returns cited sources. The `.[agent]` extra is not required.

Start Ollama and pull both models:

```powershell
ollama pull nomic-embed-text
ollama pull qwen3.5:4b
```

After starting the API, index the reports and then search or ask a question:

```powershell
Invoke-RestMethod -Uri http://localhost:8000/reports/index -Method POST
Invoke-RestMethod -Uri http://localhost:8000/reports/ask `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"question":"What is the spam share in the SMS dataset?"}'
```

`POST /reports/search` accepts the same JSON and returns matching chunks with similarity scores. `top_k` is optional (1–10, default 4). Re-run `/reports/index` after report files change. Set `OLLAMA_URL`, `OLLAMA_MODEL`, and `OLLAMA_EMBEDDING_MODEL` to configure the service URL and models. Ollama failures return `503`; searching without an index or with a mismatched embedding model returns `409`.

## Tests

Run the Python test suite with:

```powershell
python -m pytest
```

See [docs/demo.md](docs/demo.md) for test details and manual API checks.

To explore CSV datasets in `data/`, install the optional analysis dependency with `python -m pip install -e ".[analysis]"` and run `python tools/analyze_datasets.py`. Reports are written to `reports/data-analysis/`.

## Docker and CI

```powershell
docker build -f ci/python/Dockerfile -t antiscam-api .
docker run --rm -p 8000:8000 antiscam-api
```

The Jenkins pipeline is in `ci/python/Jenkinsfile`.
