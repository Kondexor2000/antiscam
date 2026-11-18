from pydantic import BaseModel, Field


class Message(BaseModel):
    text: str


class ScanResult(BaseModel):
    status: str
    risk_score: int
    reasons: list[str]
    safe_links: list[str]
    risky_links: list[str]


class ReportQuestion(BaseModel):
    question: str = Field(min_length=1, max_length=2000)
    top_k: int = Field(default=4, ge=1, le=10)