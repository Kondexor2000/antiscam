"""Download selected UCI AntiScam datasets as local CSV files.

Run with: python tools/fetch_datasets.py
Requires: pip install -e ".[datasets]"
"""

from __future__ import annotations

import io
import urllib.request
import zipfile
from pathlib import Path

import pandas as pd
from ucimlrepo import fetch_ucirepo


ROOT = Path(__file__).resolve().parents[1]
DATA_DIR = ROOT / "data"

TABULAR_DATASETS = {
    327: "uci_phishing_websites.csv",
    967: "uci_phiusiil_phishing_url.csv",
}
SMS_ARCHIVE_URL = "https://archive.ics.uci.edu/static/public/228/sms+spam+collection.zip"


def main() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)

    # UCI currently serves this classic corpus as a ZIP; retain its original
    # labels and message text while converting the tab-separated file to CSV.
    with urllib.request.urlopen(SMS_ARCHIVE_URL, timeout=60) as response:
        archive_bytes = response.read()
    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archive:
        sms = pd.read_csv(
            archive.open("SMSSpamCollection"),
            sep="\t",
            header=None,
            names=["label", "text"],
            encoding="utf-8",
        )
    sms_path = DATA_DIR / "uci_sms_spam_collection.csv"
    sms.to_csv(sms_path, index=False)
    print(f"Saved {len(sms):,} rows to {sms_path.relative_to(ROOT)}")

    for dataset_id, filename in TABULAR_DATASETS.items():
        dataset = fetch_ucirepo(id=dataset_id)
        features = dataset.data.features
        targets = dataset.data.targets
        if targets is None or targets.empty:
            raise RuntimeError(f"UCI dataset {dataset_id} does not provide target labels.")

        frame = pd.concat([features, targets], axis=1)
        destination = DATA_DIR / filename
        frame.to_csv(destination, index=False)
        print(f"Saved {len(frame):,} rows to {destination.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
