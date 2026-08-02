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

## Demo 4: testowanie komentarzy

Na stronie glownej jest widoczny tylko najnowszy wpis. Kliknij **Zobacz wszystkie posty**,
aby otworzyc `http://localhost:5000/?view=all`; pod kazdym wpisem znajduje sie formularz komentarza.

Mozna tez przetestowac API bezposrednio. Najpierw pobierz identyfikatory wpisow:

```powershell
Invoke-RestMethod -Uri http://localhost:5000/api/posts
```

W ponizszych poleceniach zamien `1` na istniejace `id` wpisu.

Bezpieczny komentarz powinien zostac zapisany:

```powershell
Invoke-WebRequest -Uri http://localhost:5000/api/posts/1/comments `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"content":"Dziekuje za przydatne wskazowki.","author":"Czytelnik"}'
```

Oczekiwany wynik: `201 Created`. Komentarz jest widoczny po odswiezeniu strony
oraz w odpowiedzi `GET /api/posts/1/comments`.

Komentarz scamowy powinien zostac zablokowany przez ten sam algorytm co wpisy:

```powershell
try {
  Invoke-WebRequest -Uri http://localhost:5000/api/posts/1/comments `
    -Method POST `
    -ContentType "application/json" `
    -UseBasicParsing `
    -Body '{"content":"Wyslij kod BLIK 123456 natychmiast.","author":"Oszust"}'
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

Oczekiwany wynik: `422`. Komentarz nie jest zapisywany, a odpowiedz zawiera ocene
`risk` wraz z powodami blokady, np. `BLIK CONFIRMED`.

## Demo 5: Python API

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

Oczekiwany wynik: wysoki wynik ryzyka. W polu `reasons` widoczny jest tez bazowy wynik
`ML intent score`, wyliczony przez hybrydowy pipeline TF-IDF + Naive Bayes.

## Demo 6: Hybryda ML + twarde reguly

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Konto zablokowane, kliknij https://g00gle.com/login i potwierdz kod BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: `HIGH RISK`. Model ML nadaje bazowy wynik intencji, a reguly BLIK,
podejrzanego linku i typosquattingu dzialaja jako twarde modyfikatory wyniku.

## Demo 7: Normalizacja obfuskacji

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"B L I K 123456 k-o-d natychmiast"}'
```

Oczekiwany wynik: `HIGH RISK`. `normalization.py` laczy rozstrzelone litery,
usuwa proste znaki wstawione w slowa i przekazuje oczyszczony tekst do `engine.py`.

## Demo 8: Bezpieczne wycinanie domen i literowki

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Nie loguj sie przez https://google.com.evil.example ani https://g00gle.com/login"}'
```

Oczekiwany wynik: `HIGH RISK`. `links.py` uzywa `tldextract`, wiec
`google.com.evil.example` nie jest traktowane jak zaufane `google.com`, a
`g00gle.com` trafia do `Typosquatting links` dzieki odleglosci Levenshteina.

## Demo 9: Python AI explain

```powershell
Invoke-WebRequest -Uri http://localhost:8000/ai/explain `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: raport AI/NLP z `blocked_after_scan`, `block_explanation`, `scan_reasons` i zaleceniem bezpiecznej reakcji.
