from __future__ import annotations

from dataclasses import dataclass

from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.naive_bayes import MultinomialNB
from sklearn.pipeline import Pipeline


TRAINING_SAMPLES: tuple[tuple[str, str], ...] = (
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


@dataclass(frozen=True)
class MlRiskAssessment:
    label: str
    scam_probability: float
    score: int


def _build_classifier() -> Pipeline:
    classifier = Pipeline(
        [
            ("tfidf", TfidfVectorizer(ngram_range=(1, 2), lowercase=True)),
            ("nb", MultinomialNB(alpha=0.5)),
        ]
    )
    texts = [text for text, _label in TRAINING_SAMPLES]
    labels = [label for _text, label in TRAINING_SAMPLES]
    classifier.fit(texts, labels)
    return classifier


_CLASSIFIER = _build_classifier()


def classify_message(text: str) -> MlRiskAssessment:
    if not text.strip():
        return MlRiskAssessment(label="safe", scam_probability=0.0, score=0)

    probabilities = _CLASSIFIER.predict_proba([text])[0]
    classes = list(_CLASSIFIER.classes_)
    scam_probability = float(probabilities[classes.index("scam")])
    label = "scam" if scam_probability >= 0.5 else "safe"
    return MlRiskAssessment(
        label=label,
        scam_probability=scam_probability,
        score=round(scam_probability * 100),
    )
