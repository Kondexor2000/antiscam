# Demonstracja projektu

## Przygotowanie

Uruchamiaj komendy z katalogu repozytorium:

```powershell
cd C:\Users\kondz\antiscam
venv\Scripts\activate
```

Najpierw sprawdz testy, gdy aplikacja C# nie jest jeszcze uruchomiona:

```powershell
dotnet restore AntiScamBlog.sln
python -m pytest
dotnet test AntiScamBlog.sln
```

Nastepnie uruchom blog API:

```powershell
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj --urls http://localhost:5000
```

Strona demo jest wtedy dostepna pod adresem `http://localhost:5000/`.
Jesli `Invoke-WebRequest` zwraca w `catch` wartosc `0`, oznacza to brak polaczenia z serwerem, a nie odpowiedz HTTP. Sprawdz wtedy, czy aplikacja C# nadal dziala i czy uzywasz tego samego portu.

## Demo 1: wpis bezpieczny

```powershell
Invoke-WebRequest -Uri http://localhost:5000/api/posts `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"title":"Bezpieczne spotkanie","summary":"Normalny wpis edukacyjny.","content":"Czesc, opisujemy spokojne zasady ochrony przed phishingiem.","author":"AntiScam Team"}'
```

Oczekiwany wynik: `201 Created`.

## Demo 2: wpis ryzykowny

```powershell
try {
  Invoke-WebRequest -Uri http://localhost:5000/api/posts `
    -Method POST `
    -ContentType "application/json" `
    -UseBasicParsing `
    -Body '{"title":"Pilny BLIK","summary":"Konto zablokowane.","content":"Wyslij kod BLIK 123456 natychmiast i kliknij teraz.","author":"Scammer"}'
} catch {
  if ($_.Exception.Response) {
    [int]$_.Exception.Response.StatusCode
    $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
    $reader.ReadToEnd()
  } else {
    "Brak polaczenia z serwerem"
  }
}
```

Oczekiwany wynik: `422`. Wpis nie zostaje zapisany, a odpowiedz zawiera `aiExplanation` wygenerowane przez `antiscam/ai.py`.

## Demo 3: sprawdzenie, ze wpis nie istnieje

```powershell
try {
  Invoke-WebRequest -Uri http://localhost:5000/api/posts/pilny-blik -UseBasicParsing
} catch {
  if ($_.Exception.Response) {
    [int]$_.Exception.Response.StatusCode
  } else {
    "Brak polaczenia z serwerem"
  }
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
  -UseBasicParsing `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: wysoki wynik ryzyka.

## Demo 5: Python AI explain

```powershell
Invoke-WebRequest -Uri http://localhost:8000/ai/explain `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: raport AI/NLP z `blocked_after_scan`, `block_explanation`, `scan_reasons` i zaleceniem bezpiecznej reakcji.
