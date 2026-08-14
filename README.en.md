#AntiScam

AntiScam is a repository with two parts:

- Python/FastAPI engine for assessing the risk of phishing messages,
- C# ASP.NET Core WebAPI blog with HTML files and SQLite database.

The new blog is connected to the working folder, and the default SQLite database is created in `data/antiscam-blog.sqlite` (relative to the project directory).

## Wymagania

- Python 3.10+
- .NET SDK 8.0+
- beep

## Quick Start: C# WebAPI Blog
```powershell
dotnet restore AntiScamBlog.sln
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```
Once the application is launched, it is available at the address displayed by `dotnet run`, usually `http://localhost:5000` or `http://localhost:5080`.

### HTML Frontend

Open in browser:
```text
/
```
The website loads entries from the API and allows you to add a new blog entry.

Przed publikacją C# WebAPI analizuje tytuł, streszczenie, treść i autora.

The entry will only be saved in `LOW RISK` status; for `MEDIUM RISK` or `HIGH RISK` the API returns `422 Unprocessable Entity` and does not write an entry to SQLite.

### Opcjonalna baza NoSQL (MongoDB)

SQLite is still the default blog post database.

The optional MongoDB database only stores tickets rejected by risk analysis, so it does not change the operation of the existing SQLite store.

MongoDB jest włączony przez `NoSql:Enabled`.

The server address can be specified in `NoSql:ConnectionString` or via the `ANTISCAM_MONGO_CONNECTION_STRING` environment variable.

The default database and collection are `antiscam` and `blocked-submissions`.

The current configuration of both storages is returned by `GET /api/storage`.

Odrzucone zgłoszenia można odczytać przez `GET /api/incidents?limit=50`.

After enabling MongoDB, the endpoint returns entries from the `blocked-submissions` collection, newest first.

If the database is disabled or unavailable, the response is an empty list.

### Endpointy bloga
```text
GET    /api/health
GET    /api/storage
GET    /api/incidents?limit=50
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
The Python API will be available at `http://localhost:8000`.

### Python API endpoints
```text
GET  /
POST /scan
POST /ai/explain
```
Przykład:
```powershell
curl -Method POST http://localhost:8000/scan `
  -ContentType "application/json" `
  -Body '{"text":"Wyślij BLIK 123456 natychmiast!"}'
```
Endpoint `/ai/explain` pokazuje praktycznie, co ułatwia AI/NLP w projekcie: rozpoznaje intencję użytkownika, ton emocjonalny, ważne terminy, nazwy własne, podobieństwo do wzorca oszustwa i sugeruje bezpieczne następne działanie.
```powershell
curl -Method POST http://localhost:8000/ai/explain `
  -ContentType "application/json" `
  -Body '{"text":"Boję się, Bank Polska chce kod BLIK 123456 pilnie"}'
```
## Testy

Testy C#:
```powershell
dotnet test AntiScamBlog.sln
```
Python tests:
```powershell
pytest
```
The C# project includes unit tests for validation and minions, and integration tests for API, SQLite, and static HTML.

It also includes tests to block the publication of entries that are at risk of phishing or fraud.

Also includes cryptography tests: PBKDF2-HMAC-SHA256 and AES-GCM-256.

## Syllabus compliance

**The project fully meets the requirements of the syllabuses of three subjects: Fundamentals of computer security, Security of computer systems and IT security.**

The `antiscam` folder contains the implementation of all required learning outcomes, and the materials required for project assessment are located in:

- `SYLLABUS_MAPPING.md` - mapping learning outcomes to code and documentation,
- `docs/ai_syllabus_mapping.md` - syllabus mapping from the `AI_antiscam` folder,
- `docs/project_report.md` - project report,
- `docs/ai_project_report.md` - AI/NLP extension report,
- `docs/security_audit.md` - security overview and checklist,
- `docs/cryptography.md` - description of hashing, encryption and key management,
- `docs/ai_ethics.md` - AI ethics and artificial empathy,
- `docs/demo.md` - demonstration scenario,
- `docs/presentation_outline.md` - presentation outline,
- `docs/labs/` - safety laboratory instructions,
- `docs/ai_labs/` - AI/NLP lab manuals.

## Structure
```text
antiscam/                                  Pythonowy silnik AntiScam
antiscam/ai.py                             Edukacyjne komponenty AI/NLP
tests/                                     Testy Python
src/AntiScam.Blog.Api/                    C# ASP.NET Core Blog WebAPI
src/AntiScam.Blog.Api/wwwroot/            Pliki HTML, CSS i JS
tests/AntiScam.Blog.Api.Tests/            Testy jednostkowe i integracyjne C#
docs/                                     Raport, audyt, demo i laboratoria
docs/ai_labs/                              Laboratoria dla sylabusów AI_antiscam
SYLLABUS_MAPPING.md                       Mapowanie projektu na sylabusy
AntiScamBlog.sln                          Rozwiązanie .NET
README.md                                 Dokumentacja PL
README.en.md                              Dokumentacja EN
```
## C# Configuration WebAPI Blog

The default settings are in `src/AntiScam.Blog.Api/appsettings.json`:
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
For tests or local experiments, you can overwrite the database path with an environment variable:
```powershell
$env:ANTISCAM_BLOG_DB="C:\temp\antiscam-blog.sqlite"
```
### Konta, administracja i bezpieczne kopie

`POST /api/auth/register` rejestruje użytkownika (`userName`, `password`); pierwsze konto otrzymuje rolę `Admin`, kolejne rolę `User`.

Login via `POST /api/auth/login` returns the Bearer token.

Passwords are only saved as PBKDF2-HMAC-SHA256.

The administrator passes the token in the `Authorization: Bearer <token>` header and can:

- block account: `POST /api/admin/users/{id}/block`;
- see also inactive posts: `GET /api/admin/posts`;
- hide entry (soft deletion): `POST /api/admin/posts/{id}/deactivate` or `DELETE /api/posts/{id}`;
- restore entry: `POST /api/admin/posts/{id}/restore`.

When logging in from an IP address different from the previous session, the application automatically creates an encrypted copy of the database if its content has changed.

Ustaw sekret poza repozytorium:
```powershell
$env:ANTISCAM_BACKUP_KEY = "własny-długi-losowy-sekret"
```
The backup and metadata are saved in `secure_backups/backup.enc.json` and `secure_backups/backup_meta.json`.

AES-GCM-256 with a random nonce and an integrity tag is used; the source database is not saved in an explicit form.

Without the key, the backup is intentionally skipped and a warning entry is written to the log.

Backup takes a consistent SQLite snapshot through the `BackupDatabase` mechanism, so it covers all tables (entries, comments, users and sessions), not just the main database file.

If `ANTISCAM_BACKUP_KEY` is not set, the application creates a local secret in `data/antiscam-backup.key` once; the file and directory with copies are excluded from Git.

### HTTPS on the local network (OpenSSL)

The `tools/generate-https-certificate.ps1` script creates a local CA and a PFX certificate with a private IP address in the SAN, analogous to the reference project.

After installing OpenSSL, run:
```powershell
$env:ANTISCAM_HTTPS_CERT_PASSWORD = "silne-lokalne-haslo"
.\tools\generate-https-certificate.ps1 -PrivateIp "192.168.1.22"
```
Następnie ustaw `Https:Enabled` na `true` i uruchom aplikację.

Kestrel will listen on `0.0.0.0:5001` and the application will be accessible from the LAN as `https://192.168.1.22:5001`.

To remove the browser warning on devices on your network, trust the `certs/antiscam-ca.crt` file.

Without a certificate, a simple `dotnet run --project .\src\AntiScam.Blog.Api` listens on all interfaces on port 5000.

For a computer with the address `192.168.1.22`, then use `http://192.168.1.22:5000` from a device on the same network.

If the connection from another device is blocked, allow the .NET application to access incoming traffic in Windows Firewall for Private Networks.

## Updated the English version of the README

To refresh `README.en.md` based on the Polish `README.md`, run:
```powershell
.\tools\sync-readme-en.ps1
```
The script uses `deep-translator` to translate the documentation content and writes the result to `README.en.md`.

## GitHub

The repository is configured with origin:
```text
https://github.com/Kondexor2000/antiscam.git
```
Recommended flow after changes:
```powershell
git status
git add .
git commit -m "Add C# blog WebAPI with SQLite"
git push origin main
```
## License

The project is available under the MIT license.