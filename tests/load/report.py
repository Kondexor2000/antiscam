"""Convert k6 summary JSON files into a compact Markdown performance report."""
import json
import os
from datetime import datetime, timezone
from pathlib import Path


def metric(metrics, name, field):
    return metrics.get(name, {}).get("values", {}).get(field)


def fmt(value, suffix=""):
    return "n/a" if value is None else f"{value:.2f}{suffix}"


def main():
    output = ["# Raport wydajności k6", "", f"Wygenerowano: {datetime.now(timezone.utc).isoformat()}", "",
              "Scenariusze trwały 100 s: maksymalnie 20 req/s dla `/scan` i 10 req/s dla odczytu `/api/posts`. Pomiar wykonano lokalnie przez Docker Desktop; wyniki opisują to środowisko i nie są gwarancją pojemności produkcyjnej.", "",
              "| Usługa | p50 (ms) | p95 (ms) | p99 (ms) | throughput (req/s) | błędy |", "|---|---:|---:|---:|---:|---:|"]
    for name in ("scan", "blog"):
        path = Path("artifacts") / f"load-{name}.json"
        if not path.exists():
            continue
        metrics = json.loads(path.read_text(encoding="utf-8"))["metrics"]
        output.append("| {} | {} | {} | {} | {} | {} |".format(
            name,
            fmt(metric(metrics, "http_req_duration", "med")),
            fmt(metric(metrics, "http_req_duration", "p(95)")),
            fmt(metric(metrics, "http_req_duration", "p(99)")),
            fmt(metric(metrics, "http_reqs", "rate")),
            fmt((metric(metrics, "http_req_failed", "rate") or 0) * 100, "%"),
        ))
    Path(os.environ.get("LOAD_REPORT", "artifacts/load-report.md")).write_text("\n".join(output) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
