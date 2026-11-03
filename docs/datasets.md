# Publiczne zbiory danych do projektu AntiScam

Zbiory pochodzą z [UCI Machine Learning Repository](https://archive.ics.uci.edu/). Można je wykorzystać do desk research, analizy statystycznej i eksperymentów z klasyfikacją spamu/phishingu. Pobieranie zapisuje oryginalne cechy oraz etykiety do CSV w `data/`; pliki nie są wymagane do uruchomienia API.

## Dostępne zbiory

| Zbiór | Rozmiar i zawartość | Zastosowanie w AntiScam | Źródło |
| --- | --- | --- | --- |
| SMS Spam Collection | 5 574 rekordy według UCI; eksportowany CSV ma 5 572 niepuste wiadomości SMS, etykiety spam/ham | Analiza treści wiadomości i punkt wyjścia do porównania słownika wiadomości spamowych z regułami skanera. Zbiór zawiera wiadomości angielskie, więc nie zastępuje walidacji polskich danych. | [UCI, DOI 10.24432/C5CC84](https://archive.ics.uci.edu/dataset/228/sms%2Bspam%2Bcollection) |
| Phishing Websites | 11 055 rekordów, 30 cech witryn, etykieta klasy | Statystyki cech phishingowych witryn i porównanie ich z analizą domen/linków w `antiscam/links.py`. | [UCI, DOI 10.24432/C51W2X](https://archive.ics.uci.edu/dataset/327/phishing) |
| PhiUSIIL Phishing URL (Website) | 235 795 rekordów: 134 850 legalnych i 100 945 phishingowych URL-i, 54 cechy | Większa próba do statystyk, korelacji i eksperymentów klasyfikacyjnych dotyczących URL-i. | [UCI, dataset 967](https://archive.ics.uci.edu/dataset/967/phiusiil%26) |

Wszystkie trzy rekordy UCI wskazują licencję **CC BY 4.0**. Przy publikacji analiz lub przetworzonych kopii podaj autorów i cytowanie wskazane na stronie danego zbioru. Zbiory są historyczne i nie powinny służyć jako aktualna lista aktywnych zagrożeń.

## Pobranie do `data/`

Z katalogu głównego projektu zainstaluj opcjonalny dodatek i uruchom pobieranie:

```powershell
python -m pip install -e ".[datasets]"
python tools/fetch_datasets.py
```

Skrypt korzysta z oficjalnego klienta `ucimlrepo` i zapisuje:

- `data/uci_sms_spam_collection.csv`
- `data/uci_phishing_websites.csv`
- `data/uci_phiusiil_phishing_url.csv`

Wymagane jest połączenie z internetem. Duży zbiór PhiUSIIL pobieraj tylko wtedy, gdy potrzebujesz większej próby; pozostałe dwa wystarczą do krótkiego ćwiczenia.

## Propozycje analiz

- policz udział klas spam/ham w SMS Spam Collection i porównaj wyniki dla wiadomości krótkich i długich;
- porównaj częstość podejrzanych sygnałów między etykietami, np. wzmianki o nagrodzie, pilności, płatności lub linku;
- dla zbiorów witryn policz rozkłady wybranych cech oraz ich zależność od etykiety phishingowej;
- porównaj mały i duży zbiór URL-i, sprawdzając, czy rozkłady klas i cech są podobne;
- jawnie opisz ograniczenia: język i wiek SMS-ów, sposób zebrania próby oraz to, że cechy datasetu nie są równoważne bieżącej skuteczności produkcyjnego filtra.

Zbiory witryn/URL-i zawierają gotowe cechy tabelaryczne. Nie są bezpośrednim materiałem uczącym obecnego tekstowego modelu TF-IDF; służą do analiz statystycznych lub do osobnego eksperymentu z modelem klasyfikacji URL-i.

## Automatyczna analiza eksploracyjna

Aby przeanalizować wszystkie pliki CSV znajdujące się bezpośrednio w `data/`, zainstaluj pandas i uruchom skrypt z katalogu projektu:

```powershell
python -m pip install -e ".[analysis]"
python tools/analyze_datasets.py
```

Raporty CSV powstaną w `reports/data-analysis/`: podsumowanie zbiorów, profil kolumn, statystyki cech numerycznych i rozkłady rozpoznanych etykiet (`label`, `result`, `class` lub `target`). Dla kolumn `text`, `message`, `sms` i `url` skrypt podaje również długości treści. Katalogi można zmienić przez `--data-dir` i `--output-dir`. Analiza jest eksploracyjna — nie trenuje ani nie ocenia modelu.
