from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import joblib
from sklearn.pipeline import Pipeline


@dataclass(frozen=True)
class MlRiskAssessment:
    label: str
    scam_probability: float
    score: int


BASE_DIR = Path(__file__).resolve().parent.parent

MODEL_PATH = BASE_DIR / "models" / "model.joblib"

if not MODEL_PATH.exists():
    raise FileNotFoundError(
        f"Nie znaleziono modelu: {MODEL_PATH}. "
        "Uruchom najpierw train.py."
    )

_CLASSIFIER: Pipeline = joblib.load(MODEL_PATH)

_CLASSES = list(_CLASSIFIER.classes_)

if "scam" not in _CLASSES:
    raise RuntimeError(
        "Model nie zawiera klasy 'scam'."
    )

_SCAM_INDEX = _CLASSES.index("scam")


def classify_message(text: str) -> MlRiskAssessment:
    text = text.strip()

    if not text:
        return MlRiskAssessment(
            label="safe",
            scam_probability=0.0,
            score=0,
        )

    probabilities = _CLASSIFIER.predict_proba([text])[0]

    scam_probability = float(
        probabilities[_SCAM_INDEX]
    )

    label = (
        "scam"
        if scam_probability >= 0.5
        else "safe"
    )

    return MlRiskAssessment(
        label=label,
        scam_probability=scam_probability,
        score=round(scam_probability * 100),
    )