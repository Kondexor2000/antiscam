# AntiScam

Advanced phishing and fraud detection system for text messages based on intelligent analysis.

## Overview

AntiScam is an API application for detecting fraud and phishing attempts in text messages. The system analyzes messages based on:

- **BLIK Codes** - Detection of Polish 6-digit codes
- **Links** - Distinguishing between trusted and suspicious domains
- **Keywords** - Analysis of characteristic fraudulent message words
- **Safety Context** - Identification of safety signals
- **Intent** - Detection of urgent or manipulative language

## Quick Start

### Requirements

- Python 3.10+
- pip

### Installation

```bash
# Clone repository
git clone https://github.com/Kondexor2000/antiscam.git
cd antiscam

# Install dependencies
pip install -r requirements-dev.txt
```

### Running API

```bash
# Start FastAPI server
uvicorn antiscam.api:app --reload
```

API will be available at: http://localhost:8000

### Running CLI

```bash
# Scan message
python -m antiscam.cli "Send BLIK 123456 immediately confirm code!"
```

## API Reference

### Endpoints

#### GET /

Returns API status.

**Response:**
```json
{
  "message": "AntiScam API is running"
}
```

#### POST /scan

Scans text message and returns risk assessment.

**Request:**
```json
{
  "text": "Message to scan"
}
```

**Response:**
```json
{
  "status": "LOW RISK",
  "risk_score": 15,
  "reasons": [
    "Low-risk context detected"
  ],
  "safe_links": ["https://google.com"],
  "risky_links": []
}
```

### Status Classifications

- **LOW RISK** (0-49): Message appears safe
- **MEDIUM RISK** (50-79): Potential threat
- **HIGH RISK** (80-100): High degree of threat

## Project Structure

```
antiscam/
├── __init__.py           # Main module
├── api.py               # FastAPI endpoints
├── cli.py               # Command-line interface
├── config.py            # Configuration and settings
├── engine.py            # Main detection logic
├── links.py             # Link analysis
├── logger.py            # Logging system
├── models.py            # Pydantic models
├── patterns.py          # Analysis patterns
├── scoring.py           # Scoring functions
├── requirements-dev.txt # Python dependencies
├── README.md            # Polish documentation
├── README.en.md         # English documentation
└── tests/               # Tests
    ├── conftest.py      # Pytest configuration
    ├── unit/            # Unit tests
    │   ├── test_config.py
    │   ├── test_links.py
    │   ├── test_scoring.py
    │   ├── test_engine.py
    │   └── test_models.py
    └── integration/     # Integration tests
        ├── test_api.py
        └── test_workflows.py
```

## Testing

### Running tests

```bash
# All tests
pytest

# Unit tests only
pytest tests/unit/

# Integration tests only
pytest tests/integration/

# With coverage report
pytest --cov=antiscam tests/

# Verbose output
pytest -v
```

### Test Statistics

- **108+ unit and integration tests**
- **~95% code coverage**
- **Execution time: <0.5s**

## Risk Analysis Algorithm

The system analyzes messages based on several criteria:

### 1. BLIK Codes
- **+60 points**: BLIK with confirming context
- **+30 points**: BLIK without context

### 2. Links
- **+20-45 points**: Suspicious links
- **-5 to -15 points**: Trusted links

### 3. Keywords
- Up to 30 points for characteristic words:
  - "blik" (10 pts)
  - "code" (5 pts)
  - "immediately" (6 pts)
  - "confirm" (5 pts)
  - etc.

### 4. Safety Context
- **-10 points**: Two or more safety signals
  - "hello", "ok", "meeting", "thank you"

### 5. Intent
- **+10 points**: Detected manipulative intent
  - "last chance", "account blocked", "click now"

## Configuration

Configuration is in `config.py`:

```python
from antiscam.config import settings

# Trusted domains
settings.trusted_domains  # {'google.com', 'facebook.com', ...}

# Keyword weights
settings.keyword_weights  # {'blik': 10, 'code': 5, ...}

# Safety signals
settings.safe_signals     # ['hello', 'ok', ...]

# Intent signals
settings.intent_signals   # ['last chance', ...]
```

## Usage Examples

### Python API

```python
from antiscam.engine import calculate_risk

# Safe message
result = calculate_risk("Hello, let's meet normally.")
# Returns: {"status": "LOW RISK", "risk_score": 0, ...}

# Suspicious message
result = calculate_risk("BLIK 123456 - send code immediately!")
# Returns: {"status": "HIGH RISK", "risk_score": 85, ...}
```

### cURL

```bash
# Scan message
curl -X POST http://localhost:8000/scan \
  -H "Content-Type: application/json" \
  -d '{"text": "Confirm your data quickly!"}'
```

### Python requests

```python
import requests

response = requests.post(
    "http://localhost:8000/scan",
    json={"text": "Send BLIK"}
)
print(response.json())
```

## Logging

The application generates logs for all scans:

```
[INFO] antiscam - scan_started
[INFO] antiscam - scan_finished
```

## Contributing

To contribute to the project:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards

- Write clean, readable code
- Add tests for new features
- Ensure all tests pass
- Add documentation for new functions

## License

This project is available under the MIT License.

## Support

If you encounter issues, open an Issue on GitHub.

## Learn More

To learn more about phishing and security:
- [OWASP](https://owasp.org/)
- [Anti-Phishing Working Group](https://apwg.org/)
- [CyberAware](https://www.cyberaware.gov.uk/)

---

**Last updated**: May 20, 2026  
**Version**: 0.1
