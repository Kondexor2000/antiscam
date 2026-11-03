import joblib

from train import TRAINING_SAMPLES, train


def test_train_saves_model_to_requested_path_and_handles_polish_accents(tmp_path):
    model_path = tmp_path / "nested" / "model.joblib"

    train(model_path)

    classifier = joblib.load(model_path)
    unaccented = classifier.predict_proba(
        ["wyslij kod blik natychmiast ostatnia szansa"]
    )
    accented = classifier.predict_proba(
        ["wyślij kod blik natychmiast ostatnia szansa"]
    )

    assert model_path.is_file()
    assert len(TRAINING_SAMPLES) == 16
    assert (unaccented == accented).all()
