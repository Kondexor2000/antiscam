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

## Zbadane ryzyko danych publicznych

Wygenerowane raporty CSV pozwalają ocenić ryzyko jakości i interpretacji danych przed przeprowadzeniem testów. Nie stwierdzają one skuteczności produkcyjnego skanera ani częstości oszustw w populacji.

| Zbadane ryzyko | Wartość w danych | Konsekwencja i kontrola |
| --- | --- | --- |
| Duplikaty wpływające na rozkład i testy | `uci_phishing_websites.csv`: 5 206 z 11 055 wierszy to duplikaty (47,09%); `uci_sms_spam_collection.csv`: 403 z 5 572 wierszy (7,23%); w PhiUSIIL nie ma identycznych pełnych wierszy. | Nie usuwać duplikatów automatycznie przed analizą. Oceniać, czy są to rzeczywiste powtórzenia rekordów, a podczas testów sprawdzić wyniki z duplikatami i po ich jawnej decyzji o usunięciu. |
| Powtarzane wartości URL mimo braku pełnych duplikatów | W PhiUSIIL jest 235 370 unikalnych wartości `URL` przy 235 795 rekordach, więc co najmniej część identycznych URL-i występuje więcej niż raz. | Przed trainingiem i testem sprawdzić, czy powtórzone URL-e nie przeciekają między zestawami; osobno policzyć duplikaty po pełnym wierszu i po URL-u. |
| Braki danych | Wszystkie trzy zbiory mają 0 brakujących komórek i 0,0% pustych wartości w wygenerowanej tabeli profilu. | Nie zastępowano danych tylko na podstawie tych statystyk. Braki mogą nadal dotyczyć semantycznie niejawnych informacji, np. niedostępności strony lub historycznego wieku domeny. |
| Nierównomierne klasy | Spam stanowi 747 z 5 572 SMS-ów (13,41%), klasy 1 i -1 w zbiorze stron wynoszą odpowiednio 55,69% i 44,31%, a w PhiUSIIL `label=1` to 57,19%, a `label=0` 42,81%. | Przy modelowaniu podać macierz pomyłek i metryki dla każdej klasy; nie oceniać wyłącznie accuracy. Należy rozważyć próbkowanie i raportowanie precision, recall, F1 oraz przedziałów ufności. |
| Zamieszanie znaczenia kodów | W zbiorze stron `result=1/-1`, a w PhiUSIIL `label=1/0`; tylko PhiUSIIL potwierdza zgodnie z dokumentacją UCI, że `1` oznacza legalny URL, a `0` phishing. | Zachować oryginalne kody i osobną tabelę znaczeń; nie używać słów „phishing/legitimate” przed sprawdzeniem dokumentacji konkretnego zbioru. |
| Niepewna reprezentatywność | SMS-y są angielskie, zbiory URL-i mają cechy statyczne i nie opisują polskiego kontekstu BLIK, a zbiory pochodzą z różnych okresów i metod tworzenia. | Wyniki ograniczyć do etykiety i jednostki konkretnego zbioru. Nie przenosić proporcji spamu ani phishingu na wszystkie wiadomości, strony lub pola BLIK w Polsce. |
| Wartości odstające i rozkłady skośne | W PhiUSIIL mediana długości URL wynosi 27, średnia 34,57, maksimum 6 097; inne cechy mają również duże maksimumy, np. `LargestLineLength` 13 975 732 i `LineOfCode` 442 666. | Nie opisywać średnich jako typowego URL-a bez mediany i kwartylów. Histogramy i testy odporne mają poprzedzać wybór testu parametrycznego; przed analizą wykreślić obserwacje odstające i sprawdzić ich realność. |
| Kody zmiennych jako wartości ilościowe | W zbiorze stron cechy są kodowane głównie jako `-1/0/1`, `-1/1` lub `0/1`; średnie i odchylenia opisują kody, a nie naturalną długość ani częstotliwość zjawiska. | Nie obliczać współczynnika zmienności ani interpretować średnich jako fizycznych wielkości dla zmiennych nominalnych/porządkowych. |
| Brak danych czasowych | CSV zawierają rozkłady i cechy, ale nie porównywalny szereg lat dotyczący oszustw SMS lub BLIK. | Nie liczyć trendu ani stwierdzać wzrostu/spadku oszustw na podstawie jednorazowych udziałów klas. |

Kontrola wyników publicznych powinna obejmować co najmniej:

1. liczebność i udział klas przed i po jawnej analizie duplikatów,
2. rozdzielenie zbioru tak, aby ten sam tekst lub ten sam URL nie występował jednocześnie w treningu i teście,
3. porównanie cech grupami etykiet z medianą, kwartylami i wielkością efektu,
4. korektę wielokrotnych porównań i osobne raportowanie niepewności,
5. uzupełnienie analizy niezależnymi polskimi danymi przed jakąkolwiek oceną skanera lub oszustw BLIK.

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

## Uzasadnienie zgodności projektu z blokami tematycznymi Elementów Statystyki

Uzasadnienia opierają się na surowych zbiorach w `data/` i wygenerowanych tabelach w `reports/data-analysis/`. Istniejące pliki CSV zawierają głównie statystyki opisowe, profil danych i rozkłady klas. Dodatkowe kwartyle długości SMS oraz wartości korelacji, testu hipotezy i regresji obliczono bezpośrednio z surowych danych na potrzeby uzasadnienia; nie są one zawartością istniejących plików raportów ani oceną produkcyjnego skanera.

### 1. Statystyka matematyczna, estymatory i statystyka opisowa (W1, W2, U1, U2, U3, K1)

Projekt realizuje ten blok, ponieważ rozpoczyna od obserwacji zapisanych w publicznych zbiorach, a następnie opisuje ich liczebności, udziały, rozkłady i zmienność. [dataset_summary.csv](../reports/data-analysis/dataset_summary.csv) podaje rozmiar, liczbę zmiennych, kompletność i liczbę duplikatów, [class_distribution.csv](../reports/data-analysis/class_distribution.csv) zestawia liczebności oraz odsetki klas, a [numeric_summary.csv](../reports/data-analysis/numeric_summary.csv) zawiera średnią, odchylenie standardowe, kwartyle, medianę, minimum i maksimum zmiennych numerycznych. Są to bezpośrednie przykłady statystyk opisowych wykorzystywanych do syntetycznego przedstawienia danych przed formułowaniem wniosków.

Na przykład eksport [SMS Spam Collection](../data/uci_sms_spam_collection.csv) zawiera 5 572 wiadomości: 4 825 z etykietą `ham` i 747 z `spam`. Udział spamu w tej próbie to estymator proporcji `747/5572 = 13,41%`; przedział Wilsona 95% wynosi około 12,54–14,33% przy założeniu modelu dwumianowego i niezależności obserwacji. Statystyka długości wiadomości ma średnią 80,49 znaków, medianę 62, kwartyle 36 i 122 oraz maksimum 910. Dla PhiUSIIL, opisanego w [dataset_summary.csv](../reports/data-analysis/dataset_summary.csv), długość URL ma średnią 34,57, medianę 27, kwartyle 23–34 i maksimum 6 097. Zestawienie średniej z medianą i kwartylami pokazuje, jak opisywać położenie i rozproszenie również przy asymetrycznym rozkładzie. Student może przejść od jednostkowych rekordów do estymatorów i interpretacji ich niepewności, jednocześnie ucząc się, że przedział dla konkretnej próby nie czyni jej reprezentatywną dla wszystkich SMS-ów ani oszustw w Polsce.

### 2. Elementy analizy eksploracyjnej (W2, U1, U2, U3, K1)

Zgodność z analizą eksploracyjną wynika z tego, że przed właściwymi testami projekt sprawdza strukturę, jakość, typy i rozkłady danych. [column_profile.csv](../reports/data-analysis/column_profile.csv) pokazuje typy kolumn, liczby niepustych i unikalnych wartości oraz braki; [dataset_summary.csv](../reports/data-analysis/dataset_summary.csv) podaje duplikaty i ogólny rozmiar; [numeric_summary.csv](../reports/data-analysis/numeric_summary.csv) pozwala znaleźć skrajne zakresy i rozbieżności między średnią a medianą. W trzech zbiorach znajduje się zero pustych komórek, ale to nie rozstrzyga o kompletności znaczeniowej ani aktualności danych. W zbiorze `Phishing Websites` 5 206 z 11 055 wierszy to duplikaty, a w SMS-ach 403 z 5 572; PhiUSIIL nie ma duplikatów całych wierszy, lecz 425 rekordów zawiera powtórzone wartości URL. Te obserwacje uzasadniają kontrolę duplikatów i sprawdzenie, czy powtarzające się rekordy nie wpływają na opis lub podział trening/test.

Eksploracja ujawnia także odmienne skale zmiennych. W `Phishing Websites` liczne cechy są kodami `-1`, `0`, `1`, więc ich średniej nie wolno interpretować jak długości fizycznej. W PhiUSIIL `URLLength` ma medianę 27 i maksimum 6 097; `LargestLineLength` osiąga 13 975 732, co uzasadnia wykresy, sprawdzenie wartości skrajnych oraz podawanie mediany i kwartylów obok średniej. Dla SMS profil wykazuje 5 169 unikalnych tekstów na 5 572 rekordy, co również wskazuje na powtórzenia. Takie kroki prowadzą do decyzji analitycznych: które rekordy opisać, jak interpretować kody, czy użyć metod odpornych na skośność i jak ograniczyć ryzyko przecieku tych samych obserwacji między podzbiory. Projekt nie traktuje pojedynczego podsumowania jako automatycznego dowodu jakości danych, lecz wykorzystuje je do sformułowania kontroli i ograniczeń.

### 3. Analiza korelacji (W1, W2, U1, U2, U3, K1, K2)

Blok korelacji jest bezpośrednio związany z pytaniem projektu o to, czy wybrane cechy URL współwystępują z etykietą phishingową oraz jak same cechy adresów wiążą się ze sobą. Surowy zbiór [uci_phiusiil_phishing_url.csv](../data/uci_phiusiil_phishing_url.csv) zawiera 235 795 rekordów i cechy takie jak `URLLength`, `DomainLength`, `NoOfSubDomain`, `IsHTTPS` oraz `label`; profile i podsumowania numeryczne w `reports/data-analysis/` pokazują skalę i typy tych zmiennych. Na potrzeby tego uzasadnienia przeliczono korelację rang Spearmana, kodując phishing jako 1 (oryginalne `label=0`) i legalny URL jako 0 (`label=1`). Otrzymano rho = 0,399 dla `URLLength` względem etykiety phishingowej, rho = 0,214 dla `DomainLength`, rho = -0,609 dla `IsHTTPS` oraz rho = -0,033 dla liczby subdomen. Sama długość domeny i URL wykazują też silną współzmienność (Spearman rho = 0,803).

Spearman opiera się na rangach, więc jest przydatny przy skośnych rozkładach, wartościach odstających i monotonicznej, niekoniecznie liniowej zależności. Przy tak dużej liczebności testy zwracają p < 0,001 nawet dla bardzo słabej zależności, jak rho = -0,033; dlatego interpretacja powinna uwzględniać siłę związku, a nie wyłącznie istotność statystyczną. Związek `IsHTTPS` z etykietą nie oznacza, że szyfrowanie powoduje legalność lub phishing, a korelacja długości URL nie daje reguły pozwalającej bezbłędnie sklasyfikować pojedynczy adres. W ten sposób projekt obejmuje obliczanie i interpretowanie korelacji, odróżnia asocjację od przyczynowości oraz pokazuje, dlaczego wyniku z jednego zbioru nie należy bez walidacji przenosić na inne populacje i aktualne incydenty.

### 4. Testowanie hipotez statystycznych (W1, W2, U1, U2, U3, K1)

Zastosowanie tego bloku przedstawia porównanie cechy liczbowej między dwiema klasami w PhiUSIIL. Hipotezę można ustalić przed testem następująco: H0 — rozkład długości URL jest taki sam dla rekordów phishingowych i legalnych; H1 — rozkłady różnią się. Ponieważ długość jest skośna i ma skrajne wartości, zastosowano dwustronny test U Manna–Whitneya zamiast zakładać normalność lub równość wariancji. W pełnym zbiorze phishingowe URL-e (`label=0`) miały medianę długości 34 znaków i kwartyle 26–48; legalne (`label=1`) medianę 26 i kwartyle 23–29. Test dał p < 0,001, a korelacja rangowo-dwuseryjna wyniosła około 0,465, wskazując na zauważalną różnicę w rangach. Duża próba uzasadnia jednocześnie omawianie wielkości efektu, a nie ograniczanie wniosku do samego p-value.

W zbiorze jest 425 powtórzonych wartości `URL`, dlatego sprawdzono wrażliwość wyniku po pozostawieniu jednego rekordu na URL. Po tej kontroli pozostaje 235 370 adresów; mediany nadal wynoszą 34 i 26, efekt rangowo-dwuseryjny około 0,463, a p < 0,001. To ćwiczy nie tylko wyznaczenie hipotez, testu i decyzji, lecz także ocenę wpływu zależności w danych. Wynik należy interpretować jako różnicę w tym konkretnym zbiorze, nie jako dowód, że długi URL jest phishingiem. Przy analizie wielu cech trzeba z góry wybrać hipotezy lub skorygować p-value, np. metodą Holma; wnioski o populacji wymagają ponadto reprezentatywnego doboru próby, którego gotowy zbiór UCI nie gwarantuje.

### 5. Analiza regresji (W1, W2, U1, U2, U3, U4, K1, K2)

Regresja jest uzasadniona binarną zmienną celu w PhiUSIIL: po przekształceniu etykiety `label=0` w phishing (`1`), a `label=1` w legalny URL (`0`), można modelować prawdopodobieństwo przynależności do klasy phishingowej. W dodatkowym eksperymencie wykonano regresję logistyczną na sześciu cechach liczbowych i binarnych: `URLLength`, `DomainLength`, `NoOfSubDomain`, `IsDomainIP`, `HasObfuscation` i `IsHTTPS`. Zastosowano standaryzację oraz podział trening/test grupowany po domenie, tak aby ta sama domena nie występowała po obu stronach; przy stałym `random_state=42` zbiór testowy liczył 58 572 rekordy, a przecięcie domen wyniosło zero. Na tym pojedynczym podziale accuracy wyniosło 97,87%, precision dla phishingu 99,96%, recall 95,02%, F1 97,43%, a ROC-AUC 99,71%. Macierz pomyłek wskazała 1 241 fałszywie ujemnych i 9 fałszywie dodatnich przewidywań.

Wyniki pokazują, jak przejść od opisu i korelacji do modelu regresyjnego, dobrać binarną zmienną objaśnianą, zinterpretować metryki oraz rozróżnić koszt przeoczenia od kosztu fałszywego alarmu. Nie są jednak dowodem, że podobna jakość wystąpi na nowych domenach, polskich wiadomościach ani w AntiScam. Metryki są wyjątkowo wysokie i pochodzą z jednego historycznego zbioru; cechy `URLLength` i `DomainLength` są silnie skorelowane, co może destabilizować współczynniki i ilorazy szans. Grupowanie po domenie ogranicza jeden rodzaj przecieku, ale nie eliminuje podobieństw źródłowych ani problemów z etykietami. Dlatego regresja służy tu jako przykład analizy na danych publicznych; wiarygodna ocena wymagałaby walidacji zewnętrznej, kalibracji, przedziałów niepewności i audytu cech oraz podziału danych.

## Wykonanie demo 10–14: API skanera i objaśnienie AI

Dema uruchomiono lokalnie 26 listopada 2026 r. na API z włączonym agentem Ollama. Dema 10–13 wywołują `/scan`, więc ich wynik pochodzi z deterministycznego silnika, nie z agenta. Demo 14 wywołuje `/ai/explain` i zwraca zarówno wynik skanera, jak i objaśnienie agenta.

| Demo | Wynik końcowy | Wynik bazowy ML | Sprawdzone zachowanie |
| --- | --- | --- | --- |
| 10 | `HIGH RISK`, 100/100 | `scam`, 72 (`p=0.72`) | Powód ML i potwierdzony BLIK znalazły się w `reasons`; kod został zredagowany. |
| 11 | `HIGH RISK`, 100/100 | `scam`, 86 (`p=0.86`) | Link `g00gle.com` był ryzykowny, wykryto typosquatting, a kod BLIK nie wyciekł do odpowiedzi. |
| 12 | `HIGH RISK`, 100/100 | `scam`, 74 (`p=0.74`) | Rozstrzelone `B L I K` i `k-o-d` wystarczyły, by wykryć kontekst BLIK; kod został zredagowany. |
| 13 | `HIGH RISK`, 100/100 | `safe`, 48 (`p=0.48`) | Mimo bezpiecznej etykiety bazowego ML oba linki trafiły do `risky_links`, a `g00gle.com` do typosquattingu. To przykład działania reguł ponad wynikiem bazowym, nie dowód jakości na rzeczywistych incydentach. |
| 14 | `HIGH RISK`, 100/100 | `scam`, 72 (`p=0.72`) | Obecne były wymagane pola raportu, `blocked_after_scan=true`, odpowiedź agenta i redakcja kodu. |

Wyniki 10–13 spełniły sprawdzane warunki API, a w żadnej odpowiedzi testowej nie ujawniono sześciocyfrowego kodu. Przy starcie pojawiło się też ostrzeżenie o niezgodności wersji scikit-learn zapisanej w modelu i używanej w środowisku; bazowe predykcje ML mogą być przez to niewiarygodne. Nie oznacza to, że agent nie może podawać nieprawdziwych informacji. W demo 14 objaśnienie stwierdziło, że „system blokuje taką próbę”, choć `/ai/explain` jedynie zwraca raport i flagę `blocked_after_scan`; endpoint nie wykonuje transakcji ani nie blokuje wysyłania wiadomości. Zalecenie zablokowania nadawcy również jest poradą tekstową, a nie wykonaną czynnością. To konkretna, myląca nadinterpretacja agenta. Wartości `p=...` pochodzą z modelu i nie są skalibrowanym prawdopodobieństwem oszustwa.

## Wykonanie demo 17–21: skaner i objaśnienia AI

Dema uruchomiono lokalnie 26 listopada 2026 r. przez `POST /ai/explain` z włączonym agentem Ollama. Wszystkie pięć skonstruowanych wiadomości otrzymało `HIGH RISK` i wynik 100/100; agent zwrócił objaśnienie, a kody BLIK z demo 18 i 19 nie pojawiły się w odpowiedziach. To potwierdza wyłącznie działanie wybranych ścieżek lokalnego skanera i redakcji kodów — nie jest testem danych publicznych ani badaniem częstości oszustw.

| Demo | Temat przykładu | Wynik skanera | Obserwacja z wykonania |
| --- | --- | --- | --- |
| 17 | Podszycie pod bank i `g00gle.com` | `HIGH RISK`, 100/100 | Agent wskazał typosquatting; brak potwierdzenia na liście CERT nie jest dowodem bezpieczeństwa. |
| 18 | Pilna prośba o kod BLIK | `HIGH RISK`, 100/100 | Agent ostrzegł przed przekazaniem kodu; sześciocyfrowy kod został zredagowany. |
| 19 | BLIK, pilność i `paypaI.example` | `HIGH RISK`, 100/100 | Agent ostrzegł przed linkiem i kodem; jego przypisanie typosquattingu nie powinno być traktowane jako potwierdzone bez sprawdzenia wyniku narzędzia. |
| 20 | Podejrzana domena obok `google.com` | `HIGH RISK`, 100/100 | Agent odróżnił domeny, ale nazwanie `google.com` „bezpiecznym” wykracza poza lokalną regułę zaufanej domeny i nie potwierdza bieżącej reputacji ani bezpieczeństwa strony. |
| 21 | Pilny link do rzekomego zwrotu | `HIGH RISK`, 100/100 | Agent ostrzegł przed logowaniem z wiadomości; podany link i tekst są przykładowe, a nie zaczerpnięte z incydentu w zbiorze. |

W objaśnieniach pojawiły się też twierdzenia, których nie należy uznawać za ustalone fakty: m.in. niekalibrowana „83% pewność” w demie 18 oraz interpretacja braku dopasowania CERT jako „braku fałszywego alarmu” w demie 17. Wynik skanera pozostaje deterministycznym wynikiem reguł i lokalnego modelu; opis agenta wymaga weryfikacji z `reasons` i wynikami narzędzi. `HIGH RISK` dla skonstruowanego przykładu nie oznacza, że model wykrył odpowiadający mu rekord w publicznym źródle.

### Wspólny wniosek badawczy z demo 10–14 i 17–21

Wyniki są dowodem, że prototyp w tych konkretnych próbach rozpoznał skonstruowane podejrzane wiadomości jako `HIGH RISK`, wskazał część sygnałów i redagował kody BLIK. Pokazują potencjał użycia skanera jako warstwy ostrzegawczej. Nie dowodzą jednak, że projekt uniemożliwia hakerom przejęcie cudzych kont ani wysyłanie z nich wiadomości: testy nie obejmowały włamań, kontroli dostępu do kont ani egzekwowania blokady w rzeczywistym przepływie wysyłania. `/scan` i `/ai/explain` zwracają oceny i objaśnienia, a komunikat agenta o blokowaniu nie jest dowodem, że wysyłka została faktycznie zatrzymana. Do potwierdzenia takiego efektu potrzebna byłaby integracja skanera z bramką wysyłki oraz niezależny test bezpieczeństwa i pomiar skuteczności blokowania na realistycznych przypadkach.

Gałąź [`only-python`](https://github.com/Kondexor2000/antiscam/tree/only-python) jest wskazana jako wariant badawczo-wdrożeniowy. Gałąź [`main`](https://github.com/Kondexor2000/antiscam/tree/main) udostępnia implementację projektu, ale sama w sobie nie jest dowodem, że osobom włamującym się na cudze konta nie udaje się wysyłać podejrzanych wiadomości. Takie twierdzenie wymagałoby niezależnych testów kontroli dostępu i blokowania wysyłki w działającym systemie.

Dalszy rozwój i wdrożenie projektu wymagają konsultacji z analitykiem ds. cyberbezpieczeństwa, w szczególności w zakresie modelu zagrożeń, kontroli dostępu, integracji skanera z przepływem wysyłki oraz niezależnej walidacji skuteczności.

Skuteczna konsultacja i dalszy rozwój mogą pomóc bezpiecznie aktualizować wykorzystywane dane publiczne, takie jak listy ostrzeżeń i zbiory referencyjne, co może poprawić wykrywanie nowych podejrzanych treści. Sama aktualizacja danych nie uniemożliwia jednak przejęcia kont ani wysyłania z nich wiadomości. Ograniczenie takiego działania wymagałoby także egzekwowanych kontroli dostępu i integracji skanera z rzeczywistą ścieżką wysyłania; ich skuteczność należałoby potwierdzić niezależnymi testami.

### Kontrola wzmianek o BLIK w lokalnych zbiorach publicznych

Przeszukano wszystkie rekordy w trzech plikach UCI w `data/`, stosując dopasowanie całego słowa `BLIK` bez rozróżniania wielkości liter. Wynik: **0 dopasowań w 11 055** rekordach `uci_phishing_websites.csv`, **0 w 235 795** rekordach `uci_phiusiil_phishing_url.csv` i **0 w 5 572** wiadomościach `uci_sms_spam_collection.csv`.

Samo wyszukiwanie podciągu `blik` dało 14 trafień w PhiUSIIL, ale były to kolizje tekstowe, m.in. nazwy zawierające `republik...`, `publik...` i `weblike`; nie stanowią one wzmianek o systemie BLIK. Wśród tych 14 trafień 12 miało etykietę `1` (legalny URL), a 2 etykietę `0` (phishing). To pokazuje, dlaczego proste wyszukiwanie fragmentu słowa może prowadzić do fałszywych wniosków.

Wniosek jest ograniczony do tych trzech lokalnych zbiorów: nie zawierają one jawnych wzmianek o BLIK jako systemie płatności ani osobnej etykiety ryzyka BLIK. Nie dowodzi to, że ryzyko nie istnieje w danych publicznych ogółem — w raporcie wskazano już [ostrzeżenie Policji z 2020 r. o wyłudzeniach kodów BLIK](https://policja.pl/pol/aktualnosci/189192,Uwaga-na-oszustow-wyludzajacych-kody-BLIK.html). Ten przykład dokumentuje istnienie opisywanych przypadków, ale nie mierzy ich częstości.

## Raport z wyników demo RAG 22–26

Przytoczone odpowiedzi zawierają oznaki błędnego wyświetlania polskich znaków, np. `fragmentÃ³w` i `Å¹rÃ³dÅo`. To wygląda na niezgodne dekodowanie UTF-8, ale sam wklejony tekst nie rozstrzyga, czy błąd powstał w odpowiedzi API, odczycie w PowerShell czy kopiowaniu. Przed oceną jakości językowej modelu należy porównać surową odpowiedź HTTP z tekstem wyświetlonym w terminalu.

| Demo | Co pokazuje przytoczony wynik | Ocena |
| --- | --- | --- |
| 22 — PhiUSIIL | `label=1`: 134 850 (57,19%) | Liczby są poprawne, ale UCI definiuje `1` jako legalny URL, a `0` jako phishing. Phishing stanowi więc 100 945 rekordów (42,81%). Nagłówek, pytanie i oczekiwane wartości dema poprawiono w `docs/demo.md`. |
| 23 — Phishing Websites | `result=1`: 6 157 (55,69%); `result=-1`: 4 898 (44,31%) | Liczebności i odsetki zgadzają się z lokalnym `class_distribution.csv`. Bez zweryfikowania semantyki kodów należy w odpowiedzi zachować nazwy `1` i `-1`. |
| 24 — SMS Spam Collection | 747 wiadomości spam, 13,41% | Zgadza się z eksportem zawierającym 5 572 niepuste rekordy. Jest to udział etykiety w tym zbiorze, nie odsetek oszustw BLIK ani ocena konkretnej wiadomości. |
| 25 — pilny BLIK | Przeszukane raporty nie zawierają przykładu wiadomości ani statystyk BLIK | To poprawnie ogranicza wniosek do zawartości indeksowanych raportów. Brak wzmianki nie dowodzi, że oszustwa BLIK są nowe lub rzadkie. |
| 26 — konkretny link | Zagregowane raporty nie potwierdzają konkretnej domeny `g00gle.com` | Odpowiedź słusznie odróżnia rozkład zbioru od reputacji pojedynczego URL-a. Do tego służy skaner lub aktualne źródło reputacji, nie zagregowana tabela klas. |

### Zbadane ryzyko wyników demo 22–26

Dema 22–26 sprawdzają przede wszystkim poprawność odpowiedzi RAG i granice interpretacji wyników. Nie tworzą zestawu testowego o znanej podstawie prawdy dla jakości systemu, więc nie należy z ich wyniku wyliczać dokładności, czułości ani współczynnika fałszywych alarmów.

| Ryzyko | Ocena dla dema | Zapewnienie kontroli |
| --- | --- | --- |
| Odwrócenie znaczenia etykiety | Ryzyko wysokie w demie 22: `label=1` oznacza legalny URL, a nie phishing. Odczytanie odwrotnie prowadziłoby do błędnego udziału phishingu wynoszącego 57,19% zamiast 42,81%. | Porównać kod z dokumentacją zbioru i zawsze podawać nazwy klasy dla danego źródła. |
| Niezweryfikowana semantyka kodów | W demie 23 liczby dla `1` i `-1` są poprawne, ale bez dokumentacji wyniku klasy można pomylić ich znaczenie. | Zachować w odpowiedzi kody `1` i `-1` oraz powiązać je z semantyką po osobnym potwierdzeniu dokumentacji. |
| Ekstrapolacja z etykiety zbioru | Dema 24 pokazuje udział spamu w angielskim zbiorze SMS, ale nie orzeka o konkretnych wiadomościach ani polskich oszustwach BLIK. | Nie używać udziału 13,41% jako oceny ryzyka konkretnego SMS-a; określić zasięg wyników jako jeden zbiór. |
| Dowód nieobecności | Demo 25 może poprawnie stwierdzić, że brakowało przykładu lub statystyki BLIK w `reports/`, ale to nie dowodzi, że zdarzenia nie istniały poza nim. | Podać zakres indeksowanych plików i datę przeszukania; braku wzmianki nie interpretować jako braku zagrożenia ani trendu. |
| Reputacja pojedynczego linku | Demo 26 potwierdziło tylko brak agregowanego wpisu o `g00gle.com`; nie potwierdziło ani nie obaliło, czy ten konkretny adres jest phishingowy. | Dla bieżącej oceny użyć `/scan` lub `/ai/explain`; wynik silnika również nie zastępuje reputacji aktywnej domeny ani niezależnego źródła. |
| Błędne dekodowanie znaków | Przytoczone `fragmentÃ³w` i `Å¹rÃ³dÅo` może zmniejszyć wiarygodność cytatu i utrudnić ocenę źródła. Sam tekst nie rozstrzyga, gdzie powstał problem. | Zweryfikować surową odpowiedź HTTP i końcową treść terminalową; raportować zachowaną wartość JSON i encoding. |
| Niepełna baza źródłowa | RAG może odpowiedzieć tylko na podstawie indeksu raportów i CSV, a demo nie obejmuje wszystkich publicznych raportów ani aktualnych list domen. | Zwracać cytowane źródło i jawnie wskazywać ograniczenie „odnalezione raporty”; nie uogólniać na całą przestrzeń incydentów. |
| Zbyt rozbudowane wnioski | Wyniki liczbowe mogą zostać przedstawione jako trend, skuteczność czy stopień zagrożenia, mimo braku szeregu czasowego i zbioru testowego. | Wyznaczać tylko statystyki opisowe dostępnych danych oraz osobno raportować próbę walidacyjną z metrykami błędów. |

Wynik kontroli dla demo 22–26 należy oceniać per demo:

- Demo 22 i 23 ryzykują przede wszystkim błędną interpretacją kodów klas.
- Demo 24 ryzykuje uogólnieniem statystyki jednego zbioru na konkretną wiadomość lub oszustwa BLIK.
- Demo 25 ryzykuje błędem „dowodu braku” i wnioskowaniem o nowości lub rzadkości.
- Demo 26 ryzykuje zastąpieniem aktualnej oceny domeny wynikiem agregatu statystycznego.

#### Wniosek dotyczący lokalnego indeksu

Dema pokazują jedynie, że w indeksowanych lokalnie raportach nie znaleziono odpowiednich statystyk ani przykładu „pilnego BLIKA”. Nie oznacza to, że takie oszustwa nie są opisane w innych źródłach publicznych. Policja opisywała wyłudzenia kodów BLIK i pilne prośby o pożyczkę już w 2020 r.; ten komunikat dokumentuje przypadki, ale nie stanowi kompletnej statystyki rocznej ani dowodu na porównywalną częstość z phishingiem. [Policja: ostrzeżenie z 27 maja 2020 r.](https://policja.pl/pol/aktualnosci/189192,Uwaga-na-oszustow-wyludzajacych-kody-BLIK.html)

## Czy z tych wyników wynika, że BLIK to nowe zagrożenie?

Nie. Dema 22–26 nie analizują dat zdarzeń ani zmian częstości oszustw BLIK i phishingu. Demo 25 informuje tylko, że przeszukane raporty nie zawierały szukanych przykładów lub statystyk. Istnieją starsze materiały o phishingu: UCI podaje artykuł z 2012 r. i datę przekazania zbioru Phishing Websites w 2015 r. [UCI: Phishing Websites](https://archive.ics.uci.edu/dataset/327/phishing%20websites). To potwierdza wcześniejsze badania phishingu, ale samo w sobie nie mierzy skali zagrożenia w kolejnych latach.

Teza, że oszustwa z kodami BLIK są „świeższe”, może być osobnym pytaniem desk research, ale wymaga zestawienia datowanych i porównywalnych danych o incydentach lub stratach. Publikowane przez BLIK wolumeny transakcji nie są liczbą oszustw; przykłady postępowań policyjnych nie są pełnym szeregiem statystycznym. Jeśli nie znajdzie się wspólnej definicji i danych za kolejne lata dla obu zjawisk, w raporcie należy napisać, że porównanie historyczne nie jest możliwe na podstawie dostępnych źródeł.

## Zbadane ryzyko działania silnika

Ocena ryzyka w `engine.py` jest hybrydowym pipeline'em. Wynik ML jest punktem bazowym, a następnie są na niego nakładane reguły BLIK, linków, słów kluczowych, bezpiecznego kontekstu i intencji. Ostateczny wynik jest ograniczony do przedziału 0–100 i klasyfikowany jako: `LOW RISK` poniżej 50, `MEDIUM RISK` od 50 do 79 oraz `HIGH RISK` od 80. Potwierdzony kontekst BLIK zachowuje co najmniej wynik 60, a wykryty typosquatting co najmniej wynik 90.

| Zbadane ryzyko | Dowód lub mechanizm | Konsekwencja i reakcja |
| --- | --- | --- |
| Fałszywy alarm | Zaufany lub legalny kontekst może obniżyć wynik przez kontekst bezpieczny i zaufane domeny, ale może zostać podwyższony przez hasła, intencję lub podejrzany link. | Wynik nie dowodzi oszustwa. Wiadomość należy traktować jako sygnał do weryfikacji, szczególnie gdy BLIK, pilność i link występują razem. |
| Nie wykryte zagrożenie | Nieznana domena lub nowe sformułowanie może nie trafić do reguł ML; brak dopasowania nie oznacza, że link jest bezpieczny. | Nie należy interpretować `LOW RISK` jako potwierdzenia bezpieczeństwa ani otwierać linku tylko w celu jego sprawdzenia. |
| Nadmierna pewność modelu ML | `MlRiskAssessment.score` pochodzi z prawdopodobieństwa klasyfikatora; `p=...` nie jest skalibrowanym prawdopodobieństwem faktycznego oszustwa. | Nie publikować wyników jako ocen pewności ani dostrajać progów na podstawie pojedynczych demo. |
| Nadinterpretacja twardych reguł | Mnożniki i kwoty punktowe mogą kumulować się, a intencja, BLIK i typosquatting mają wpływ na status nawet przy niskim bazowym wyniku ML. | Wynik należy raportować wraz z polem `reasons`; jeden wynik liczbowy nie opisuje jakości dowodu. |
| Fałszywa ocena literówek | Tylko domena zarejestrowana przez wybraną listę zaufanych domen jest porównywana z odległością Levenshteina do wartości tej listy. | `g00gle.com` może zostać oznaczony jako podejrzany, ale inna, nowa lub nietypowa domena może zostać pominięta. |
| Ryzyko prywatności | Zaimplementowane reguły wymagają przetwarzania treści i mogą zwracać pełną nazwę podejrzanego linku w `reasons`. | W odpowiedzi powinien pozostać mechanizm redagowania sześciocyfrowych kodów BLIK; raporty i logi nie powinny przechowywać całej treści bez potrzeby. |
| Brak wiarygodnej walidacji | Model treningowy ma tylko 16 ręcznych przykładów, a dostępne zbiory nie tworzą reprezentatywnego, polskiego testu skanera. | Do oceny należy policzyć precision, recall, fałszywie pozytywne i fałszywie negatywne wyniki na niezależnym zbiorze oznaczonym przez eksperta. |

Testy demonstracyjne potwierdzają wybrane zachowania lokalnego silnika: uznanie ostrego, obfuskowanego lub podejrzanego komunikatu za ryzykowny, zachowanie wyników co najmniej 60 dla potwierdzonego BLIK i 90 dla typosquattingu oraz redagowanie kodu BLIK w `reasons`. Nie są one jednak badaniem statystycznym jakości wykrywania. Do zbadania ryzyka operacyjnego wymagane jest osobne oznaczenie wiadomości, rozdzielenie zbiorów treningowego i testowego oraz raportowanie macierzy błędów.

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
