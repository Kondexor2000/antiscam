"""Unit tests for the shallow ML risk model."""

import warnings
from pathlib import Path

from sklearn.exceptions import InconsistentVersionWarning

import antiscam.ml as ml
from antiscam.ml import classify_message


def test_ml_classifier_scores_scam_intent_above_safe_message():
    scam = classify_message("konto zablokowane kliknij link i potwierdz dane natychmiast")
    safe = classify_message("czesc spotkamy sie jutro o trzeciej")

    assert scam.label == "scam"
    assert scam.score > safe.score


def test_ml_classifier_empty_message_is_zero_risk():
    result = classify_message("")

    assert result.label == "safe"
    assert result.scam_probability == 0.0
    assert result.score == 0


def test_load_classifier_suppresses_inconsistent_version_warning(monkeypatch):
    calls = []

    def fake_filterwarnings(action, category=None, module=None, lineno=0, append=False):
        calls.append((action, category, module, append))

    def fake_load(_path):
        return object()

    monkeypatch.setattr(ml.joblib, "load", fake_load)
    monkeypatch.setattr(ml.warnings, "filterwarnings", fake_filterwarnings)

    result = ml.load_classifier(Path("models/model.joblib"))

    assert result is not None
    assert calls
    assert calls[0][1] is InconsistentVersionWarning
    assert calls[0][2] == r"sklearn\..*"
