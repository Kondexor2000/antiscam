#AntiScam

AntiScam is a repository with two parts:

- Python/FastAPI engine for assessing the risk of phishing messages,
- C# ASP.NET Core WebAPI blog with HTML files and SQLite database.

The new blog is connected to the working folder, and the default SQLite database is created in `data/antiscam-blog.sqlite` (relative to the project directory).

## Wymagania

- Python 3.10+
- .NET SDK 8.0+
- beep
- OpenSSL (optional, for generating HTTPS certificates)

## Szybki start: C# Blog WebAPI
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

Before publication, C# WebAPI analyzes the title, abstract, content and author.

The entry will only be saved in `LOW RISK` status; for `MEDIUM RISK` or `HIGH RISK` the API returns `422 Unprocessable Entity` and does not write an entry to SQLite.

### Opcjonalna baza NoSQL (MongoDB)

SQLite is still the default blog post database.

Opcjonalna baza MongoDB przechowuje jedynie zgłoszenia odrzucone przez analizę ryzyka, więc nie zmienia działania istniejącego magazynu SQLite.

MongoDB is enabled by `NoSql:Enabled`.

Adres serwera można podać w `NoSql:ConnectionString` albo przez zmienną środowiskową `ANTISCAM_MONGO_CONNECTION_STRING`.

The default database and collection are `antiscam` and `blocked-submissions`.

The current configuration of both storages is returned by `GET /api/storage`.

Rejected reports can be read by `GET /api/incidents?limit=50`.

Po włączeniu MongoDB endpoint zwraca wpisy z kolekcji `blocked-submissions`, od najnowszych.

If the database is disabled or unavailable, the response is an empty list.

### Blog endpoints
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
### Training the ML model

The project includes a simple Machine Learning model (TF-IDF + Multinomial Naive Bayes) for news classification. To train a model:
```powershell
python train.py
```
The script trains on 16 training samples (8 phishing, 8 safe) and saves the model in `models/model.joblib`.

The model is then used by the Python API to analyze the risk of the message.

### Dokumentacja API (Swagger UI / OpenAPI)

Both applications provide interactive documentation:

**C# Blog WebAPI:**
- Swagger UI: `http://localhost:5000/swagger/ui`
- OpenAPI JSON: `http://localhost:5000/swagger/v1/swagger.json`

**Python AntiScam API:**
- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`
- OpenAPI JSON: `http://localhost:8000/openapi.json`

## Run both applications at the same time

Aby uruchomić projekt w pełni (blog + AI engine), otwórz dwa terminale i uruchom w każdym:

**Terminal 1 - C# Blog API:**
```powershell
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```
**Terminal 2 - Python AntiScam API:**
```powershell
pip install -r requirements-dev.txt
pip install -e .
uvicorn antiscam.api:app --reload
```
The blog will be available at `http://localhost:5000` and the Python API at `http://localhost:8000`.

## Tests

C# tests:
```powershell
dotnet test AntiScamBlog.sln
```
Python tests:
```powershell
pytest
```
The C# project includes unit tests for validation and minions, and integration tests for API, SQLite, and static HTML.

Obejmuje też testy blokowania publikacji wpisów, w których wykryto ryzyko phishingu lub oszustwa.

Also includes cryptography tests: PBKDF2-HMAC-SHA256 and AES-GCM-256.

## Zmienne środowiskowe

Full list of environment variables used in the project:

| Variable | Description | Default | Example |
|---------|------|----------|----------|
| `ANTISCAM_BLOG_DB` | Path to the blog's SQLite database | `data/antiscam-blog.sqlite` | `C:\temp\antiscam-blog.sqlite` |
| `ANTISCAM_MONGO_CONNECTION_STRING` | MongoDB server address (optional) | - | `mongodb+srv://user:pass@cluster.mongodb.net` |
| `ANTISCAM_BACKUP_KEY` | Backup encryption key (AES-GCM-256) | Auto-generated in `data/` | `own-long-random-secret` |
| `ANTISCAM_HTTPS_CERT_PASSWORD` | HTTPS (OpenSSL) certificate password | - | `strong-local-password` |

**Setting Variables in PowerShell:**
```powershell
$env:ANTISCAM_BLOG_DB="C:\temp\antiscam-blog.sqlite"
$env:ANTISCAM_BACKUP_KEY="moj-sekret-backup"
```
## Zgodność z sylabusami

**The project fully meets the requirements of the syllabuses of three subjects: Fundamentals of computer security, Security of computer systems and IT security.**

Folder `antiscam` zawiera implementację wszystkich wymaganych efektów uczenia się, a materiały wymagane do oceny projektu znajdują się w:

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
train.py                                   Trenowanie modelu ML (TF-IDF + Naive Bayes)
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
Do testów lub lokalnych eksperymentów można nadpisać ścieżkę bazy zmienną środowiskową:
```powershell
$env:ANTISCAM_BLOG_DB="C:\temp\antiscam-blog.sqlite"
```
### Accounts, administration and safe copies

`POST /api/auth/register` registers user (`userName`, `password`); the first account receives the `Admin` role, the next one the `User` role.

Login via `POST /api/auth/login` returns the Bearer token.

Hasła są zapisywane wyłącznie jako PBKDF2-HMAC-SHA256.

The administrator passes the token in the `Authorization: Bearer <token>` header and can:

- zablokować konto: `POST /api/admin/users/{id}/block`;
- zobaczyć także nieaktywne wpisy: `GET /api/admin/posts`;
- ukryć wpis (miękkie usunięcie): `POST /api/admin/posts/{id}/deactivate` lub `DELETE /api/posts/{id}`;
- przywrócić wpis: `POST /api/admin/posts/{id}/restore`.

When logging in from an IP address different from the previous session, the application automatically creates an encrypted copy of the database if its content has changed.

Set a secret outside the repository:
```powershell
$env:ANTISCAM_BACKUP_KEY = "własny-długi-losowy-sekret"
```
The backup and metadata are saved in `secure_backups/backup.enc.json` and `secure_backups/backup_meta.json`.

AES-GCM-256 with a random nonce and an integrity tag is used; the source database is not saved in an explicit form.

Without the key, the backup is intentionally skipped and a warning entry is written to the log.

Backup wykonuje spójny snapshot SQLite przez mechanizm `BackupDatabase`, więc obejmuje wszystkie tabele (wpisy, komentarze, użytkowników i sesje), a nie tylko plik głównej bazy.

If `ANTISCAM_BACKUP_KEY` is not set, the application creates a local secret in `data/antiscam-backup.key` once; the file and directory with copies are excluded from Git.

### HTTPS w sieci lokalnej (OpenSSL)

The `tools/generate-https-certificate.ps1` script creates a local CA and a PFX certificate with a private IP address in the SAN, analogous to the reference project.

After installing OpenSSL, run:
```powershell
$env:ANTISCAM_HTTPS_CERT_PASSWORD = "silne-lokalne-haslo"
.\tools\generate-https-certificate.ps1 -PrivateIp "192.168.1.22"
```
Then set `Https:Enabled` to `true` and run the application.

Kestrel będzie nasłuchiwał na `0.0.0.0:5001`, a aplikacja będzie dostępna z LAN jako `https://192.168.1.22:5001`.

To remove the browser warning on devices on your network, trust the `certs/antiscam-ca.crt` file.

Without a certificate, a simple `dotnet run --project .\src\AntiScam.Blog.Api` listens on all interfaces on port 5000.

Dla komputera z adresem `192.168.1.22` użyj wtedy `http://192.168.1.22:5000` z urządzenia w tej samej sieci.

If the connection from another device is blocked, allow the .NET application to access incoming traffic in Windows Firewall for Private Networks.

## Updated the English version of the README

Aby odświeżyć `README.en.md` na podstawie polskiego `README.md`, uruchom:
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
## Troubleshooting / FAQ

### Port jest już w użyciu

**Problem:** "Address already in use" przy uruchamianiu aplikacji.

**Rozwiązanie - C# API:**
```powershell
# Zmień port w appsettings.json lub poprzez zmienną:
$env:ASPNETCORE_URLS="http://localhost:5002"
```
**Solution - Python API:**
```powershell
uvicorn antiscam.api:app --reload --port 8001
```
### The SQLite database is locked

**Problem:** "database is locked" during tests or concurrent operations.

**Rozwiązanie:**
- Upewnij się, że tylko jedna instancja C# API jest uruchomiona
- Zamknij inne procesy korzystające z bazy (np.

`sqlite3.exe`)
- Delete the `.sqlite-journal` file if it exists

### Python dependencies nie instalują się

**Issue:** Errors during `pip install -r requirements-dev.txt`.

**Rozwiązanie:**
```powershell
# Uaktualnij pip i setuptools
python -m pip install --upgrade pip setuptools
# Czyszczenie cache
pip cache purge
# Spróbuj ponownie
pip install -r requirements-dev.txt
```
### Backup key is not set

**Problem:** "Backup key not configured" in the logs even though `ANTISCAM_BACKUP_KEY` is empty.

**Solution:**
- The application automatically creates a local key in `data/antiscam-backup.key`
- To use your own key, set the `ANTISCAM_BACKUP_KEY` variable before running
- The file `data/antiscam-backup.key` and the `secure_backups/` directory are excluded from Git

### Tests fail

**Issue:** Errors in `pytest` or `dotnet test`.

**Solution:**
```powershell
# Zczyść cache i zbuduj na nowo
rm -Force -Recurse bin, obj  # lub Remove-Item
rm -Force .pytest_cache
dotnet clean
dotnet build
pytest --tb=short  # Szczegółowy output
```
### OpenSSL certificate issues

**Problem:** HTTPS errors on Windows or certificate not trusted.

**Solution:**
- Run the script as Administrator: `Set-ExecutionPolicy -ExecutionPolicy Unrestricted`
- Make sure OpenSSL is installed: `openssl version`
- Trust CA certificate: `certs/antiscam-ca.crt` (add to Windows Certificate Store)

## Licencja

The project is available under the MIT license.