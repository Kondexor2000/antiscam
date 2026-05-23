# AntiScam

AntiScam to repozytorium z dwoma częściami:

- silnikiem Python/FastAPI do oceny ryzyka wiadomości phishingowych,
- blogiem C# ASP.NET Core WebAPI z plikami HTML i bazą SQLite.

Nowy blog jest połączony z folderem roboczym `C:\Users\kondz\antiscam`, a domyślna baza SQLite powstaje w `C:\Users\kondz\antiscam\data\antiscam-blog.sqlite`.

## Wymagania

- Python 3.10+
- .NET SDK 8.0+
- pip

## Szybki start: C# Blog WebAPI

```powershell
dotnet restore AntiScamBlog.sln
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```

Po uruchomieniu aplikacja jest dostępna pod adresem wyświetlonym przez `dotnet run`, zwykle `http://localhost:5000` albo `http://localhost:5080`.

### Frontend HTML

Otwórz w przeglądarce:

```text
/
```

Strona ładuje wpisy z API i pozwala dodać nowy wpis blogowy.

Przed publikacją C# WebAPI analizuje tytuł, streszczenie, treść i autora. Wpis zostanie zapisany tylko przy statusie `LOW RISK`; dla `MEDIUM RISK` lub `HIGH RISK` API zwraca `422 Unprocessable Entity` i nie zapisuje wpisu w SQLite.

### Endpointy bloga

```text
GET    /api/health
GET    /api/workspace
GET    /api/posts
GET    /api/posts/{slug}
POST   /api/posts
PUT    /api/posts/{id}
DELETE /api/posts/{id}
```

Przykład dodania wpisu:

```powershell
curl -Method POST http://localhost:5000/api/posts `
  -ContentType "application/json" `
  -Body '{"title":"Alarm phishingowy","summary":"Krótki opis","content":"Treść wpisu","author":"AntiScam Team"}'
```

## Szybki start: Python AntiScam API

```powershell
pip install -r requirements-dev.txt
pip install -e .
uvicorn antiscam.api:app --reload
```

API Pythonowe będzie dostępne pod adresem `http://localhost:8000`.

### Endpointy Python API

```text
GET  /
POST /scan
```

Przykład:

```powershell
curl -Method POST http://localhost:8000/scan `
  -ContentType "application/json" `
  -Body '{"text":"Wyślij BLIK 123456 natychmiast!"}'
```

## Testy

Testy C#:

```powershell
dotnet test AntiScamBlog.sln
```

Testy Python:

```powershell
pytest
```

Projekt C# zawiera testy jednostkowe dla walidacji i slugów oraz testy integracyjne API, SQLite i statycznej strony HTML.
Obejmuje też testy blokowania publikacji wpisów, w których wykryto ryzyko phishingu lub oszustwa.
Obejmuje również testy kryptografii: PBKDF2-HMAC-SHA256 i AES-GCM-256.

## Zgodność z sylabusami

Materiały wymagane do oceny projektu znajdują się w:

- `SYLLABUS_MAPPING.md` - mapowanie efektów uczenia się na kod i dokumentację,
- `docs/project_report.md` - raport projektowy,
- `docs/security_audit.md` - przegląd bezpieczeństwa i checklista,
- `docs/cryptography.md` - opis hashowania, szyfrowania i zarządzania kluczami,
- `docs/demo.md` - scenariusz demonstracji,
- `docs/presentation_outline.md` - konspekt prezentacji,
- `docs/labs/` - instrukcje laboratoryjne.

## Struktura

```text
antiscam/                                  Pythonowy silnik AntiScam
tests/                                     Testy Python
src/AntiScam.Blog.Api/                    C# ASP.NET Core Blog WebAPI
src/AntiScam.Blog.Api/wwwroot/            Pliki HTML, CSS i JS
tests/AntiScam.Blog.Api.Tests/            Testy jednostkowe i integracyjne C#
docs/                                     Raport, audyt, demo i laboratoria
SYLLABUS_MAPPING.md                       Mapowanie projektu na sylabusy
AntiScamBlog.sln                          Rozwiązanie .NET
README.md                                 Dokumentacja PL
README.en.md                              Dokumentacja EN
```

## Konfiguracja C# Blog WebAPI

Domyślne ustawienia są w `src/AntiScam.Blog.Api/appsettings.json`:

```json
{
  "Workspace": {
    "RootPath": "C:\\Users\\kondz\\antiscam"
  },
  "Blog": {
    "DatabasePath": "C:\\Users\\kondz\\antiscam\\data\\antiscam-blog.sqlite"
  }
}
```

Do testów lub lokalnych eksperymentów można nadpisać ścieżkę bazy zmienną środowiskową:

```powershell
$env:ANTISCAM_BLOG_DB="C:\temp\antiscam-blog.sqlite"
```

## GitHub

Repozytorium jest skonfigurowane z origin:

```text
https://github.com/Kondexor2000/antiscam.git
```

Zalecany przepływ po zmianach:

```powershell
git status
git add .
git commit -m "Add C# blog WebAPI with SQLite"
git push origin main
```

## Licencja

Projekt jest dostępny na licencji MIT.
