FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY pyproject.toml train.py ./
COPY antiscam ./antiscam

RUN python -m pip install --no-cache-dir . \
    && python train.py

EXPOSE 8000

CMD ["uvicorn", "antiscam.api:app", "--host", "0.0.0.0", "--port", "8000"]
