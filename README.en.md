# AntiScam

AntiScam is a repository with three parts:

- Python/FastAPI engine for assessing the risk of phishing messages,
- C# ASP.NET Core WebAPI blog with HTML files and SQL Server database.
- AntiScam API Platform with a persistent API registry and reverse proxy.

The blog uses Microsoft SQL Server through `Microsoft.Data.SqlClient`. Create the `AntiScamBlog` database on a SQL Server or LocalDB instance, then configure `Blog:ConnectionString` or `ANTISCAM_BLOG_CONNECTION_STRING`.

## Extension suggestions

git clone https://github.com/Kondexor2000/antihacker.git

## Requirements

- Python 3.10+
- .NET SDK 8.0+
- Redis (optional; response cache for public blog endpoints)
- beep
- OpenSSL (optional, for generating HTTPS certificates)

## Quick Start: C# WebAPI Blog
```powershell
dotnet restore AntiScamBlog.sln
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```
Once the application is launched, it is available at the address displayed by `dotnet run`, usually `http://localhost:5000` or `http://localhost:5080`.

### Optional Redis cache

Public post and comment reads are cached for 60 seconds. The cache is invalidated when a post is created or edited, or a comment is added. Without Redis, ASP.NET uses its built-in in-memory cache. To share cached responses across instances, start Redis and set `ANTISCAM_REDIS_CONNECTION_STRING` (for example, `localhost:6379`) or configure `Cache:RedisConnectionString`:

```powershell
docker run --rm -d --name antiscam-redis -p 6379:6379 redis:7-alpine
$env:ANTISCAM_REDIS_CONNECTION_STRING = "localhost:6379"
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```

`GET /api/health` reports the selected cache store in `distributedCache`.

### API Control Plane

The `/` dashboard reads the live API catalog and request counters from the running backend. Blog API requests (`/api/posts` and comments) and gateway forwards (`/gateway/{id}/{path}`) are counted separately. API configurations are persisted in the blog's SQL Server database. Each API receives a random client key whose hash is stored in SQL Server.

Set an administrator key before starting the application (at least 32 random bytes). Management endpoints return `503` when no key is configured:

```powershell
$env:ANTISCAM_CONTROL_PLANE_KEY = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```

The dashboard prompts for this key and keeps it only in the browser session. Control Plane endpoints require the `X-Control-Plane-Key` header. A client key is generated during API registration or rotation, shown only once, and required in the `X-API-Key` header when calling `/gateway/{id}/{path}`.

This is a working single-instance MVP, not a production platform: metrics are held in memory and reset on restart; they are not shared across instances. Per-API keys are shared service secrets, not a replacement for OAuth2/OIDC, roles, or per-client quotas. WAF/DDoS protection, distributed rate limiting, and an upstream secret store are not included. Restrict network access to the admin interface, use HTTPS, and register only trusted upstream destinations.

### Blog frontend

Open in browser:
```text
/blog.html
```
The blog frontend loads entries from the API and allows you to add a new blog entry.

Before publication, C# WebAPI analyzes the title, abstract, content and author.

The entry will only be saved in `LOW RISK` status; for `MEDIUM RISK` or `HIGH RISK` the API returns `422 Unprocessable Entity` and does not write an entry to SQL Server.

## Docker Compose: the whole project

One Compose stack starts the Python AntiScam API, the .NET blog and dashboard,
SQL Server, and Redis. SQL Server and Redis stay on the private Compose network.
The API creates the SQL schema during startup.

Create a local environment file and replace both example secrets:

```powershell
Copy-Item .env.example .env
```

Edit `.env`: `MSSQL_SA_PASSWORD` must meet SQL Server's password requirements,
and `ANTISCAM_CONTROL_PLANE_KEY` must be a random secret of at least 32 bytes.
The `.env` file is ignored by Git. Then run:

```powershell
docker compose up --build -d
docker compose ps
```

The blog and dashboard are at `http://localhost:5000`; the Python API is at
`http://localhost:8000`. View logs with `docker compose logs -f blog-api
python-api`. Stop the stack with `docker compose down`. SQL and Redis data stay
in named volumes; `docker compose down -v` also removes that data.

MongoDB is optional and stores submissions rejected by risk analysis. Set
`ANTISCAM_MONGO_ENABLED=true` in `.env`, then run `docker compose --profile
nosql up --build -d`. MongoDB integration is disabled by default.

## Jenkins CI

The root `Jenkinsfile` runs Python tests, .NET tests against a temporary SQL
Server, and builds both application images. Configure the Jenkins job to use
this file and a Linux agent with Python 3, .NET SDK 8, and Docker CLI/daemon.
Images are built locally and are not pushed to a registry; publishing requires
registry credentials to be configured.

### Optional NoSQL database (MongoDB)

SQL Server is still the default blog post database.

The optional MongoDB database only stores tickets rejected by risk analysis, so it does not change the operation of the existing SQL Server store.

MongoDB is enabled by `NoSql:Enabled`.

The server address can be specified in `NoSql:ConnectionString` or via the `ANTISCAM_MONGO_CONNECTION_STRING` environment variable.

The default database and collection are `antiscam` and `blocked-submissions`.

The current configuration of both storages is returned by `GET /api/storage`.

Rejected reports can be read by `GET /api/incidents?limit=50`.

After enabling MongoDB, the endpoint returns entries from the `blocked-submissions` collection, newest first.

If the database is disabled or unavailable, the response is an empty list.

### Blog endpoints
```text
GET    /api/health
GET    /api/gateway/apis
GET    /api/gateway/apis/{id}
POST   /api/gateway/apis
PUT    /api/gateway/apis/{id}
DELETE /api/gateway/apis/{id}
POST   /api/gateway/apis/{id}/rotate-key
GET    /api/gateway/metrics
GET    /api/blog/metrics
ANY    /gateway/{id}/{path}
GET    /api/storage
GET    /api/incidents?limit=50
GET    /api/workspace
GET    /api/posts
GET    /api/posts/{slug}
POST   /api/posts
PUT    /api/posts/{id}
DELETE /api/posts/{id}
```
Example of adding an entry:
```powershell
curl -Method POST http://localhost:5000/api/posts `
  -ContentType "application/json" `
  -Body '{"title":"Phishing Alert","summary":"Short description","content":"Post content","author":"AntiScam Team"}'
```
## Quickstart: Python AntiScam API
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
Example:
```powershell
curl -Method POST http://localhost:8000/scan `
  -ContentType "application/json" `
  -Body '{"text":"Send BLIK 123456 immediately!"}'
```
The `/ai/explain` endpoint shows practically what AI/NLP facilitates in a project: it recognizes user intent, emotional tone, important terms, proper names, similarity to a fraud pattern and suggests a safe next action.
```powershell
curl -Method POST http://localhost:8000/ai/explain `
  -ContentType "application/json" `
  -Body '{"text":"I\'m scared, Polish Bank wants my BLIK code 123456 urgently"}'
```
### Training the ML model

The project includes a simple Machine Learning model (TF-IDF + Multinomial Naive Bayes) for news classification. To train a model:
```powershell
python train.py
```
The script trains on 16 training samples (8 phishing, 8 safe) and saves the model in `models/model.joblib`.

The model is then used by the Python API to analyze the risk of the message.

### API Documentation (Swagger UI / OpenAPI)

Both applications provide interactive documentation:

**C# Blog WebAPI:**
- Swagger UI: `http://localhost:5000/swagger/ui`
- OpenAPI JSON: `http://localhost:5000/swagger/v1/swagger.json`

**Python AntiScam API:**
- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`
- OpenAPI JSON: `http://localhost:8000/openapi.json`

## Run both applications at the same time

To run the project fully (blog + AI engine), open two terminals and run in each:

**Terminal 1 - C# API Blog:**
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
The C# project includes unit tests for validation and minions, and integration tests for API, SQL Server, and static HTML.

It also includes tests to block the publication of entries that are at risk of phishing or fraud.

Also includes AES-GCM-256 cryptography tests.

## Environment variables

Full list of environment variables used in the project:

| Variable | Description | Default | Example |
|---------|------|----------|----------|
| `ANTISCAM_BLOG_CONNECTION_STRING` | SQL Server connection string | `Blog:ConnectionString` | `Server=(localdb)\MSSQLLocalDB;Database=AntiScamBlog;Integrated Security=True;TrustServerCertificate=True` |
| `ANTISCAM_CONTROL_PLANE_KEY` | API Control Plane administrator key | unset | generate 32 random bytes |
| `ANTISCAM_MONGO_CONNECTION_STRING` | MongoDB server address (optional) | - | `mongodb+srv://user:pass@cluster.mongodb.net` |
| `ANTISCAM_REDIS_CONNECTION_STRING` | Shared cache for blog responses across instances | in-memory cache | `localhost:6379` |
| `ANTISCAM_HTTPS_CERT_PASSWORD` | HTTPS (OpenSSL) certificate password | - | `strong-local-password` |

**Setting Variables in PowerShell:**
```powershell
$env:ANTISCAM_BLOG_CONNECTION_STRING = 'Server=(localdb)\MSSQLLocalDB;Database=AntiScamBlog;Integrated Security=True;TrustServerCertificate=True'
```
## Compliance with Syllabi

**The project fully meets the requirements of the syllabi for three courses: Fundamentals of Computer Security, Computer Systems Security, and Information Security.**

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
antiscam/                                  Python AntiScam engine
antiscam/ai.py                             Educational AI/NLP components
tests/                                     Python tests
train.py                                   ML model training (TF-IDF + Naive Bayes)
src/AntiScam.Blog.Api/                    C# ASP.NET Core Blog WebAPI
src/AntiScam.Blog.Api/wwwroot/            HTML, CSS and JS files
tests/AntiScam.Blog.Api.Tests/            Unit and integration tests C#
docs/                                     Report, audit, demo and laboratories
docs/ai_labs/                              Laboratories for AI_antiscam syllabi
SYLLABUS_MAPPING.md                       Project to syllabi mapping
AntiScamBlog.sln                          .NET Solution
README.md                                 Polish documentation
README.en.md                              English documentation
```
## C# Configuration WebAPI Blog

The default settings are in `src/AntiScam.Blog.Api/appsettings.json`:
```json
{
  "Workspace": {
    "RootPath": "."
  },
  "Blog": {
    "ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=AntiScamBlog;Integrated Security=True;TrustServerCertificate=True"
  }
}
```
For testing or local experiments, override the connection string with an environment variable:
```powershell
$env:ANTISCAM_BLOG_CONNECTION_STRING = 'Server=(localdb)\MSSQLLocalDB;Database=AntiScamBlog;Integrated Security=True;TrustServerCertificate=True'
```
### HTTPS on the local network (OpenSSL)

The `tools/generate-https-certificate.ps1` script creates a local CA and a PFX certificate with a private IP address in the SAN, analogous to the reference project. After installing OpenSSL, run:
```powershell
$env:ANTISCAM_HTTPS_CERT_PASSWORD = "strong-local-password"
.\tools\generate-https-certificate.ps1 -PrivateIp "192.168.1.22"
```
Then set `Https:Enabled` to `true` and run the application.

Kestrel will listen on `0.0.0.0:5001` and the application will be accessible from the LAN as `https://192.168.1.22:5001`.

To remove the browser warning on network devices, trust the `certs/antiscam-ca.crt` file.

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
git commit -m "Add C# blog WebAPI with SQL Server"
git push origin main
```
## Troubleshooting / FAQ

### The port is already in use

**Problem:** "Address already in use" when starting the application.

**Solution - C# API:**
```powershell
# Change the port in appsettings.json or via environment variable:
$env:ASPNETCORE_URLS="http://localhost:5002"
```
**Solution - Python API:**
```powershell
uvicorn antiscam.api:app --reload --port 8001
```
### SQL Server connection fails

**Problem:** The API cannot connect to SQL Server.

**Solution:**
- Check that SQL Server/LocalDB is running and reachable.
- Create the `AntiScamBlog` database before the first startup.
- Check `Blog:ConnectionString` or `ANTISCAM_BLOG_CONNECTION_STRING`, including account permissions.

The existing SQLite file is left on disk and is not imported automatically. Export any data you need before removing that file.

### Python dependencies not installing

**Issue:** Errors during `pip install -r requirements-dev.txt`.

**Solution:**
```powershell
# Update pip and setuptools
python -m pip install --upgrade pip setuptools
# Clear cache
pip cache purge
# Try again
pip install -r requirements-dev.txt
```
### Tests fail

**Issue:** Errors in `pytest` or `dotnet test`.

**Solution:**
```powershell
# Clean cache and rebuild
rm -Force -Recurse bin, obj  # or Remove-Item
rm -Force .pytest_cache
dotnet clean
dotnet build
pytest --tb=short  # Detailed output
```
### OpenSSL certificate issues

**Problem:** HTTPS errors on Windows or certificate not trusted.

**Solution:**
- Run the script as Administrator: `Set-ExecutionPolicy -ExecutionPolicy Unrestricted`
- Make sure OpenSSL is installed: `openssl version`
- Trust CA certificate: `certs/antiscam-ca.crt` (add to Windows Certificate Store)

## License

The project is available under the MIT license.
