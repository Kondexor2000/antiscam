"""Exploratory analysis for CSV datasets stored in the project's data directory.

Run from the project root with: python tools/analyze_datasets.py
Requires: pip install -e ".[analysis]"
"""

from __future__ import annotations

import argparse
from pathlib import Path

import pandas as pd


ROOT = Path(__file__).resolve().parents[1]
DATA_DIR = ROOT / "data"
OUTPUT_DIR = ROOT / "reports" / "data-analysis"
TARGET_COLUMNS = ("label", "result", "class", "target")
CONTENT_COLUMNS = ("text", "message", "sms", "url")


def analyze_dataset(csv_path: Path) -> tuple[dict[str, object], pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    frame = pd.read_csv(csv_path, low_memory=False)
    row_count = len(frame)
    missing_counts = frame.isna().sum()
    missing_percentage = missing_counts / row_count * 100 if row_count else missing_counts * 0

    column_names = {str(column).casefold(): column for column in frame.columns}
    target_column = next(
        (column_names[name] for name in TARGET_COLUMNS if name in column_names),
        None,
    )
    content_column = next(
        (column_names[name] for name in CONTENT_COLUMNS if name in column_names),
        None,
    )

    content_lengths = (
        frame[content_column].astype("string").fillna("").str.len()
        if content_column is not None
        else None
    )
    summary: dict[str, object] = {
        "dataset": csv_path.name,
        "rows": row_count,
        "columns": len(frame.columns),
        "duplicate_rows": int(frame.duplicated().sum()),
        "missing_cells": int(missing_counts.sum()),
        "missing_percentage": round(
            float(missing_counts.sum() / (row_count * len(frame.columns)) * 100),
            2,
        )
        if row_count and len(frame.columns)
        else 0.0,
        "target_column": str(target_column) if target_column is not None else "",
        "content_column": str(content_column) if content_column is not None else "",
        "content_length_mean": round(float(content_lengths.mean()), 2)
        if content_lengths is not None and row_count
        else None,
        "content_length_median": round(float(content_lengths.median()), 2)
        if content_lengths is not None and row_count
        else None,
        "content_length_max": int(content_lengths.max())
        if content_lengths is not None and row_count
        else None,
    }

    column_profile = pd.DataFrame(
        {
            "dataset": csv_path.name,
            "column": [str(column) for column in frame.columns],
            "dtype": [str(frame[column].dtype) for column in frame.columns],
            "non_null_count": [int(frame[column].notna().sum()) for column in frame.columns],
            "missing_count": [int(missing_counts[column]) for column in frame.columns],
            "missing_percentage": [
                round(float(missing_percentage[column]), 2) for column in frame.columns
            ],
            "unique_values": [int(frame[column].nunique(dropna=True)) for column in frame.columns],
        }
    )

    numeric = frame.select_dtypes(include="number")
    if numeric.empty:
        numeric_summary = pd.DataFrame(
            columns=["dataset", "feature", "count", "mean", "std", "min", "25%", "50%", "75%", "max"]
        )
    else:
        numeric_summary = numeric.describe().T
        numeric_summary.index.name = "feature"
        numeric_summary = numeric_summary.reset_index()
        numeric_summary.insert(0, "dataset", csv_path.name)

    if target_column is None:
        class_distribution = pd.DataFrame(
            columns=["dataset", "target", "class", "count", "percentage"]
        )
    else:
        class_values = frame[target_column].astype("string").fillna("<missing>")
        counts = class_values.value_counts(dropna=False)
        class_distribution = pd.DataFrame(
            {
                "dataset": csv_path.name,
                "target": str(target_column),
                "class": counts.index.astype(str),
                "count": counts.to_numpy(),
                "percentage": (counts.to_numpy() / row_count * 100).round(2)
                if row_count
                else [],
            }
        )

    return summary, column_profile, numeric_summary, class_distribution


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Analyze CSV datasets and save exploratory summary tables."
    )
    parser.add_argument(
        "--data-dir",
        type=Path,
        default=DATA_DIR,
        help="Folder containing CSV files (default: %(default)s)",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=OUTPUT_DIR,
        help="Folder for analysis CSV reports (default: %(default)s)",
    )
    args = parser.parse_args()

    csv_files = sorted(args.data_dir.glob("*.csv"))
    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in {args.data_dir}")

    results = [analyze_dataset(csv_path) for csv_path in csv_files]
    summaries = pd.DataFrame([result[0] for result in results])
    column_profiles = pd.concat([result[1] for result in results], ignore_index=True)
    numeric_summaries = pd.concat([result[2] for result in results], ignore_index=True)
    class_distributions = pd.concat([result[3] for result in results], ignore_index=True)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    reports = {
        "dataset_summary.csv": summaries,
        "column_profile.csv": column_profiles,
        "numeric_summary.csv": numeric_summaries,
        "class_distribution.csv": class_distributions,
    }
    for filename, report in reports.items():
        report.to_csv(args.output_dir / filename, index=False)

    print(summaries.to_string(index=False))
    print(f"\nReports saved to: {args.output_dir.resolve()}")


if __name__ == "__main__":
    main()
