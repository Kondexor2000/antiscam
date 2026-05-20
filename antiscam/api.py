from fastapi import FastAPI
from .models import Message
from .engine import calculate_risk

app = FastAPI()


@app.get("/")
def home():
    return {"message": "AntiScam API is running"}


@app.post("/scan")
def scan_message(msg: Message):
    return calculate_risk(msg.text)