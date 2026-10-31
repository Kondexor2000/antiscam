# Raport projektowy: AntiScam

## Cel projektu

AntiScam jest projektem demonstracyjno-laboratoryjnym pokazujacym, jak wykrywac ryzykowne komunikaty phishingowe, jak blokowac publikacje tresci o wysokim ryzyku oraz jak dokumentowac i testowac bezpieczna implementacje uslugi.

Projekt sklada sie z dwoch czesci:

- Python/FastAPI: silnik oceny ryzyka wiadomosci.
- C# ASP.NET Core WebAPI: blog edukacyjny z HTML, SQL Server i blokada publikacji tresci ryzykownych.

## Zakres bezpieczenstwa

Projekt obejmuje:

- wykrywanie kodow BLIK i manipulacyjnego kontekstu,
- analize linkow i domen,
- scoring slow kluczowych,
- walidacje danych wejsciowych,
- blokowanie publikacji wpisow `MEDIUM RISK` i `HIGH RISK`,
- parametryzowane zapytania SQL Server,
- escaping tresci w frontendzie,
- przyklady kryptografii: PBKDF2-HMAC-SHA256 oraz AES-GCM-256,
- testy jednostkowe i integracyjne.

## Architektura

Pythonowy silnik AntiScam (`antiscam/`) odpowiada za lekka analize tekstu i endpoint `/scan`. Czesc C# (`src/AntiScam.Blog.Api/`) udostepnia blog, statyczne pliki HTML/CSS/JS, REST API oraz repozytorium SQL Server.

Ryzykowny wpis blogowy nie jest zapisywany. API zwraca `422 Unprocessable Entity` z wynikiem oceny ryzyka, co jest testowane w `tests/AntiScam.Blog.Api.Tests/Integration/BlogApiTests.cs`.

## Kryptografia

Modul `src/AntiScam.Blog.Api/Security/` zawiera dwa kontrolowane przyklady:

- `AesGcmAuthenticatedEncryptor`: szyfrowanie uwierzytelnione AES-GCM-256 z losowym nonce i dodatkowo uwierzytelnianymi danymi.

Klucze szyfrujace nie sa przechowywane w repozytorium. W scenariuszu produkcyjnym powinny pochodzic z menedzera sekretow lub zmiennych srodowiskowych.

## Wyniki testow

Weryfikacja lokalna:

```powershell
dotnet test AntiScamBlog.sln
python -m pytest
```

Oczekiwany wynik po aktualizacji:

- testy C#: obejmuja walidacje, slugi, ocene ryzyka, blokade publikacji, SQL Server, HTML i kryptografie,
- testy Python: obejmuja scoring, modele, linki, API i workflow.

## Ograniczenia

Silnik oceny ryzyka jest deterministyczny i edukacyjny. Nie zastępuje profesjonalnego systemu antyfraudowego ani aktualizowanej bazy reputacji URL. Projekt celowo nie wykonuje prawdziwych atakow i nie sluzy do omijania zabezpieczen.

## Aspekty prawne i etyczne

Demonstracje powinny odbywac sie na danych testowych, bez prawdziwych danych osobowych, hasel, tokenow i aktywnych linkow do zlosliwych domen. Laboratoria pokazuja bezpieczne wzorce obronne i nie zawieraja instrukcji szkodliwego wykorzystania podatnosci poza kontrolowanym scenariuszem edukacyjnym.

## Dalszy rozwoj

- integracja z aktualizowana lista reputacji domen,
- role i uwierzytelnianie dla panelu publikacji,
- raport HTML/PDF z wynikow skanowania,
- automatyczny pipeline CI z testami i audytem zaleznosci.

## Plan weryfikacji jakosci i odpornosci

Aby potwierdzic gotowosc projektu w trudniejszych scenariuszach, dalsza weryfikacja powinna objac cztery obszary:

### Wydajnosc

- Zmierzyc czas odpowiedzi, przepustowosc i zuzycie zasobow dla typowego oraz szczytowego obciazenia.
- Wykonac testy obciazeniowe i dlugotrwale dla endpointu `/scan` oraz API bloga.
- Ustalic progi akceptacji, np. maksymalny czas odpowiedzi p95 przy okreslonej liczbie rownoleglych zadan.
- Zidentyfikowac waskie gardla w analizie tekstu, komunikacji miedzy uslugami i dostepie do SQL Server.

### Niezawodnosc

- Sprawdzic zachowanie przy timeoutach, ponawianiu zadan, niedostepnosci SQL Server lub silnika oceny ryzyka oraz po restarcie uslug.
- Zweryfikowac, ze awaria zaleznosci nie powoduje utraty ani niekontrolowanego zapisu danych i jest sygnalizowana czytelnym bledem.
- Opisac i sprawdzic kopie zapasowe oraz odtwarzanie danych.
- Zweryfikowac monitoring, logowanie bledow i alerty dla awarii oraz pogorszenia czasu odpowiedzi.

### Jakosc architektury

- Przejrzec granice odpowiedzialnosci miedzy silnikiem Python, API ASP.NET Core, frontendem i repozytorium SQL Server.
- Sprawdzic zaleznosci miedzy komponentami, obsluge bledow i mozliwosc niezaleznego testowania oraz rozwijania modulow.
- Przesledzic kluczowe przeplywy end-to-end: skanowanie wiadomosci oraz probę publikacji ryzykownego wpisu.
- Oceniac zmiany pod katem skalowania, ryzyka efektow ubocznych i zgodnosci kontraktow API.

### Bezpieczenstwo w trudniejszych scenariuszach

- Przejrzec uprawnienia, konfiguracje sekretow, walidacje danych wejsciowych, izolacje danych uzytkownikow oraz wrazliwe informacje w logach.
- Testowac prompt injection i proby wyciagania danych, jesli do projektu zostanie dodany model jezykowy lub komponent korzystajacy z instrukcji uzytkownika.
- Sprawdzic naduzycia endpointow i narzedzi, w tym niebezpieczne lub powtorzone dzialania oraz niepoprawne i zlosliwe pliki, jesli obsluga plikow zostanie wdrozona.
- Potwierdzic, ze bledy i nietypowe wejscia nie prowadza do ujawnienia danych, obejscia blokady publikacji ani niekontrolowanego wykonania operacji.

Dla kazdego obszaru nalezy zapisac scenariusze, mierniki i kryteria zaliczenia. Przykladowe kryteria to: czas p95 ponizej ustalonego limitu przy zadanym obciazeniu, brak utraty danych po awarii, kontrolowane zachowanie przy niedostepnej zaleznosci oraz brak dostepu do danych spoza zakresu uprawnien.
