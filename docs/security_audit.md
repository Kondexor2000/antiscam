# Przeglad bezpieczenstwa

## Zakres

Przeglad obejmuje:

- Pythonowy silnik `antiscam/`,
- C# WebAPI `src/AntiScam.Blog.Api/`,
- frontend HTML/CSS/JS w `wwwroot`,
- testy jednostkowe i integracyjne,
- dokumentacje i scenariusze demonstracyjne.

## Checklista

| Obszar | Status | Dowod |
| --- | --- | --- |
| Walidacja danych wejsciowych | OK | `BlogPostValidator`, modele Pydantic |
| Blokada tresci ryzykownych | OK | `RiskAnalyzer`, test `CreatePost_DoesNotPersistPostWhenRiskIsDetected` |
| SQL injection | OK | Parametry `SqlCommand.Parameters.AddWithValue` |
| XSS w wyswietlaniu wpisow | OK | `escapeHtml` w `wwwroot/app.js` |
| Sekrety w repozytorium | OK | Brak kluczy; dokument `docs/cryptography.md` opisuje zmienne srodowiskowe |
| Kryptografia | OK | PBKDF2-HMAC-SHA256 i AES-GCM-256 z testami |
| Testy regresyjne | OK | `dotnet test AntiScamBlog.sln`, `python -m pytest` |
| Artefakty builda i baza runtime | OK | `.gitignore` ignoruje `bin/`, `obj/`, pliki SQL Server runtime |

## Ryzyka szczatkowe

- Scoring phishingu jest heurystyczny i moze dawac falszywe alarmy.
- Projekt nie korzysta jeszcze z zewnetrznej reputacji domen.
- Brak uwierzytelniania uzytkownikow bloga; w obecnym zakresie jest to demonstracja lokalna.
- Wyniki skanowania zależności i Bandit są generowane przez CI; wymagają przeglądu
  artefaktu `security-report` dla konkretnego uruchomienia.

## Rekomendacje

1. Przeglądać raport `security-report` dla każdego uruchomienia CI i usuwać
   wykryte podatności zależności oraz problemy statycznej analizy.
2. Dodac uwierzytelnianie oraz role dla panelu publikacji.
3. Dodac automatyczne skanowanie zaleznosci.
4. Rozszerzyc reputacje domen o aktualizowana liste z zaufanego zrodla.

## Wynik

Po aktualizacji projekt spelnia wymagania audytowe na poziomie projektu dydaktycznego: dokumentuje ryzyka, pokazuje mitigacje i ma testy potwierdzajace najwazniejsze mechanizmy obronne.

## Automatyczny raport bezpieczeństwa

Workflow `.github/workflows/quality-evidence.yml` zapisuje wyniki `pip-audit`,
`dotnet list package --vulnerable --include-transitive` i Bandit do artefaktu
`security-report`. Raport stanowi zapis wyników konkretnego commita; tabela
checklisty powyżej opisuje istniejące mechanizmy i testy projektu, a nie wynik
bieżącego skanowania podatności. Należy sprawdzać status i surowe raporty
narzędzi w każdym przebiegu CI.

Lokalny smoke test API z 2026-10-31 wykonał 10 scenariuszy wejść adversarialnych
na izolowanym Docker Compose; wszystkie zakończyły się PASS. Szczegóły są w
`docs/evidence/security-local-smoke.md`. Wynik ten obejmuje klasyfikację wejść i
izolację żądań; nie zastępuje audytu zależności ani raportów Bandit w CI.
