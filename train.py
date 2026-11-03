from pathlib import Path

import joblib
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.naive_bayes import MultinomialNB
from sklearn.pipeline import Pipeline

# The available UCI SMS corpus is English and the URL datasets contain
# structured URL features, so neither is representative training data here.
TRAINING_SAMPLES = (
    ("wyslij kod blik natychmiast ostatnia szansa", "scam"),
    ("konto zablokowane kliknij link i potwierdz dane", "scam"),
    ("doplat 1 zl do paczki inaczej zwrot przesylki", "scam"),
    ("bank wymaga pilnej weryfikacji hasla", "scam"),
    ("odbierz nagrode podaj kod sms teraz", "scam"),
    ("twoje konto zostanie zablokowane zaloguj sie natychmiast", "scam"),
    ("prosimy o szybki przelew na nowy rachunek", "scam"),
    ("kliknij teraz aby uniknac blokady konta", "scam"),
    ("czesc spotkamy sie jutro o trzeciej", "safe"),
    ("dziekuje za dokument wysle poprawki wieczorem", "safe"),
    ("przypomnienie o spotkaniu zespolu w poniedzialek", "safe"),
    ("artykul edukacyjny o ochronie przed phishingiem", "safe"),
    ("potwierdzam odbior paczki wszystko ok", "safe"),
    ("normalna wiadomosc od znajomego bez linku", "safe"),
    ("raport jest gotowy do omowienia na zajeciach", "safe"),
    ("hej czy mozesz oddzwonic po pracy", "safe"),
)

BASE_DIR = Path(__file__).resolve().parent
MODEL_PATH = BASE_DIR / "models" / "model.joblib"


def train(model_path: Path = MODEL_PATH) -> None:
    labels = {label for _, label in TRAINING_SAMPLES}
    if labels != {"safe", "scam"}:
        raise ValueError("Training samples must contain both 'safe' and 'scam' labels.")
    if any(not text.strip() for text, _ in TRAINING_SAMPLES):
        raise ValueError("Training samples must not contain empty messages.")

    classifier = Pipeline(
        [
            (
                "tfidf",
                TfidfVectorizer(
                    ngram_range=(1, 2),
                    lowercase=True,
                    strip_accents="unicode",
                ),
            ),
            (
                "nb",
                MultinomialNB(alpha=0.5),
            ),
        ]
    )

    texts = [text for text, _ in TRAINING_SAMPLES]
    target_labels = [label for _, label in TRAINING_SAMPLES]

    classifier.fit(texts, target_labels)

    model_path.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(classifier, model_path)

    print(f"Model zapisany: {model_path}")


if __name__ == "__main__":
    train()