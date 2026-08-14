from pathlib import Path

from antiscam.readme_sync import translate_readme


class DummyTranslator:
    def translate(self, text):
        return text.replace("AntiScam", "AntiScam").replace("Polski", "English")


def test_translate_readme_uses_custom_translator(tmp_path):
    source = tmp_path / "README.md"
    target = tmp_path / "README.en.md"

    source.write_text("# Polski opis\n\nAntiScam jest dobrym projektem.\n", encoding="utf-8")

    result = translate_readme(
        source_path=source,
        target_path=target,
        source_lang="pl",
        target_lang="en",
        translator_factory=lambda: DummyTranslator(),
    )

    assert result == "# Polski opis\n\nAntiScam jest dobrym projektem.\n"
    assert target.read_text(encoding="utf-8") == "# Polski opis\n\nAntiScam jest dobrym projektem.\n"
