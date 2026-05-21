# AntiScam

AntiScam now contains two application surfaces:

- a Python/FastAPI phishing-risk engine,
- a C# ASP.NET Core Blog WebAPI with HTML files and SQLite storage.

The new blog API is connected to the workspace folder `C:\Users\kondz\antiscam`. By default, its SQLite database is created at `C:\Users\kondz\antiscam\data\antiscam-blog.sqlite`.

## Requirements

- Python 3.10+
- .NET SDK 8.0+
- pip

## Quick Start: C# Blog WebAPI

```powershell
dotnet restore AntiScamBlog.sln
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj
```

After startup, open the URL printed by `dotnet run`, usually `http://localhost:5000` or `http://localhost:5080`.

### HTML Frontend

Open:

```text
/
```

The page loads posts from the API and lets you create a new blog post.

### Blog Endpoints

```text
GET    /api/health
GET    /api/workspace
GET    /api/posts
GET    /api/posts/{slug}
POST   /api/posts
PUT    /api/posts/{id}
DELETE /api/posts/{id}
```

Create a post:

```powershell
curl -Method POST http://localhost:5000/api/posts `
  -ContentType "application/json" `
  -Body '{"title":"Phishing alert","summary":"Short summary","content":"Post body","author":"AntiScam Team"}'
```

## Quick Start: Python AntiScam API

```powershell
pip install -r requirements-dev.txt
pip install -e .
uvicorn antiscam.api:app --reload
```

The Python API will be available at `http://localhost:8000`.

### Python API Endpoints

```text
GET  /
POST /scan
```

Example:

```powershell
curl -Method POST http://localhost:8000/scan `
  -ContentType "application/json" `
  -Body '{"text":"Send BLIK 123456 immediately!"}'
```

## Tests

C# tests:

```powershell
dotnet test AntiScamBlog.sln
```

Python tests:

```powershell
pytest
```

The C# project includes unit tests for validation and slug generation plus integration tests for the API, SQLite persistence, and the static HTML page.

## Structure

```text
antiscam/                                  Python AntiScam engine
tests/                                     Python tests
src/AntiScam.Blog.Api/                    C# ASP.NET Core Blog WebAPI
src/AntiScam.Blog.Api/wwwroot/            HTML, CSS, and JS files
tests/AntiScam.Blog.Api.Tests/            C# unit and integration tests
AntiScamBlog.sln                          .NET solution
README.md                                 Polish documentation
README.en.md                              English documentation
```

## C# Blog WebAPI Configuration

Defaults live in `src/AntiScam.Blog.Api/appsettings.json`:

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

For tests or local experiments, override the database path with an environment variable:

```powershell
$env:ANTISCAM_BLOG_DB="C:\temp\antiscam-blog.sqlite"
```

## GitHub

The repository origin is configured as:

```text
https://github.com/Kondexor2000/antiscam.git
```

Suggested flow after changes:

```powershell
git status
git add .
git commit -m "Add C# blog WebAPI with SQLite"
git push origin main
```

## License

This project is available under the MIT License.
