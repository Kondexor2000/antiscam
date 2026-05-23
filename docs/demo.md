# Demonstracja projektu

## Przygotowanie

```powershell
dotnet restore AntiScamBlog.sln
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj --urls http://localhost:5087
```

W drugim terminalu:

```powershell
python -m pytest
dotnet test AntiScamBlog.sln
```

## Demo 1: wpis bezpieczny

```powershell
Invoke-WebRequest -Uri http://localhost:5087/api/posts `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"title":"Bezpieczne spotkanie","summary":"Normalny wpis edukacyjny.","content":"Czesc, opisujemy spokojne zasady ochrony przed phishingiem.","author":"AntiScam Team"}'
```

Oczekiwany wynik: `201 Created`.

## Demo 2: wpis ryzykowny

```powershell
try {
  Invoke-WebRequest -Uri http://localhost:5087/api/posts `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"title":"Pilny BLIK","summary":"Konto zablokowane.","content":"Wyslij kod BLIK 123456 natychmiast i kliknij teraz.","author":"Scammer"}'
} catch {
  [int]$_.Exception.Response.StatusCode
}
```

Oczekiwany wynik: `422`. Wpis nie zostaje zapisany.

## Demo 3: sprawdzenie, ze wpis nie istnieje

```powershell
try {
  Invoke-WebRequest -Uri http://localhost:5087/api/posts/pilny-blik
} catch {
  [int]$_.Exception.Response.StatusCode
}
```

Oczekiwany wynik: `404`.

## Demo 4: Python API

```powershell
uvicorn antiscam.api:app --reload
```

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: wysoki wynik ryzyka.
