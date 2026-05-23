"""Educational AI/NLP helpers for the AntiScam project.

The module intentionally uses only the Python standard library so the examples
remain easy to inspect during labs. It is not meant to replace production ML
libraries; it provides small, deterministic building blocks for syllabus demos.
"""

from __future__ import annotations

import math
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from typing import Iterable


TOKEN_PATTERN = re.compile(r"[a-zA-ZąćęłńóśźżĄĆĘŁŃÓŚŹŻ0-9]+")
NAMED_ENTITY_PATTERN = re.compile(
    r"\b(?:[A-ZĄĆĘŁŃÓŚŹŻ][a-ząćęłńóśźżA-ZĄĆĘŁŃÓŚŹŻ]+)(?:\s+[A-ZĄĆĘŁŃÓŚŹŻ][a-ząćęłńóśźżA-ZĄĆĘŁŃÓŚŹŻ]+)*\b"
)


def tokenize(text: str) -> list[str]:
    """Lowercase and tokenize Polish/English text."""

    return [match.group(0).lower() for match in TOKEN_PATTERN.finditer(text)]


def bag_of_words(text: str) -> Counter[str]:
    return Counter(tokenize(text))


def cosine_similarity(left: Counter[str], right: Counter[str]) -> float:
    shared = set(left) & set(right)
    numerator = sum(left[token] * right[token] for token in shared)
    left_norm = math.sqrt(sum(value * value for value in left.values()))
    right_norm = math.sqrt(sum(value * value for value in right.values()))
    if left_norm == 0 or right_norm == 0:
        return 0.0
    return numerator / (left_norm * right_norm)


@dataclass(frozen=True)
class Classification:
    label: str
    scores: dict[str, float]


class NaiveBayesTextClassifier:
    """Tiny multinomial Naive Bayes classifier for lab-sized text datasets."""

    def __init__(self) -> None:
        self._label_counts: Counter[str] = Counter()
        self._token_counts: dict[str, Counter[str]] = defaultdict(Counter)
        self._total_tokens: Counter[str] = Counter()
        self._vocabulary: set[str] = set()

    def train(self, samples: Iterable[tuple[str, str]]) -> None:
        for text, label in samples:
            tokens = tokenize(text)
            self._label_counts[label] += 1
            self._token_counts[label].update(tokens)
            self._total_tokens[label] += len(tokens)
            self._vocabulary.update(tokens)

    def predict(self, text: str) -> Classification:
        if not self._label_counts:
            raise ValueError("Classifier has not been trained.")

        tokens = tokenize(text)
        total_documents = sum(self._label_counts.values())
        vocabulary_size = max(1, len(self._vocabulary))
        scores: dict[str, float] = {}

        for label, label_count in self._label_counts.items():
            score = math.log(label_count / total_documents)
            denominator = self._total_tokens[label] + vocabulary_size
            for token in tokens:
                score += math.log((self._token_counts[label][token] + 1) / denominator)
            scores[label] = score

        best_label = max(scores, key=scores.get)
        return Classification(best_label, scores)


class NGramLanguageModel:
    """Simple add-one smoothed n-gram language model."""

    def __init__(self, n: int = 2) -> None:
        if n < 1:
            raise ValueError("n must be at least 1.")
        self.n = n
        self._counts: Counter[tuple[str, ...]] = Counter()
        self._contexts: Counter[tuple[str, ...]] = Counter()
        self._vocabulary: set[str] = set()

    def train(self, texts: Iterable[str]) -> None:
        for text in texts:
            tokens = ["<s>"] * (self.n - 1) + tokenize(text) + ["</s>"]
            self._vocabulary.update(tokens)
            for index in range(len(tokens) - self.n + 1):
                ngram = tuple(tokens[index : index + self.n])
                context = ngram[:-1]
                self._counts[ngram] += 1
                self._contexts[context] += 1

    def probability(self, context: Iterable[str], token: str) -> float:
        context_tuple = tuple(context)[-(self.n - 1) :] if self.n > 1 else tuple()
        ngram = context_tuple + (token.lower(),)
        vocabulary_size = max(1, len(self._vocabulary))
        return (self._counts[ngram] + 1) / (self._contexts[context_tuple] + vocabulary_size)


def extract_terms(text: str, min_length: int = 4) -> list[str]:
    counts = Counter(token for token in tokenize(text) if len(token) >= min_length)
    return [term for term, _ in counts.most_common()]


def extract_named_entities(text: str) -> list[str]:
    return [match.group(0) for match in NAMED_ENTITY_PATTERN.finditer(text)]


class TranslationMemory:
    def __init__(self) -> None:
        self._entries: list[tuple[str, str, Counter[str]]] = []

    def add(self, source: str, target: str) -> None:
        self._entries.append((source, target, bag_of_words(source)))

    def suggest(self, source: str) -> tuple[str, float] | None:
        if not self._entries:
            return None
        vector = bag_of_words(source)
        source_text, target_text, score = max(
            (
                (entry_source, entry_target, cosine_similarity(vector, entry_vector))
                for entry_source, entry_target, entry_vector in self._entries
            ),
            key=lambda item: item[2],
        )
        _ = source_text
        return target_text, score


@dataclass(frozen=True)
class DialogResponse:
    intent: str
    message: str
    emotion: str


class AntiScamDialogBot:
    def respond(self, message: str) -> DialogResponse:
        text = message.lower()
        emotion = detect_emotion(message)

        if "blik" in text or "kod" in text:
            return DialogResponse(
                "report_scam",
                "Nie podawaj kodu. Zweryfikuj prosbe innym kanalem i zachowaj zrzut ekranu.",
                emotion,
            )
        if "link" in text or "http" in text:
            return DialogResponse(
                "check_link",
                "Sprawdz domene, literowki i kontekst. Nie loguj sie przez podejrzany link.",
                emotion,
            )
        return DialogResponse(
            "education",
            "Opisz wiadomosc, a pomoge ocenic ryzyko i dobrac bezpieczna reakcje.",
            emotion,
        )


def detect_emotion(text: str) -> str:
    lowered = text.lower()
    if any(word in lowered for word in ["boje", "boję", "strach", "panika", "pilnie"]):
        return "anxiety"
    if any(word in lowered for word in ["dziekuje", "dziękuję", "super", "ok"]):
        return "positive"
    if any(word in lowered for word in ["zly", "zły", "wkurzony", "oszust"]):
        return "anger"
    return "neutral"


class KnowledgeGraph:
    def __init__(self) -> None:
        self._edges: dict[str, list[tuple[str, str]]] = defaultdict(list)

    def add(self, subject: str, relation: str, obj: str) -> None:
        self._edges[subject].append((relation, obj))

    def facts_about(self, subject: str) -> list[tuple[str, str]]:
        return list(self._edges.get(subject, []))

    def objects(self, relation: str) -> list[str]:
        return [
            obj
            for edges in self._edges.values()
            for edge_relation, obj in edges
            if edge_relation == relation
        ]


def cloud_deployment_profile() -> dict[str, str]:
    return {
        "iaas": "VM or container host for the API and SQLite volume",
        "paas": "Managed App Service with environment-based configuration",
        "faas": "Serverless scan endpoint for bursty message checks",
        "saas": "Hosted AntiScam dashboard consumed by end users",
    }
