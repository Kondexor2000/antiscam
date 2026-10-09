# AntiScam

AntiScam to repozytorium z trzema częściami:

- silnikiem Python/FastAPI do oceny ryzyka wiadomości phishingowych,
- blogiem C# ASP.NET Core WebAPI z plikami HTML i bazą SQLite,
- panelem AntiScam API Platform z lokalnym rejestrem API i reverse proxy.

Nowy blog jest połączony z folderem roboczym, a domyślna baza SQLite powstaje w `data/antiscam-blog.sqlite` (względnie do katalogu projektu).

## Sugestie rozszerzeń

git clone https://github.com/Kondexor2000/antihacker.git

## Wymagania

- Python 3.10+
- .NET SDK 8.0+
- pip
- OpenSSL (opcjonalnie, do generowania certyfikatów HTTPS)

## Szybki start: C# Blog WebAPI

```powershell
dotnet restore AntiScamBlog.sln
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```

Po uruchomieniu aplikacja jest dostępna pod adresem wyświetlonym przez `dotnet run`, zwykle `http://localhost:5000` albo `http://localhost:5080`.

### Panel API Platform

Otwórz w przeglądarce:

```text
/
```

Strona główna (`/`) odczytuje katalog usług i liczniki żądań z działającego backendu. Żądania Blog API (`/api/posts` i komentarze) oraz przekazania gatewaya (`/gateway/{id}/{path}`) są liczone oddzielnie. Konfiguracje API są trwale zapisywane w tej samej bazie SQLite co blog. Każde API ma losowy klucz klienta, którego hash jest przechowywany w bazie.

Przed uruchomieniem ustaw klucz administratora (co najmniej 32 losowe bajty) — bez niego endpointy zarządzania zwracają `503`:

```powershell
$env:ANTISCAM_CONTROL_PLANE_KEY = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```

Panel poprosi o ten klucz przy pierwszym odczycie katalogu i przechowa go tylko w pamięci sesji przeglądarki. Endpointy Control Plane wymagają nagłówka `X-Control-Plane-Key`. Klucz klienta API jest generowany przy rejestracji (lub rotacji), pokazywany tylko raz i wymagany w nagłówku `X-API-Key` przy wywołaniu `/gateway/{id}/{path}`. Przykładowe operacje zarządzania:

```text
GET    /api/gateway/apis
GET    /api/gateway/apis/{id}
POST   /api/gateway/apis
PUT    /api/gateway/apis/{id}
DELETE /api/gateway/apis/{id}
POST   /api/gateway/apis/{id}/rotate-key
GET    /api/gateway/metrics
GET    /api/blog/metrics
```

To działający, pojedynczoinstancyjny MVP, nie gotowa platforma produkcyjna. Metryki są przechowywane w pamięci i zerują się po restarcie; nie są współdzielone między instancjami. Klucz API jest współdzielonym sekretem usługi, nie zastępuje OAuth2/OIDC, ról ani limitów per klient. Aplikacja nie zapewnia WAF/DDoS, rozproszonego rate limitingu ani magazynu sekretów upstreamów. Ogranicz dostęp sieciowy do panelu administracyjnego, używaj HTTPS i rejestruj wyłącznie zaufane adresy upstream. Istniejąca analiza antyscamowa pozostaje dostępna w dotychczasowym API bloga.

### Blog i analiza antyscamowa

Endpointy wpisów blogowych nadal działają niezależnie od nowego panelu.

Przed publikacją C# WebAPI analizuje tytuł, streszczenie, treść i autora. Wpis zostanie zapisany tylko przy statusie `LOW RISK`; dla `MEDIUM RISK` lub `HIGH RISK` API zwraca `422 Unprocessable Entity` i nie zapisuje wpisu w SQLite.

### Opcjonalna baza NoSQL (MongoDB)

SQLite nadal jest domyślną bazą wpisów bloga. Opcjonalna baza MongoDB przechowuje jedynie zgłoszenia odrzucone przez analizę ryzyka, więc nie zmienia działania istniejącego magazynu SQLite.

MongoDB jest włączony przez `NoSql:Enabled`. Adres serwera można podać w `NoSql:ConnectionString` albo przez zmienną środowiskową `ANTISCAM_MONGO_CONNECTION_STRING`. Domyślna baza i kolekcja to `antiscam` oraz `blocked-submissions`. Bieżącą konfigurację obu magazynów zwraca `GET /api/storage`.

Odrzucone zgłoszenia można odczytać przez `GET /api/incidents?limit=50`. Po włączeniu MongoDB endpoint zwraca wpisy z kolekcji `blocked-submissions`, od najnowszych. Przy wyłączonej lub niedostępnej bazie odpowiedzią jest pusta lista.

### Endpointy backendu

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

### Obsługa żądań przez Gateway

Gateway przekazuje żądania HTTP do upstreamu zarejestrowanego dla danego API. Są dwa oddzielne klucze:

- `ANTISCAM_CONTROL_PLANE_KEY` / nagłówek `X-Control-Plane-Key` — dostęp administratora do rejestracji API, konfiguracji i metryk.
- klucz klienta API / nagłówek `X-API-Key` — wymagany przy każdym żądaniu przekazywanym przez `/gateway`.

#### 1. Zarejestruj upstream

Uruchom usługę upstream i upewnij się, że jest osiągalna z procesu Blog API. Wartością `upstreamUrl` jest bazowy adres usługi, bez danych logowania, query ani fragmentu. Poniższy przykład rejestruje Python AntiScam API, uruchomione domyślnie na porcie 8000:

```powershell
$baseUrl = "http://localhost:5000" # Zastąp adresem C# Blog API, jeśli używa innego portu.
$adminKey = $env:ANTISCAM_CONTROL_PLANE_KEY
$adminHeaders = @{ "X-Control-Plane-Key" = $adminKey }

$registration = Invoke-RestMethod `
  -Uri "$baseUrl/api/gateway/apis" `
  -Method POST `
  -Headers $adminHeaders `
  -ContentType "application/json" `
  -Body '{"name":"Python AntiScam","version":"v1","upstreamUrl":"http://localhost:8000","enabled":true}'

$apiId = $registration.api.id
$clientKey = $registration.apiKey
$registration.api
```

Odpowiedź zawiera `api.id`, adres `api.gatewayUrl` oraz wygenerowany `apiKey`. Zapisz klucz klienta w bezpiecznym magazynie — zwracany jest tylko podczas rejestracji lub rotacji, a baza przechowuje wyłącznie jego hash. W panelu Control Plane możesz dodać API, edytować nazwę, wersję i upstream, włączać lub wyłączać rejestrację, rotować klucz klienta oraz usuwać API. Formularz **Dodaj API** / **Edytuj** zapisuje konfigurację, a po rejestracji lub rotacji pokazuje nowy klucz w oknie umożliwiającym skopiowanie.

#### 2. Wywołuj usługę przez Gateway

Ścieżka po identyfikatorze API jest dołączana do `upstreamUrl`; query string, metoda HTTP, treść żądania i zwykłe nagłówki są przekazywane upstreamowi. Na przykład `/gateway/{id}/scan?verbose=true` kieruje do `/scan?verbose=true` usługi upstream:

Żądanie możesz też wysłać z panelu: przy wybranym API kliknij **Testuj**, wklej klucz klienta `X-API-Key`, wybierz metodę, wpisz ścieżkę i ewentualną treść, a następnie kliknij **Wyślij żądanie**. Wynik pokazuje status, czas, nagłówki i treść odpowiedzi. Dodatkowe nagłówki wpisuj po jednym w formacie `Nazwa: wartość`. Klucz nie jest zapisywany przez panel. Przycisk testu wykonuje rzeczywiste żądanie i zwiększa metryki Gateway.

Przykład testu Python AntiScam API w panelu: upstream `http://localhost:8000`, metoda `POST`, ścieżka `/scan`, `Content-Type: application/json` i treść:

```json
{"text":"Wyślij BLIK 123456 natychmiast!"}
```

Panel i usługa upstream muszą być uruchomione, a wpisana ścieżka jest ścieżką względem skonfigurowanego upstreamu. Klucz klienta jest pokazywany przy rejestracji lub rotacji; jeśli nie został zapisany, trzeba go obrócić i zaktualizować używających go klientów.

```powershell
$clientHeaders = @{ "X-API-Key" = $clientKey }

# Python AntiScam API: GET / na upstreamie http://localhost:8000
Invoke-RestMethod `
  -Uri "$baseUrl/gateway/$apiId/" `
  -Method GET `
  -Headers $clientHeaders

# Python AntiScam API: POST /scan
$scanBody = @{ text = "Wyślij BLIK 123456 natychmiast!" } | ConvertTo-Json
Invoke-RestMethod `
  -Uri "$baseUrl/gateway/$apiId/scan" `
  -Method POST `
  -Headers $clientHeaders `
  -ContentType "application/json" `
  -Body $scanBody
```

Obsługiwane metody to `GET`, `HEAD`, `POST`, `PUT`, `PATCH`, `DELETE` i `OPTIONS`. Odpowiedź upstreamu — status HTTP, treść i nagłówki odpowiedzi — jest zwracana klientowi. Nagłówki hop-by-hop są usuwane, a `X-API-Key` i `X-Control-Plane-Key` nie są przekazywane upstreamowi.

Typowe błędy Gateway:

| HTTP | Znaczenie |
| --- | --- |
| `401 Unauthorized` | Brak lub niepoprawny `X-API-Key`. |
| `404 Not Found` | Nieznany identyfikator API. |
| `503 Service Unavailable` | API jest wyłączone w katalogu. |
| `502 Bad Gateway` | Nie można połączyć się z upstreamem. Sprawdź `upstreamUrl` i dostępność usługi z hosta Blog API. |
| `504 Gateway Timeout` | Upstream nie odpowiedział w wymaganym czasie. |

#### 3. Zarządzaj rejestracją i kluczami

Endpointy `/api/gateway/apis` wymagają nagłówka `X-Control-Plane-Key`. Dostępne są: `GET /api/gateway/apis` (lista), `GET /api/gateway/apis/{id}` (szczegóły), `POST /api/gateway/apis` (rejestracja), `PUT /api/gateway/apis/{id}` (zmiana nazwy, wersji, upstreamu lub stanu), `DELETE /api/gateway/apis/{id}` (usunięcie) i `POST /api/gateway/apis/{id}/rotate-key` (rotacja klucza klienta).

Przykład aktualizacji upstreamu:

```powershell
$config = @{
  name = "Python AntiScam"
  version = "v1"
  upstreamUrl = "http://localhost:8000"
  enabled = $true
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri "$baseUrl/api/gateway/apis/$apiId" `
  -Method PUT `
  -Headers $adminHeaders `
  -ContentType "application/json" `
  -Body $config
```

Rotacja natychmiast unieważnia poprzedni klucz klienta. Odpowiedź zawiera nowy `apiKey`; zaktualizuj nim klientów przed kolejnymi wywołaniami Gateway:

```powershell
$rotated = Invoke-RestMethod `
  -Uri "$baseUrl/api/gateway/apis/$apiId/rotate-key" `
  -Method POST `
  -Headers $adminHeaders
$clientKey = $rotated.apiKey
```

#### 4. Sprawdź metryki

`GET /api/gateway/metrics` zwraca liczbę żądań przekazanych przez Gateway, liczbę odpowiedzi 4xx/5xx i średnie opóźnienie. `GET /api/gateway/apis/{id}` oraz lista API podają te same liczniki osobno dla każdej rejestracji. Oba endpointy wymagają `X-Control-Plane-Key`.

```powershell
Invoke-RestMethod `
  -Uri "$baseUrl/api/gateway/metrics" `
  -Headers $adminHeaders
```

Liczniki są przechowywane w pamięci bieżącego procesu i zerują się po restarcie. Obejmują wszystkie żądania do tras Gateway, również te odrzucone z powodu braku klucza; ruch do Blog API poza `/gateway/{id}/{path}` jest mierzony osobno przez `/api/blog/metrics`.

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

### Trenowanie modelu ML

Projekt zawiera prosty model Machine Learning (TF-IDF + Multinomial Naive Bayes) do klasyfikacji wiadomości. Aby wytrenować model:

```powershell
python train.py
```

Skrypt trenuje na 16 próbkach treningowych (8 phishingowych, 8 bezpiecznych) i zapisuje model w `models/model.joblib`. Model jest następnie używany przez Python API do analizy ryzyka wiadomości.

### Dokumentacja API (Swagger UI / OpenAPI)

Obie aplikacje udostępniają interaktywną dokumentację:

**C# Blog WebAPI:**
- Swagger UI: `http://localhost:5000/swagger/ui`
- OpenAPI JSON: `http://localhost:5000/swagger/v1/swagger.json`

**Python AntiScam API:**
- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`
- OpenAPI JSON: `http://localhost:8000/openapi.json`

## Uruchamianie obu aplikacji jednocześnie

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

Blog będzie dostępny na `http://localhost:5000`, a Python API na `http://localhost:8000`.

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
Obejmuje również testy kryptografii AES-GCM-256.

## Zmienne środowiskowe

Pełna lista zmiennych środowiskowych używanych w projekcie:

| Zmienna | Opis | Domyślnie | Przykład |
|---------|------|----------|----------|
| `ANTISCAM_BLOG_DB` | Ścieżka do bazy SQLite blogu | `data/antiscam-blog.sqlite` | .\data\antiscam-blog.sqlite |
| `ANTISCAM_CONTROL_PLANE_KEY` | Klucz administratora API Control Plane | brak | wygeneruj 32 losowe bajty |
| `ANTISCAM_MONGO_CONNECTION_STRING` | Adres serwera MongoDB (opcjonalne) | - | `mongodb+srv://user:pass@cluster.mongodb.net` |
| `ANTISCAM_HTTPS_CERT_PASSWORD` | Hasło do certyfikatu HTTPS (OpenSSL) | - | `silne-lokalne-haslo` |

**Ustawianie zmiennych w PowerShell:**
```powershell
$env:ANTISCAM_BLOG_DB = '.\data\antiscam-blog.sqlite'
$env:ANTISCAM_CONTROL_PLANE_KEY = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

## Zgodność z sylabusami

**Projekt w pełni spełnia wymagania sylabusów trzech przedmiotów: Podstawy bezpieczeństwa komputerowego, Bezpieczeństwo systemów komputerowych oraz Bezpieczeństwo informatyczne.**

Folder `antiscam` zawiera implementację wszystkich wymaganych efektów uczenia się, a materiały wymagane do oceny projektu znajdują się w:

- `SYLLABUS_MAPPING.md` - mapowanie efektów uczenia się na kod i dokumentację,
- `docs/ai_syllabus_mapping.md` - mapowanie sylabusów z folderu `AI_antiscam`,
- `docs/project_report.md` - raport projektowy,
- `docs/ai_project_report.md` - raport rozszerzenia AI/NLP,
- `docs/security_audit.md` - przegląd bezpieczeństwa i checklista,
- `docs/cryptography.md` - opis hashowania, szyfrowania i zarządzania kluczami,
- `docs/ai_ethics.md` - etyka AI i sztuczna empatia,
- `docs/demo.md` - scenariusz demonstracji,
- `docs/presentation_outline.md` - konspekt prezentacji,
- `docs/labs/` - instrukcje laboratoryjne bezpieczeństwa,
- `docs/ai_labs/` - instrukcje laboratoryjne AI/NLP.

## Struktura

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

## Konfiguracja C# Blog WebAPI

Domyślne ustawienia są w `src/AntiScam.Blog.Api/appsettings.json`:

```json
{
  "Workspace": {
    "RootPath": "."
  },
  "Blog": {
    "DatabasePath": "data/antiscam-blog.sqlite"
  }
}
```

Do testów lub lokalnych eksperymentów można nadpisać ścieżkę bazy zmienną środowiskową:

```powershell
$env:ANTISCAM_BLOG_DB = '.\data\antiscam-blog.sqlite'
```

### HTTPS w sieci lokalnej (OpenSSL)

Skrypt `tools/generate-https-certificate.ps1` tworzy lokalne CA oraz certyfikat PFX z prywatnym adresem IP w SAN, analogicznie do projektu referencyjnego. Po zainstalowaniu OpenSSL uruchom:

```powershell
$env:ANTISCAM_HTTPS_CERT_PASSWORD = "silne-lokalne-haslo"
.\tools\generate-https-certificate.ps1 -PrivateIp "192.168.1.22"
```

Następnie ustaw `Https:Enabled` na `true` i uruchom aplikację. Kestrel będzie nasłuchiwał na `0.0.0.0:5001`, a aplikacja będzie dostępna z LAN jako `https://192.168.1.22:5001`. Aby usunąć ostrzeżenie przeglądarki na urządzeniach w sieci, zaufaj plikowi `certs/antiscam-ca.crt`.

Bez certyfikatu zwykłe `dotnet run --project .\src\AntiScam.Blog.Api` nasłuchuje na wszystkich interfejsach na porcie 5000. Dla komputera z adresem `192.168.1.22` użyj wtedy `http://192.168.1.22:5000` z urządzenia w tej samej sieci. Jeśli połączenie z innego urządzenia zostanie zablokowane, zezwól aplikacji .NET na ruch przychodzący w Zaporze Windows dla sieci prywatnych.

## Aktualizacja angielskiej wersji README

Aby odświeżyć `README.en.md` na podstawie polskiego `README.md`, uruchom:

```powershell
.\tools\sync-readme-en.ps1
```

Skrypt używa `deep-translator` do tłumaczenia treści dokumentacji i zapisuje wynik do `README.en.md`.

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

## Troubleshooting / FAQ

### Port jest już w użyciu

**Problem:** "Address already in use" przy uruchamianiu aplikacji.

**Rozwiązanie - C# API:**
```powershell
# Zmień port w appsettings.json lub poprzez zmienną:
$env:ASPNETCORE_URLS="http://localhost:5002"
```

**Rozwiązanie - Python API:**
```powershell
uvicorn antiscam.api:app --reload --port 8001
```

### Baza SQLite jest zablokowana

**Problem:** "database is locked" podczas testów lub jednoczesnych operacji.

**Rozwiązanie:**
- Upewnij się, że tylko jedna instancja C# API jest uruchomiona
- Zamknij inne procesy korzystające z bazy (np. `sqlite3.exe`)
- Usuń plik `.sqlite-journal` jeśli istnieje

### Python dependencies nie instalują się

**Problem:** Błędy podczas `pip install -r requirements-dev.txt`.

**Rozwiązanie:**
```powershell
# Uaktualnij pip i setuptools
python -m pip install --upgrade pip setuptools
# Czyszczenie cache
pip cache purge
# Spróbuj ponownie
pip install -r requirements-dev.txt
```

### Testy nie przechodzą

**Problem:** Błędy w `pytest` lub `dotnet test`.

**Rozwiązanie:**
```powershell
# Zczyść cache i zbuduj na nowo
rm -Force -Recurse bin, obj  # lub Remove-Item
rm -Force .pytest_cache
dotnet clean
dotnet build
pytest --tb=short  # Szczegółowy output
```

### OpenSSL certificate issues

**Problem:** Błędy HTTPS na Windows lub certyfikat nie jest zaufany.

**Rozwiązanie:**
- Uruchom skrypt jako Administrator: `Set-ExecutionPolicy -ExecutionPolicy Unrestricted`
- Upewnij się, że OpenSSL jest zainstalowany: `openssl version`
- Zaufaj certyfikatowi CA: `certs/antiscam-ca.crt` (dodaj do Windows Certificate Store)

## Licencja

Projekt jest dostępny na licencji MIT.
