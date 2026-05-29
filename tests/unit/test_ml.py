"""Unit tests for the shallow ML risk model."""

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
