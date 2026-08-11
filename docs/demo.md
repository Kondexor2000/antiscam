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

## Automatyczne wykonanie demonstracji

Skrypt `Invoke-Demo.ps1` uruchamia na tymczasowej bazie SQLite scenariusze C# z
Demo 1-8. Sprawdza odpowiedzi HTTP i przerywa dzialanie przy pierwszej
niezgodnosci.

```powershell
.\Invoke-Demo.ps1
```

Wariant uruchomienia na innym porcie:

```powershell
.\Invoke-Demo.ps1 -CSharpPort 5000
```

## Testy C#

Testy .NET sa zapisane w projekcie `tests/AntiScam.Blog.Api.Tests` i korzystaja
z xUnit. Nie wymagaja uruchomionego serwera: testy integracyjne startuja API w
pamieci, tworza osobna tymczasowa baze SQLite oraz zastepuja integracje AI i
MongoDB deterministycznymi implementacjami testowymi.

Uruchom wszystkie 25 przypadkow testowych:

```powershell
dotnet test AntiScamBlog.sln
```

Mozna tez uruchomic tylko wybrana grupe:

```powershell
dotnet test tests\AntiScam.Blog.Api.Tests\AntiScam.Blog.Api.Tests.csproj --filter "FullyQualifiedName~Unit"
dotnet test tests\AntiScam.Blog.Api.Tests\AntiScam.Blog.Api.Tests.csproj --filter "FullyQualifiedName~Integration"
```

### Testy jednostkowe (13 przypadkow)

| Obszar | Testowane zachowanie |
| --- | --- |
| `AesGcmAuthenticatedEncryptor` | Szyfrowanie i odszyfrowanie zwraca pierwotna tresc oraz oznacza algorytm AES-GCM. |
| `AesGcmAuthenticatedEncryptor` | Zmiana danych dodatkowych (AAD) powoduje `CryptographicException`. |
| `BlogPostValidator` | Brak tytulu, opisu, tresci i autora zwraca bledy walidacji dla wszystkich pol. |
| `BlogPostValidator` | Kompletny wpis nie zwraca bledow walidacji. |
| `RiskAnalyzer` | Zwykly wpis edukacyjny otrzymuje status `LOW RISK` i moze zostac opublikowany. |
| `RiskAnalyzer` | Wiadomosc z kodem BLIK jest oznaczona jako `HIGH RISK`, zablokowana i zawiera powod `BLIK CONFIRMED`. |
| `RiskAnalyzer` | Obfuskowany zapis `B L I K` oraz `k-o-d` jest normalizowany i blokowany. |
| `RiskAnalyzer` | Podszywanie sie pod zaufana domene i literowka `g00gle.com` sa wykrywane jako ryzykowne linki. |
| `SecurePasswordHasher` | Haslo zahaszowane przez serwis przechodzi weryfikacje i ma oczekiwany algorytm. |
| `SecurePasswordHasher` | Nieprawidlowe haslo nie przechodzi weryfikacji. |
| `SlugGenerator` | Polski tytul jest zamieniany na przyjazny adres URL. |
| `SlugGenerator` | Spacje, interpunkcja i znaki specjalne sa usuwane z adresu URL. |
| `SlugGenerator` | Pusty tytul zwraca domyslny slug `post`. |

### Testy integracyjne API (12 przypadkow)

| Endpoint lub obszar | Testowane zachowanie |
| --- | --- |
| `GET /api/posts` | Zwraca co najmniej dwa wpisy startowe. |
| `GET /api/posts/latest` | Zwraca najnowszy wpis z listy wpisow. |
| `GET /api/storage` | Raportuje SQLite jako magazyn glowny oraz aktywny MongoDB jako magazyn incydentow. |
| `GET /api/incidents` | Z testowym magazynem incydentow zwraca pusta liste. |
| `POST /api/posts` | Poprawny wpis zwraca `201 Created`, otrzymuje slug i jest dostepny przez `GET`. |
| `POST /api/posts` | Wpis bez tytulu zwraca `400 Bad Request`. |
| `POST /api/posts` | Wpis z oszustwem BLIK zwraca `422`, zawiera ocene i wyjasnienie AI, a wpis nie jest zapisywany. |
| `POST /api/posts` | Obfuskowany BLIK i link `g00gle.com` zwracaja `422`, a wpis nie jest zapisywany. |
| `POST /api/posts/{id}/comments` | Bezpieczny komentarz zwraca `201 Created` i jest dostepny przez `GET`. |
| `POST /api/posts/{id}/comments` | Komentarz scamowy zwraca `422` i nie pojawia sie na liscie komentarzy. |
| `GET /` | Strona startowa zwraca oczekiwany statyczny HTML oraz link do wszystkich wpisow. |
| Administracja i uwierzytelnianie | Administrator moze zablokowac uzytkownika, usunac wpis programowo oraz go przywrocic; zablokowany uzytkownik nie moze sie zalogowac. |

Nastepnie uruchom blog API:

```powershell
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj --urls http://0.0.0.0:5000
```

Serwer C# nasluchuje wtedy na wszystkich interfejsach sieciowych pod adresem
`0.0.0.0:5000`. Na komputerze serwera strona demo jest dostepna pod adresem
`http://localhost:5000/`; z drugiego urzadzenia w tej samej sieci nalezy uzyc
`http://ADRES_IP_SERWERA:5000/`. Nie nalezy wpisywac `0.0.0.0` jako adresu w
przegladarce lub w `Invoke-WebRequest` — jest to adres nasluchu, nie cel polaczenia.
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

## Demo 5: stan API i magazynow C#

Ponizsze zapytania pokazuja stan aplikacji oraz konfiguracje magazynow. SQLite
przechowuje wpisy, uzytkownikow i sesje; MongoDB, jesli jest dostepny, przechowuje
incydenty zablokowanych wpisow.

```powershell
Invoke-RestMethod -Uri http://localhost:5000/api/health
Invoke-RestMethod -Uri http://localhost:5000/api/storage
Invoke-RestMethod -Uri "http://localhost:5000/api/incidents?limit=10"
Invoke-RestMethod -Uri http://localhost:5000/api/workspace
```

Oczekiwany wynik: zdrowie API ma status `ok`, a `/api/storage` wskazuje SQLite
jako magazyn podstawowy. Lista incydentow moze byc pusta, gdy MongoDB nie jest
uruchomione albo nie zablokowano jeszcze zadnego wpisu.

## Demo 6: rejestracja, logowanie i sesja C#

Pierwsze konto w nowej bazie danych otrzymuje role `Admin`. Aby bezpiecznie
zademonstrowac role administratora bez zmiany stalej bazy, uruchom serwer w
osobnym terminalu z nowa, unikalna baza:

```powershell
$env:ANTISCAM_BLOG_DB = Join-Path $PWD ("data\antiscam-demo-{0}.sqlite" -f [guid]::NewGuid().ToString("N"))
dotnet run --project src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj --urls http://0.0.0.0:5000
```

W terminalu z poleceniami demonstracyjnymi zarejestruj administratora i zwyklego
czytelnika, a nastepnie pobierz token sesji administratora:

```powershell
$admin = Invoke-RestMethod -Uri http://localhost:5000/api/auth/register -Method POST -ContentType "application/json" -Body '{"userName":"administrator-demo","password":"StrongPassword123!"}'
$reader = Invoke-RestMethod -Uri http://localhost:5000/api/auth/register -Method POST -ContentType "application/json" -Body '{"userName":"czytelnik-demo","password":"AnotherStrongPassword123!"}'
$login = Invoke-RestMethod -Uri http://localhost:5000/api/auth/login -Method POST -ContentType "application/json" -Body '{"userName":"administrator-demo","password":"StrongPassword123!"}'
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$login.user
```

Oczekiwany wynik: `administrator-demo` ma role `Admin`, a odpowiedz logowania
zawiera `accessToken`. Hasla nie sa zwracane przez API ani przechowywane w formie
tekstowej.

## Demo 7: moderacja administratora C#

Token z poprzedniego kroku pozwala pobrac konta i wpisy, zablokowac czytelnika
oraz wykonac programowe usuniecie i przywrocenie wpisu. Polecenia wykorzystuja
utworzona w poprzednim kroku zmienna `$headers`. Id czytelnika pobieramy z API,
zamiast polegac na zmiennej `$reader` z poprzedniego bloku.

```powershell
$users = Invoke-RestMethod -Uri http://localhost:5000/api/admin/users -Headers $headers
Invoke-RestMethod -Uri http://localhost:5000/api/admin/posts -Headers $headers

$reader = $users | Where-Object { $_.userName -eq "czytelnik-demo" } | Select-Object -First 1
if ($null -eq $reader) {
  throw "Nie znaleziono konta czytelnik-demo. Wykonaj najpierw Demo 6 na tej samej bazie."
}
if ($reader.isBlocked) {
  Invoke-WebRequest -Uri "http://localhost:5000/api/admin/users/$($reader.id)/unblock" -Method POST -Headers $headers -UseBasicParsing
}
Invoke-WebRequest -Uri "http://localhost:5000/api/admin/users/$($reader.id)/block" -Method POST -Headers $headers -UseBasicParsing
try {
  Invoke-WebRequest -Uri http://localhost:5000/api/auth/login -Method POST -ContentType "application/json" -Body '{"userName":"czytelnik-demo","password":"AnotherStrongPassword123!"}' -UseBasicParsing
} catch {
  [int]$_.Exception.Response.StatusCode
}
```

Oczekiwany wynik: blokada zwraca `204 No Content`, a logowanie zablokowanego konta
zwraca `403 Forbidden`. Aby pokazac programowe usuwanie i odtworzenie wpisu,
pobierz jego identyfikator, a potem wykonaj:

```powershell
$post = (Invoke-RestMethod -Uri http://localhost:5000/api/posts)[0]
Invoke-WebRequest -Uri "http://localhost:5000/api/posts/$($post.id)" -Method DELETE -Headers $headers -UseBasicParsing
try {
  Invoke-RestMethod -Uri http://localhost:5000/api/posts/$($post.slug)
} catch {
  [int]$_.Exception.Response.StatusCode
}
Invoke-WebRequest -Uri "http://localhost:5000/api/admin/posts/$($post.id)/restore" -Method POST -Headers $headers -UseBasicParsing
Invoke-RestMethod -Uri http://localhost:5000/api/posts/$($post.slug)
```

Oczekiwany wynik: usuniecie zwraca `204`, odczyt usunietego wpisu `404`, a po
przywroceniu wpis ponownie jest dostepny. Mozna rowniez odblokowac konto przez
`POST /api/admin/users/{id}/unblock` z tym samym naglowkiem autoryzacji.

## Demo 8: aktualizacja wpisu i wylogowanie C#

Aktualizacja wykorzystuje walidacje i analize ryzyka tak samo jak tworzenie
wpisu. Uzyj istniejacego identyfikatora wpisu w zmiennej `$post`:

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/posts/$($post.id)" -Method PUT -ContentType "application/json" -Body '{"title":"Zaktualizowane zasady bezpieczenstwa","summary":"Krotka aktualizacja.","content":"Nie podawaj nikomu kodow autoryzacyjnych i weryfikuj nadawce.","author":"AntiScam Team"}'
Invoke-WebRequest -Uri http://localhost:5000/api/auth/logout -Method POST -Headers $headers -UseBasicParsing
try {
  Invoke-RestMethod -Uri http://localhost:5000/api/admin/users -Headers $headers
} catch {
  [int]$_.Exception.Response.StatusCode
}
```

Oczekiwany wynik: aktualizacja zwraca wpis ze zmienionymi danymi, wylogowanie
zwraca `204 No Content`, a ponowne uzycie tokenu dla endpointu administratora
zwraca `401 Unauthorized`.

## Demo 9: automatyczny szyfrowany backup po zmianie IP C#

Automatyczna wersja tej demonstracji korzysta z dwoch roznych adresow loopback i
nie zmienia danych projektu:

```powershell
.\Invoke-Demo-Backup.ps1
```

Opcja `-KeepArtifacts` pozostawia tymczasowy katalog z zaszyfrowanym backupem do
inspekcji. Ponizsza procedura z dwoma urzadzeniami pozostaje przydatna jako test
rzeczywistej konfiguracji sieciowej.

Ten scenariusz wymaga dwoch urzadzen w tej samej sieci, np. komputera serwera
i telefonu lub drugiego laptopa. Aplikacja porownuje adres IP klienta z adresami
zapisanymi w poprzednich sesjach: pierwsze logowanie nie tworzy kopii, a kolejne
logowanie tego samego konta z innego IP tworzy backup SQLite zaszyfrowany AES-GCM.

Na komputerze serwera sprawdz jego adres LAN i stan plikow backupu:

```powershell
Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "169.254*" -and $_.IPAddress -ne "127.0.0.1" }
Get-ChildItem .\secure_backups\backup.enc.json, .\secure_backups\backup_meta.json -ErrorAction SilentlyContinue | Select-Object Name, Length, LastWriteTimeUtc
```

Na pierwszym urzadzeniu zarejestruj konto i zaloguj sie pierwszy raz. Zamiast
`ADRES_IP_SERWERA` wpisz adres LAN pokazany w poprzednim poleceniu:

```powershell
Invoke-RestMethod -Uri http://ADRES_IP_SERWERA:5000/api/auth/register `
  -Method POST -ContentType "application/json" `
  -Body '{"userName":"backup-demo","password":"StrongPassword123!"}'

Invoke-RestMethod -Uri http://ADRES_IP_SERWERA:5000/api/auth/login `
  -Method POST -ContentType "application/json" `
  -Body '{"userName":"backup-demo","password":"StrongPassword123!"}'
```

Na drugim urzadzeniu (z innym adresem IP) wykonaj tylko logowanie tym samym kontem:

```powershell
Invoke-RestMethod -Uri http://ADRES_IP_SERWERA:5000/api/auth/login `
  -Method POST -ContentType "application/json" `
  -Body '{"userName":"backup-demo","password":"StrongPassword123!"}'
```

Ponownie na komputerze serwera sprawdz pliki:

```powershell
Get-ChildItem .\secure_backups\backup.enc.json, .\secure_backups\backup_meta.json | Select-Object Name, Length, LastWriteTimeUtc
Get-Content .\secure_backups\backup_meta.json
```

Oczekiwany wynik: po drugim logowaniu pojawia sie albo otrzymuje nowszy znacznik
czasu plik `backup.enc.json`, obok niego istnieje `backup_meta.json` z algorytmem
`AES-GCM-256`. Nie otwieraj `backup.enc.json` jako danych aplikacji: zawiera
zaszyfrowany ladunek kopii. Jezeli oba urzadzenia maja ten sam adres IP (np. przez
ten sam VPN/proxy), automatyzacja celowo nie uruchomi backupu.

## Demo 10: Python API

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

## Demo 11: Hybryda ML + twarde reguly

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Konto zablokowane, kliknij https://g00gle.com/login i potwierdz kod BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: `HIGH RISK`. Model ML nadaje bazowy wynik intencji, a reguly BLIK,
podejrzanego linku i typosquattingu dzialaja jako twarde modyfikatory wyniku.

## Demo 12: Normalizacja obfuskacji

```powershell
Invoke-WebRequest -Uri http://localhost:8000/scan `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"B L I K 123456 k-o-d natychmiast"}'
```

Oczekiwany wynik: `HIGH RISK`. `normalization.py` laczy rozstrzelone litery,
usuwa proste znaki wstawione w slowa i przekazuje oczyszczony tekst do `engine.py`.

## Demo 13: Bezpieczne wycinanie domen i literowki

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

## Demo 14: Python AI explain

```powershell
Invoke-WebRequest -Uri http://localhost:8000/ai/explain `
  -Method POST `
  -ContentType "application/json" `
  -UseBasicParsing `
  -Body '{"text":"Wyslij BLIK 123456 natychmiast"}'
```

Oczekiwany wynik: raport AI/NLP z `blocked_after_scan`, `block_explanation`, `scan_reasons` i zaleceniem bezpiecznej reakcji.
