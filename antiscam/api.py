from fastapi import FastAPI, HTTPException
from .models import Message, ReportQuestion
from .engine import calculate_risk
from .ai import explain_ai_assistance
from .reports_rag import (
    OllamaError,
    ReportIndexModelMismatch,
    ReportIndexNotFound,
    ReportRAG,
)

app = FastAPI()
report_rag = ReportRAG()


@app.get("/")
def home():
    return {"message": "AntiScam API is running"}


@app.post("/scan")
def scan_message(msg: Message):
    return calculate_risk(msg.text)


@app.post("/ai/explain")
def explain_ai_message(msg: Message):
    return explain_ai_assistance(msg.text)


@app.post("/reports/index")
def index_reports():
    try:
        return report_rag.index_reports()
    except FileNotFoundError as error:
        raise HTTPException(status_code=404, detail=str(error)) from error
    except OllamaError as error:
        raise HTTPException(status_code=503, detail=str(error)) from error


@app.post("/reports/search")
def search_reports(request: ReportQuestion):
    try:
        results = report_rag.search(request.question, request.top_k)
    except ReportIndexNotFound as error:
        raise HTTPException(status_code=409, detail=str(error)) from error
    except ReportIndexModelMismatch as error:
        raise HTTPException(status_code=409, detail=str(error)) from error
    except OllamaError as error:
        raise HTTPException(status_code=503, detail=str(error)) from error
    return {
        "question": request.question,
        "results": [
            {
                "source": result.source,
                "content": result.content,
                "similarity": result.similarity,
            }
            for result in results
        ],
    }


@app.post("/reports/ask")
def ask_reports(request: ReportQuestion):
    try:
        return report_rag.ask(request.question, request.top_k)
    except ReportIndexNotFound as error:
        raise HTTPException(status_code=409, detail=str(error)) from error
    except ReportIndexModelMismatch as error:
        raise HTTPException(status_code=409, detail=str(error)) from error
    except OllamaError as error:
        raise HTTPException(status_code=503, detail=str(error)) from error
