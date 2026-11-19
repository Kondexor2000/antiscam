# Desk research: analiza statystyczna zagrożeń SMS i phishingu

## Zmieniony temat projektu

**Statystyczna analiza publicznych danych o spamie SMS, phishingu i podejrzanych adresach URL oraz ich zastosowanie w edukacji o oszustwach BLIK.** Projekt wykorzystuje istniejące zbiory danych i publiczne raporty. Nie wymaga ankiety ani zbierania danych od respondentów. Zastępuje temat o obowiązkach domowych.

Jednostkami analizy będą rekordy gotowych zbiorów: wiadomości SMS, adresy URL i opisane w raportach incydenty. Wyniki pokażą rozkłady i zależności w tych konkretnych źródłach, a nie częstość wszystkich oszustw wśród Polaków.

## Cel i pytania badawcze

**Cel:** opisać, jakie statystyki o spamie SMS i phishingu zawierają publiczne dane, sprawdzić, jak wybrane cechy różnią się między klasami oraz ocenić, czego te dane nie pozwalają stwierdzić o oszustwach z użyciem BLIK.

Pytania:

1. Jaki jest rozkład klas spam/ham w publicznym zbiorze SMS?
2. Jak rozkładają się etykiety phishing/legitimate w zbiorach stron i URL-i?
3. Czy wybrane cechy numeryczne różnią się pomiędzy etykietami w każdym zbiorze?
4. Czy wybrane cechy URL są statystycznie powiązane z etykietą klasy?
5. Jak zmieniały się zgłoszenia phishingu i oszustw SMS w opublikowanych raportach rocznych oraz czy dostępne źródła pozwalają osobno wyznaczyć trend oszustw BLIK?

Pytanie 5 ma charakter desk research. Należy przepisać wartości z raportów wraz z definicją wskaźnika, rokiem, jednostką i źródłem. Jeśli nie ma porównywalnego rocznego szeregu danych o BLIK, nie należy wyliczać trendu ani porównywać go liczbowo z phishingiem.

## Hipotezy do sprawdzenia na zbiorach

**Porównanie cech URL między klasami:**

- H0: wybrana cecha URL (np. długość adresu) ma taki sam rozkład w klasie phishingowej i legalnej.
- H1: rozkłady tej cechy różnią się między klasami.

**Związek cech URL:**

- H0: korelacja między wybranymi cechami numerycznymi adresów URL wynosi zero.
- H1: korelacja jest różna od zera.

Hipotezy należy określić przed analizą, wskazując konkretne cechy i zbiór. Testowanie wielu cech wymaga korekty wielokrotnych porównań, np. metodą Holma. Dla porównania historii BLIK i phishingu nie stawia się hipotezy o „nowości” BLIK, dopóki nie zostanie znaleziony spójny szereg czasowy z porównywalnymi definicjami.

## Źródła i zakres danych

### Zbiory lokalne projektu

| Źródło | Jednostka i dostępne informacje | Zastosowanie i ograniczenie |
| --- | --- | --- |
| `uci_sms_spam_collection.csv` | 5 572 niepuste wiadomości w eksporcie; etykiety spam/ham | Rozkład klas i analiza wybranych cech tekstu, np. długości wiadomości. Zbiór jest anglojęzyczny i nie zawiera osobnej etykiety BLIK. |
| `uci_phishing_websites.csv` | 11 055 rekordów witryn, cechy tabelaryczne, kolumna celu `result` | Rozkład etykiet i porównanie cech witryn między klasami. To dane o witrynach, nie polskich SMS-ach. |
| `uci_phiusiil_phishing_url.csv` | 235 795 URL-i i cech; `label=1` oznacza legalny URL, `label=0` phishing | Duży zbiór do analizy rozkładów i porównań cech. Etykietę trzeba interpretować zgodnie z dokumentacją UCI. |
| Raporty CERT Polska | Roczne agregaty zgłoszeń i incydentów; definicje i sposób raportowania zależą od raportu | Desk research o trendach w phishingu/SMS-ach. Zmiany kanałów zgłaszania, np. uruchomienie numeru 8080, mogą powodować nieporównywalność liczby zgłoszeń między latami. |
| Raporty BLIK, Policji i instytucji publicznych | Dane o transakcjach, oszustwach lub postępowaniach, zależnie od publikacji | Wykorzystywać tylko te wskaźniki, które są bezpośrednio o oszustwach i mają jasną definicję. Liczba wszystkich transakcji BLIK nie jest liczbą oszustw. Komunikaty o pojedynczych sprawach nie tworzą kompletnej statystyki rocznej. |

Zbiory do analizy można pobrać poleceniem `python tools/fetch_datasets.py` po instalacji dodatku `.[datasets]`. Raporty eksploracyjne powstają poleceniem `python tools/analyze_datasets.py` po instalacji `.[analysis]`. Przy publikacji należy podać autorów, cytowania i licencje wskazane przez źródła.

Przykładowe źródła pierwotne:

- [UCI: SMS Spam Collection](https://archive.ics.uci.edu/dataset/228/sms%2Bspam%2Bcollection)
- [UCI: Phishing Websites](https://archive.ics.uci.edu/dataset/327/phishing%20websites)
- [UCI: PhiUSIIL Phishing URL](https://archive.ics.uci.edu/dataset/967/phiusiil%2Bphishing%2Burl%2Bdataset)
- [CERT Polska: raport roczny 2023](https://cert.pl/posts/2024/04/raport-roczny-2023/)
- [CERT Polska: raport roczny 2024](https://cert.pl/posts/2025/04/raport-roczny-2024/)
- [CERT Polska: przegląd kampanii phishingowych z 2024 r.](https://cert.pl/posts/2025/04/przeglad-kampanii-2024/)
- [BLIK: statystyki transakcji za 2024 r.](https://www.blik.com/ponad-24-mld-transakcji-blikiem-w-2024-r-i-7-mld-w-ciagu-10-lat)

Statystyki transakcji BLIK mogą dać kontekst skali użycia systemu, ale nie są miarą oszustw. Do oceny oszustw trzeba używać wskaźników strat, reklamacji lub przestępstw, jeśli publikacja podaje je w sposób porównywalny.

## Plan przygotowania i analizy danych

### Jakość danych

1. Zachować oryginalne pliki i zapisać ich źródła, datę pobrania, liczbę rekordów, kolumny i definicję etykiety.
2. Sprawdzić braki, duplikaty, zakresy wartości, typy kolumn oraz zgodność liczebności z dokumentacją zbioru.
3. Nie łączyć automatycznie zbiorów o różnych definicjach etykiet, językach i jednostkach analizy.
4. Dla raportów rocznych przygotować tabelę: `rok`, `źródło`, `nazwa wskaźnika`, `wartość`, `jednostka`, `definicja`, `uwagi o metodzie`.
5. Zmiany sposobu zgłaszania i klasyfikowania incydentów opisać jako ograniczenia szeregu czasowego; liczby zgłoszeń nie zawsze są liczbą wszystkich zdarzeń.

### Statystyka opisowa i wizualizacje

- Dla klas spam/ham i phishing/legitimate podać liczebności i odsetki.
- Dla wybranych cech numerycznych URL podać medianę, kwartyle, średnią, odchylenie standardowe, minimum i maksimum osobno dla klas.
- Współczynnik zmienności stosować tylko dla dodatnich zmiennych na skali ilorazowej, dla których zero ma sens; nie stosować go do kodów etykiet.
- Przygotować wykresy słupkowe udziałów klas, histogramy wybranych cech, boxploty cech według etykiety oraz wykresy rozrzutu wybranych par cech.
- W szeregu rocznym pokazać punkty i zmianę rok do roku tylko wtedy, gdy definicje wskaźników pozostają wystarczająco porównywalne. Nie ekstrapolować krótkiego szeregu.

### Testy i modele

- Obejrzeć rozkłady i wartości odstające. Test Shapiro–Wilka może uzupełnić ocenę normalności, ale przy dużych zbiorach wykrywa nawet małe odchylenia; decyzję oprzeć także na wykresach i charakterze zmiennej.
- Porównując cechę liczbową między dwiema klasami, użyć testu t/Welcha, gdy założenia są uzasadnione, albo testu U Manna–Whitneya dla rozkładów skośnych lub obserwacji porządkowych. Przy wielu cechach skorygować p-value.
- Dla zależności między cechami użyć korelacji Pearsona, jeśli zależność jest liniowa i nie dominują obserwacje odstające; w innym wypadku Spearmana. Podać współczynnik i przedział ufności.
- Ponieważ etykieta phishing/legitimate jest binarna, właściwym modelem regresyjnym jest regresja logistyczna, a nie regresja liniowa dla ciągłego wyniku. Raportować współczynniki/ilorazy szans z niepewnością oraz ograniczenia modelu. Nie przedstawiać tego jako jakości produkcyjnego skanera bez niezależnego testu.
- Dla małej liczby rocznych obserwacji ograniczyć się do wykresu i statystyk opisowych zamiast testów trendu o niskiej mocy.

## Raport z wyników demo RAG 22–26

Przytoczone odpowiedzi zawierają oznaki błędnego wyświetlania polskich znaków, np. `fragmentÃ³w` i `Å¹rÃ³dÅo`. To wygląda na niezgodne dekodowanie UTF-8, ale sam wklejony tekst nie rozstrzyga, czy błąd powstał w odpowiedzi API, odczycie w PowerShell czy kopiowaniu. Przed oceną jakości językowej modelu należy porównać surową odpowiedź HTTP z tekstem wyświetlonym w terminalu.

| Demo | Co pokazuje przytoczony wynik | Ocena |
| --- | --- | --- |
| 22 — PhiUSIIL | `label=1`: 134 850 (57,19%) | Liczby są poprawne, ale UCI definiuje `1` jako legalny URL, a `0` jako phishing. Phishing stanowi więc 100 945 rekordów (42,81%). Nagłówek, pytanie i oczekiwane wartości dema poprawiono w `docs/demo.md`. |
| 23 — Phishing Websites | `result=1`: 6 157 (55,69%); `result=-1`: 4 898 (44,31%) | Liczebności i odsetki zgadzają się z lokalnym `class_distribution.csv`. Bez zweryfikowania semantyki kodów należy w odpowiedzi zachować nazwy `1` i `-1`. |
| 24 — SMS Spam Collection | 747 wiadomości spam, 13,41% | Zgadza się z eksportem zawierającym 5 572 niepuste rekordy. Jest to udział etykiety w tym zbiorze, nie odsetek oszustw BLIK ani ocena konkretnej wiadomości. |
| 25 — pilny BLIK | Przeszukane raporty nie zawierają przykładu wiadomości ani statystyk BLIK | To poprawnie ogranicza wniosek do zawartości indeksowanych raportów. Brak wzmianki nie dowodzi, że oszustwa BLIK są nowe lub rzadkie. |
| 26 — konkretny link | Zagregowane raporty nie potwierdzają konkretnej domeny `g00gle.com` | Odpowiedź słusznie odróżnia rozkład zbioru od reputacji pojedynczego URL-a. Do tego służy skaner lub aktualne źródło reputacji, nie zagregowana tabela klas. |

## Czy z tych wyników wynika, że BLIK to nowe zagrożenie?

Nie. Dema 22–26 nie analizują dat zdarzeń ani zmian częstości oszustw BLIK i phishingu. Demo 25 informuje tylko, że przeszukane raporty nie zawierały szukanych przykładów lub statystyk. Istnieją starsze materiały o phishingu: UCI podaje artykuł z 2012 r. i datę przekazania zbioru Phishing Websites w 2015 r. [UCI: Phishing Websites](https://archive.ics.uci.edu/dataset/327/phishing%20websites). To potwierdza wcześniejsze badania phishingu, ale samo w sobie nie mierzy skali zagrożenia w kolejnych latach.

Teza, że oszustwa z kodami BLIK są „świeższe”, może być osobnym pytaniem desk research, ale wymaga zestawienia datowanych i porównywalnych danych o incydentach lub stratach. Publikowane przez BLIK wolumeny transakcji nie są liczbą oszustw; przykłady postępowań policyjnych nie są pełnym szeregiem statystycznym. Jeśli nie znajdzie się wspólnej definicji i danych za kolejne lata dla obu zjawisk, w raporcie należy napisać, że porównanie historyczne nie jest możliwe na podstawie dostępnych źródeł.

## Ograniczenia i plan pracy

- Zbiory mają różne okresy, języki, definicje etykiet i jednostki. Nie należy łączyć ich w jeden wspólny mianownik bez uzasadnienia.
- Dane UCI opisują konkretne zbiory, a nie aktualną populację wszystkich wiadomości i stron.
- Zmiany liczby zgłoszeń CERT mogą wynikać ze zmian kanałów zgłoszeniowych lub metod klasyfikacji.
- Brak szeregu statystyk BLIK nie może być zastąpiony wolumenem transakcji ani pojedynczymi komunikatami o przestępstwach.
- RAG może pomylić znaczenie etykiety lub wygenerować niepoparte twierdzenie. Wartości należy weryfikować bezpośrednio w CSV i źródłowej dokumentacji.

Kolejność prac:

1. Pobrać i opisać publiczne zbiory oraz raporty źródłowe.
2. Zbudować tabelę słownika danych i zweryfikować kody klas, szczególnie `label=1` w PhiUSIIL.
3. Odtworzyć statystyki opisowe i wykresy poleceniami projektu.
4. Wybrać z góry kilka cech do testów porównawczych i korelacji; raportować wielkości efektu oraz korektę wielokrotnych testów.
5. Zestawić dostępne raporty roczne CERT i źródła o BLIK, sprawdzając definicje, zakres i porównywalność.
6. Sformułować wnioski wyłącznie w zakresie danych; oddzielić statystyki zbiorów od aktualnej oceny pojedynczego linku i od historycznych tez o BLIK.
