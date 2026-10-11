# Plan testów QA

Dokument opisuje powtarzalny test projektu AntiScam dla QA: testy automatyczne,
smoke testy oraz ręczne scenariusze akceptacyjne dla API Python i C#.
Komendy są przeznaczone dla Windows PowerShell i należy uruchamiać je z katalogu
głównego repozytorium.

## Zakres i wymagania

Projekt składa się z Python AntiScam API (FastAPI) oraz C# Blog/API Platform
(ASP.NET Core i SQLite). Do testów potrzebne są:

- Python 3.10 lub nowszy oraz `pip`,
- .NET SDK 8.0 lub nowszy,
- PowerShell.

MongoDB i zewnętrzna usługa AI nie są wymagane do automatycznych testów. Testy
.NET uruchamiają API w pamięci, używają tymczasowej bazy SQLite i zastępują
integracje testowymi implementacjami.

## Przygotowanie środowiska

Utwórz lokalne środowisko Python i zainstaluj zależności:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements-dev.txt
python -m pip install -e .
```

Przy blokadzie uruchamiania skryptów aktywacja nie jest wymagana — użyj
bezpośrednio `.\.venv\Scripts\python.exe` zamiast `python`.

Przywróć zależności .NET:

```powershell
dotnet restore AntiScamBlog.sln
```

## Testy automatyczne

Uruchom oba zestawy niezależnie, aby łatwo wskazać warstwę, w której wystąpił
błąd:

```powershell
python -m pytest
dotnet test AntiScamBlog.sln
```

Testy Python obejmują walidację API, klasyfikację ryzyka, normalizację,
analizę linków, modele AI/ML i scenariusze złośliwych danych. Testy .NET
obejmują logikę jednostkową oraz integrację API, repozytorium SQLite,
publikowanie wpisów i komentarzy, uwierzytelnianie, Control Plane i Gateway.

Do diagnozowania regresji można uruchamiać wybrane warstwy:

```powershell
python -m pytest tests\unit
python -m pytest tests\integration
dotnet test tests\AntiScam.Blog.Api.Tests\AntiScam.Blog.Api.Tests.csproj --filter "FullyQualifiedName~Unit"
dotnet test tests\AntiScam.Blog.Api.Tests\AntiScam.Blog.Api.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## Smoke test C#

Skrypt uruchamia rzeczywiste API C# na osobnej, tymczasowej bazie SQLite,
sprawdza podstawowe przepływy HTTP i domyślnie zatrzymuje serwer oraz usuwa
utworzone pliki tymczasowe:

```powershell
.\Invoke-Demo.ps1
```

Jeśli domyślny port jest zajęty, wybierz wolny:

```powershell
.\Invoke-Demo.ps1 -CSharpPort 5010
```

Nie używaj `-KeepServers`, jeśli nie chcesz pozostawić uruchomionego serwera
i artefaktów demonstracji.

## Ręczne testy akceptacyjne

### Uruchom aplikacje na danych QA

W pierwszym terminalu PowerShell ustaw osobną bazę oraz losowy klucz
administracyjny, a następnie uruchom API C#:

```powershell
$env:ANTISCAM_BLOG_DB = Join-Path $env:TEMP ("antiscam-qa-" + [guid]::NewGuid().ToString("N") + ".sqlite")
$env:ANTISCAM_CONTROL_PLANE_KEY = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj --urls http://localhost:5000
```

W drugim terminalu, z aktywnym środowiskiem `.venv`, uruchom API Python:

```powershell
uvicorn antiscam.api:app --reload --port 8000
```

Nie korzystaj z rzeczywistych wiadomości ani produkcyjnych baz. Klucz
administracyjny traktuj jak sekret i nie umieszczaj go w zgłoszeniu błędu.

### C# Blog/API Platform

Wysyłaj zapytania do `http://localhost:5000` i porównuj wynik z kryteriami:

| ID | Scenariusz | Oczekiwany wynik |
| --- | --- | --- |
| C01 | `GET /api/health` | `200`; pole `status` ma wartość `ok`. |
| C02 | `GET /api/workspace` | `200`; `exists` ma wartość `true`, a `databasePath` wskazuje tymczasową bazę QA. |
| C03 | `GET /api/storage` | `200`; magazyn podstawowy to SQLite. |
| C04 | `GET /api/posts` | `200`; lista zawiera co najmniej dwa wpisy startowe. |
| C05 | Utworzenie wpisu z neutralną treścią przez `POST /api/posts` | `201`; odpowiedź zawiera `slug`, a `GET /api/posts/{slug}` zwraca utworzony wpis. |
| C06 | Utworzenie wpisu zawierającego żądanie kodu BLIK przez `POST /api/posts` | `422`; wpis nie pojawia się na liście. |
| C07 | Brak klucza lub nieprawidłowy klucz przy `GET /api/gateway/apis` | Dostęp jest odrzucony; poprawny klucz to nagłówek `X-Control-Plane-Key` z wartością ustawioną przy uruchomieniu. |
| C08 | Strona główna `/` oraz widok wszystkich wpisów `/?view=all` | Oba widoki ładują się; na stronie wpisów można otworzyć wpis i wysłać komentarz. |

Przykładowe kontrole stanu i danych:

```powershell
$baseUrl = "http://localhost:5000"
Invoke-RestMethod -Uri "$baseUrl/api/health"
Invoke-RestMethod -Uri "$baseUrl/api/workspace"
Invoke-RestMethod -Uri "$baseUrl/api/storage"
Invoke-RestMethod -Uri "$baseUrl/api/posts"
```

Przykładowe utworzenie bezpiecznego wpisu:

```powershell
$post = @{
    title = "QA - bezpieczny wpis"
    summary = "Scenariusz testowy"
    content = "Weryfikuj nadawcę i nie podawaj kodów autoryzacyjnych."
    author = "QA"
} | ConvertTo-Json

$created = Invoke-RestMethod -Uri "$baseUrl/api/posts" -Method POST -ContentType "application/json" -Body $post
Invoke-RestMethod -Uri "$baseUrl/api/posts/$($created.slug)"
```

### Python AntiScam API

Wysyłaj zapytania do `http://localhost:8000` i sprawdzaj:

| ID | Scenariusz | Oczekiwany wynik |
| --- | --- | --- |
| P01 | `GET /` | `200`; odpowiedź potwierdza działanie API. |
| P02 | `POST /scan` z neutralnym tekstem, np. `Cześć, spotkamy się o trzeciej.` | `200`; niski wynik ryzyka i brak ryzykownych linków. |
| P03 | `POST /scan` z tekstem `Wyślij kod BLIK 123456 natychmiast!` | `200`; status `HIGH RISK`, wynik ryzyka w zakresie 0–100. |
| P04 | `POST /scan` z obfuskowanym BLIK, np. `B L I K 123456 k-o-d natychmiast` | `200`; wiadomość nadal zostaje oznaczona jako ryzykowna. |
| P05 | `POST /scan` z adresem `https://g00gle.com/login` | `200`; adres znajduje się w `risky_links`. |
| P06 | `POST /scan` z pustym obiektem JSON | `422`; niepoprawne żądanie nie kończy się błędem serwera `5xx`. |
| P07 | `POST /ai/explain` z wiadomością scamową | `200`; raport zawiera `intent`, `scan_status`, `suggested_action` i wyjaśnienie blokady. |

Przykładowe żądania:

```powershell
$scan = @{ text = "Wyślij kod BLIK 123456 natychmiast!" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8000/scan" -Method POST -ContentType "application/json" -Body $scan

$explanation = @{ text = "Boję się, bank chce kod BLIK 123456 pilnie" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8000/ai/explain" -Method POST -ContentType "application/json" -Body $explanation
```

### Gateway — opcjonalnie

Ten przepływ wymaga działających obu API. Zarejestruj Python API w panelu
Control Plane, używając `upstreamUrl` równemu `http://localhost:8000`, i zachowaj
zwrócony klucz klienta wyłącznie na czas testu. Zweryfikuj:

1. Żądanie do `/gateway/{id}/scan` bez `X-API-Key` jest odrzucane.
2. To samo żądanie z poprawnym `X-API-Key` zwraca wynik Python `/scan`.
3. Wyłączenie rejestracji API blokuje dalsze przekazywanie żądań.
4. Po rotacji klucza poprzedni klucz przestaje działać, a nowy działa.
5. Błędny lub niedostępny upstream zwraca błąd Gateway, a nie udawaną
   odpowiedź sukcesu.

Testy Gateway wykonuj wyłącznie na lokalnym środowisku i usuń testową
rejestrację po zakończeniu.

## Zakończenie testów i zgłaszanie błędów

Po zatrzymaniu obu serwerów można usunąć tylko tymczasową bazę wskazaną w
`$env:ANTISCAM_BLOG_DB`. Nie usuwaj domyślnej bazy projektu.

Zgłoszenie defektu powinno zawierać: system operacyjny, wersje Python/.NET,
identyfikator scenariusza, kroki odtworzenia, wynik oczekiwany i rzeczywisty
oraz status HTTP/fragment odpowiedzi po usunięciu sekretów i danych osobowych.
