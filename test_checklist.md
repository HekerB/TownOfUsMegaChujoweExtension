# Plan Testów Stabilności - TouMegaChujoweExtension

Ten dokument zawiera listę kluczowych scenariuszy do przetestowania po ostatnich poprawkach stabilności i UI.

## 1. Krytyczne (Zapobieganie Crashom)
- [ ] **Uruchomienie gry:** Sprawdź, czy gra wczytuje się do menu głównego bez błędu "Assembly not registered".
- [ ] **Wejście do lobby:** Sprawdź, czy po wejściu do lobby i wybraniu roli nie następuje crash.
- [ ] **Rozpoczęcie rundy:** Sprawdź, czy po starcie rundy (Intro) gra działa płynnie.
- [ ] **Zduplikowany plik:** Upewnij się, że w folderze `plugins` **NIE MA** pliku `TouMegaChujoweExtension (1).dll`. Jeśli jest - usuń go przed testami!

## 2. Mirror Caster (Naprawa UI)
- [ ] **Targeting (Ekran celowania):** Otwórz ekran celowania. 
    - [ ] Czy możesz poruszać się za pomocą WASD przy otwartym menu?
    - [ ] Czy wybranie osoby (kliknięcie ikony na mapie/ekranie) działa za **pierwszym razem**?
    - [ ] Czy po wybraniu osoby ekran znika natychmiast?
- [ ] **Tarcza:** Sprawdź, czy po nadaniu tarczy, Mirror Caster dostaje cooldown, a cel dostaje efekt wizualny.

## 3. Role i Coroutines (Stabilność IL2CPP)
Przetestuj użycie umiejętności poniższych ról, aby upewnić się, że coroutines nie wywalają gry:
- [ ] **Prawnik (Lawyer):** Nadaj komuś status klienta.
- [ ] **Wraith:** Użyj teleportacji/umiejętności specjalnej.
- [ ] **Hacker:** Użyj zakłócania (Jamming).
- [ ] **Kamikaze:** Zdetonuj się.
- [ ] **Vulture:** Zjedz ciało.
- [ ] **Vampire Hunter:** Użyj kołka (Stake) na wampirze (sprawdź też animację).
- [ ] **Trapper:** Rozstaw pułapkę.

## 4. UI i Przyciski (NullReferenceException)
- [ ] **Venting (Wentylacja):** Wejdź i wyjdź z wentylacji jako rola, która ma limit użyć (np. Serial Killer / Ventable). Sprawdź, czy cooldown na przycisku odświeża się poprawnie i nie wywala gry.
- [ ] **Falcon:** Użyj Zoomu. Sprawdź, czy przycisk nie wywala błędu przy przechodzeniu między pokojami.
- [ ] **Egotist:** Sprawdź, czy przycisk wentylacji działa poprawnie.

## 5. Czat i Lokalicja
- [ ] **Czat Kochanków:** Jeśli jesteś w parze kochanków, spróbuj wysłać wiadomość na dedykowanym czacie podczas spotkania.
- [ ] **Mayor (Burmistrz):** Zagłosuj jako Mayor. Sprawdź, czy w HUD wyświetla się poprawnie liczba dostępnych głosów (powinno być np. "Votes: 3/5").

## 6. Logi
- [ ] Jeśli gra zлагаuje lub zcrashuje, sprawdź końcówkę pliku `LogOutput.log`. Szukaj fraz takich jak `NullReferenceException` lub `OutOfMemoryException`.

---
*Status: Kod został zweryfikowany kompilacją `dotnet build` - brak błędów składniowych.*
