import re
from typing import Dict
from .logger import get_logger
from .config import settings
from .links import analyze_links
from .normalization import deobfuscate_text
from .scoring import score_keywords, score_safe_context, score_intent, score_blik

logger = get_logger()


def detect_blik(text: str) -> list[str]:
    return re.findall(settings.blik_pattern, text)


def calculate_risk(text: str) -> Dict:
    logger.info("scan_started")

    risk_score = 0
    reasons = []

    normalized_text = deobfuscate_text(text)
    text_low = normalized_text.lower()

    # BLIK
    numbers = detect_blik(normalized_text)
    s, r = score_blik(numbers, text_low)
    risk_score += s
    if r:
        reasons.append(r)

    # LINKS
    safe_links, risky_links = analyze_links(text)

    if risky_links:
        risk_score += min(45, 20 + len(risky_links) * 10)
        reasons.append(f"Risky links: {risky_links}")

    if safe_links:
        risk_score -= min(15, len(safe_links) * 5)
        reasons.append(f"Trusted links: {safe_links}")

    # KEYWORDS
    kw_score, kw_reason = score_keywords(text_low)
    risk_score += kw_score
    if kw_reason:
        reasons.append(kw_reason)

    # SAFE CONTEXT
    safe_score, safe_reason = score_safe_context(text_low)
    risk_score += safe_score
    if safe_reason:
        reasons.append(safe_reason)

    # INTENT
    intent_score, intent_reason = score_intent(text_low)
    risk_score += intent_score
    if intent_reason:
        reasons.append(intent_reason)

    # NORMALIZATION
    risk_score = max(0, min(int(risk_score), 100))

    # CLASSIFICATION
    if risk_score >= 80:
        status = "HIGH RISK"
    elif risk_score >= 50:
        status = "MEDIUM RISK"
    else:
        status = "LOW RISK"

    logger.info("scan_finished")

    return {
        "status": status,
        "risk_score": risk_score,
        "reasons": reasons,
        "safe_links": safe_links,
        "risky_links": risky_links
    }
