import re
from urllib.parse import urlparse
from .config import settings


def extract_links(text: str) -> list[str]:
    return re.findall(settings.url_pattern, text)


def extract_domain(url: str) -> str:
    try:
        parsed = urlparse(url)
        return parsed.netloc.lower().replace("www.", "")
    except Exception:
        return ""


def is_trusted_domain(domain: str) -> bool:
    return any(
        domain == td or domain.endswith("." + td)
        for td in settings.trusted_domains
    )


def analyze_links(text: str) -> tuple[list[str], list[str]]:
    links = extract_links(text)

    safe_links = []
    risky_links = []

    for link in links:
        domain = extract_domain(link)

        if is_trusted_domain(domain):
            safe_links.append(link)
        else:
            risky_links.append(link)

    return safe_links, risky_links