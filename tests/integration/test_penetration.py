"""Penetration-style tests for adversarial user input."""

import pytest
from fastapi.testclient import TestClient

from antiscam.api import app


@pytest.fixture
def client():
    """Create a test client for the API."""
    return TestClient(app)


class TestPenetrationScenarios:
    """Attack-style inputs that should be detected and handled safely."""

    @pytest.mark.parametrize(
        "payload",
        [
            "B L I K 123456 natychmiast",
            "B-L-I-K 654321 kod przelew",
            "Wyslij ВLΙΚ 123456 po potwierdzeniu",
        ],
    )
    def test_obfuscated_blik_payloads_are_flagged(self, client, payload):
        """Obfuscated BLIK requests should still be recognized as risky."""
        result = client.post("/scan", json={"text": payload}).json()

        assert result["status"] in {"MEDIUM RISK", "HIGH RISK"}
        assert result["risk_score"] >= 50
        assert any("BLIK" in reason for reason in result["reasons"])

    @pytest.mark.parametrize(
        "payload, expected_url",
        [
            ("Kliknij https://g00gle.com/login i potwierdz dane", "https://g00gle.com/login"),
            ("https://google.com.evil.example/login", "https://google.com.evil.example/login"),
            ("Kliknij https://login.paypal.com.evil.example/account", "https://login.paypal.com.evil.example/account"),
        ],
    )
    def test_phishing_links_and_typosquatting_are_detected(self, client, payload, expected_url):
        """Malicious domains and lookalikes must be treated as risky."""
        result = client.post("/scan", json={"text": payload}).json()

        assert result["risky_links"]
        assert expected_url in result["risky_links"]
        assert result["risk_score"] >= 50

    def test_html_script_payload_does_not_crash_api(self, client):
        """HTML/JS payloads should be safely processed and scored."""
        payload = "<script>alert('xss')</script> Click https://g00gle.com/login and send BLIK 123456"

        response = client.post("/scan", json={"text": payload})

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "HIGH RISK"
        assert data["risk_score"] >= 80
        assert data["risky_links"] == ["https://g00gle.com/login"]

    def test_repeated_attack_payloads_keep_request_isolation(self, client):
        """Repeated malicious payloads must not leak state between requests."""
        payloads = [
            "Pilnie kliknij https://g00gle.com/login",
            "BLIK 123456 natychmiast",
            "https://google.com.evil.example/login",
        ]

        responses = [client.post("/scan", json={"text": payload}) for payload in payloads]

        assert all(response.status_code == 200 for response in responses)
        assert [response.json()["status"] for response in responses] == [
            "HIGH RISK",
            "HIGH RISK",
            "MEDIUM RISK",
        ]
