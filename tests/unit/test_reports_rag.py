import json

import pytest

from antiscam.reports_rag import OllamaClient, ReportIndexNotFound, ReportRAG


class FakeOllamaClient:
    embedding_model = "test-embedding-model"

    def embed(self, texts):
        return [
            [
                float("phishing" in text.lower()),
                float("spam" in text.lower()),
                float("safety" in text.lower()),
            ]
            for text in texts
        ]

    def generate(self, question, context):
        return f"Answer about {question}: {context}"


def test_report_index_and_vector_search_returns_relevant_source(tmp_path):
    reports_dir = tmp_path / "reports"
    reports_dir.mkdir()
    (reports_dir / "phishing.md").write_text(
        "Phishing reports describe fraudulent links.", encoding="utf-8"
    )
    (reports_dir / "safety.txt").write_text(
        "Safety reports recommend independent verification.", encoding="utf-8"
    )
    rag = ReportRAG(
        reports_dir=reports_dir,
        index_path=tmp_path / "index.sqlite",
        client=FakeOllamaClient(),
    )

    indexed = rag.index_reports()
    results = rag.search("phishing", top_k=1)

    assert indexed["indexed_chunks"] == 2
    assert results[0].source == "phishing.md"
    assert results[0].similarity == 1


def test_report_answer_includes_retrieved_citations(tmp_path):
    reports_dir = tmp_path / "reports"
    reports_dir.mkdir()
    (reports_dir / "dataset.csv").write_text(
        "dataset,description\nsms,spam examples\n", encoding="utf-8"
    )
    rag = ReportRAG(
        reports_dir=reports_dir,
        index_path=tmp_path / "index.sqlite",
        client=FakeOllamaClient(),
    )
    rag.index_reports()

    answer = rag.ask("spam")

    assert answer["question"] == "spam"
    assert "dataset.csv#row-2" in answer["answer"]
    assert answer["sources"][0]["source"] == "dataset.csv#row-2"


def test_search_requires_an_index(tmp_path):
    reports_dir = tmp_path / "reports"
    reports_dir.mkdir()
    rag = ReportRAG(
        reports_dir=reports_dir,
        index_path=tmp_path / "index.sqlite",
        client=FakeOllamaClient(),
    )

    with pytest.raises(ReportIndexNotFound, match="Run POST /reports/index first"):
        rag.search("phishing")


def test_ollama_client_uses_embed_and_chat_api(monkeypatch):
    requests = []
    responses = [
        {"embeddings": [[0.1, 0.9]]},
        {"message": {"content": "Odpowiedz z raportu."}},
    ]

    class FakeResponse:
        def __init__(self, payload):
            self.payload = payload

        def __enter__(self):
            return self

        def __exit__(self, exception_type, exception, traceback):
            return False

        def read(self):
            return json.dumps(self.payload).encode("utf-8")

    def fake_urlopen(request, timeout):
        requests.append((request, timeout))
        return FakeResponse(responses.pop(0))

    monkeypatch.setattr("antiscam.reports_rag.urllib.request.urlopen", fake_urlopen)
    client = OllamaClient()

    assert client.embed(["tekst raportu"]) == [[0.1, 0.9]]
    assert client.generate("pytanie", "kontekst") == "Odpowiedz z raportu."
    assert requests[0][0].full_url.endswith("/api/embed")
    assert json.loads(requests[0][0].data)["input"] == ["tekst raportu"]
    assert requests[1][0].full_url.endswith("/api/chat")
    chat_payload = json.loads(requests[1][0].data)
    assert chat_payload["stream"] is False
    assert chat_payload["think"] is False
    assert chat_payload["options"]["num_predict"] == 256
