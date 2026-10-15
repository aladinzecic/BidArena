# BidArena: stanje projekta

Backend ~100%, frontend (React) ~100% funkcionalno. Pitanja i odgovori (Q&A) su vraćeni u obim, vidi „Pitanja i odgovori“ ispod.

## VAŽNO pre prvog pokretanja
Kod u ovom okruženju nije kompajliran (nije bilo .NET SDK-a ni npm-a), pa redom:

```bash
# 1. nova migracija (tabele Ratings i Questions, User.RatingsCount, Notification.AuctionId)
dotnet ef migrations add AddRatingsQuestionsAndNotificationAuction --project src/BidArena.Infrastructure --startup-project src/BidArena.Api
dotnet ef database update --project src/BidArena.Infrastructure --startup-project src/BidArena.Api

# 2. build + testovi
dotnet build BidArena.slnx
dotnet test  BidArena.slnx

# 3. frontend
cd frontend && npm install && npm run dev
```

Ako build javi grešku, pošalji tačan tekst greške i ispravlja se.

## Šta je dodato u ovoj fazi
1. **Ocenjivanje**: `POST /api/v1/Auctions/{id}/ratings` (samo Settled; prodavac i pobednik ocenjuju jedno drugo, jedna ocena po aukciji). `AverageRating` i `RatingsCount` se ažuriraju (pokretni prosek).
2. **Verifikacija i način plaćanja**: `PlaceBid` zahteva `IsVerified` + `HasPaymentMethod`, `CreateAuction` zahteva verifikovanog prodavca (403 inače). Novo: `GET /Users/me`, `POST /Users/me/payment-method` (simulacija), `GET /Admin/users/unverified`.
3. **AuctionEndingSoon**: `AuctionEndingSoonWorker` (1 min) obaveštava pratioce i lidera 60 min pre kraja, jednom po korisniku i aukciji (`Notification.AuctionId`).
4. **Rok slanja**: `ShipmentOverdueWorker` (1 min): neposlata pošiljka posle roka → aukcija Cancelled, Payment Refunded, prodavcu +1 `CancelledAuctionsCount`, obe strane dobijaju obaveštenje.
5. **Ishod spora**: `POST /Admin/disputes/{id}/resolve` prima `outcome`: `FavorSeller` (Settled, prodavcu +1 uspešna) ili `RefundBuyer` (Payment Refunded, aukcija Cancelled, prodavcu +1 otkazana). Ishod je u prefiksu `Resolution` teksta.
6. **Dutch aukcija**: cena pada za `BidIncrement` svakog minuta od `StartTime`, do donje granice (`ReservePrice`, obavezna za Dutch i manja od početne). Prvi ponuđač koji prihvati trenutnu cenu pobeđuje; `EndTime = now`, a `AuctionClosingWorker` pravi Payment. Prikazana cena se računa u mapiranju (`DutchPricing`).
7. **Sitnice**: rate limiting na `/Auth/login` i `/Auth/register` (10/min po IP, 429), lozinka za dev bazu prebačena u `appsettings.Development.json`, folder `Exeptions` → `Exceptions`, novi `GET /Auctions/mine` (aukcije prodavca), novi testovi (ocene, Dutch, rok slanja, ishod spora), ažurirani testovi za nove konstruktore.

## Bez SignalR-a
SignalR/WebSocket je uklonjen (`Hubs/`, `IAuctionNotifier`, paket `@microsoft/signalr`, `/hubs` proxy). Frontend koristi polling, vidi README. Obaveštenja se i dalje čuvaju u bazi.

## Pitanja i odgovori (Q&A)
- `GET /api/v1/Auctions/{id}/questions` (javno): sva pitanja aukcije sa odgovorima.
- `POST /api/v1/Auctions/{id}/questions` (prijavljen Seller/Bidder, ne vlasnik aukcije): postavlja pitanje (5-1000 karaktera), samo dok je aukcija `Scheduled` ili `Active`.
- `POST /api/v1/Auctions/{id}/questions/{questionId}/answer` (**samo prodavac te aukcije**, 403 za sve ostale): jedan odgovor po pitanju (2000 karaktera), ponovno odgovaranje vraća 409.
- Obaveštenja: prodavac dobija `QuestionAsked`, korisnik koji je pitao dobija `QuestionAnswered`.
- Tabela `Questions` (entitet je već postojao) ulazi u novu migraciju iz koraka 1 (`RemoveQuestions` ju je ranije obrisala). Testovi: `Questions/QuestionFlowTests.cs`, `Validators/QuestionValidatorsTests.cs`.
- Frontend: sekcija „Pitanja i odgovori“ na stranici aukcije (forma za pitanje; forma za odgovor vidi samo prodavac).

## Kategorije (enum)
- `ItemCategory` (Domain/Entities/Item.cs): Satovi, Nakit, Elektronika, Racunari, Odeca, Obuca, Namestaj, DomIBasta, Umetnost, Kolekcionarstvo, Knjige, Muzika, Vozila, Sport, Igracke, Ostalo.
- API i dalje prima/vraća naziv enuma kao string; validatori odbijaju sve što nije u enumu (i brojeve). Filter `GET /Auctions?category=...` takođe validira vrednost.
- U bazi se čuva kao tekst (`ItemConfiguration`, `HasConversion<string>()`), pa se šema ne menja. Stare redove sa slobodnim tekstom treba prebaciti na `Ostalo` (vidi SQL u odgovoru/PR opisu).
- Frontend: padajuće liste u formi predmeta i u filteru aukcija; lista je u `frontend/src/format.js` (`CATEGORIES`) i mora se poklapati sa enumom.

## Frontend (`frontend/`)
React + Vite: prijava/registracija (JWT, automatski refresh), aukcije sa filterima i paginacijom, detalji sa ponudama (polling na 4 s) i countdown-om, proxy bid / Dutch prihvatanje cene, moji predmeti (CRUD, upload slika, pravljenje aukcije), moje aukcije (aktiviraj/otkaži), praćenje, plaćanja, pošiljke (slanje, potvrda prijema, spor, ocena), sporovi, obaveštenja + toast (polling na 15 s), profil, admin panel.

## Poznata ograničenja
- Ocena se računa kao pokretni prosek (bez izmene/brisanja ocene).
- Dutch aukcija se zatvara preko workera (do ~30 s kašnjenja između prihvatanja cene i kreiranja uplate).
- Nedostaju testovi za Watchlist, Notifications, Admin i GetAuctions/GetAuctionById handlere.

## Slike uz spor (reklamaciju)
- Novi entitet `DisputePhoto` + migracija `20261008190000_AddDisputePhotos` (ručno napisana, kod nije kompajliran u ovom okruženju). Primeni je običnim `dotnet ef database update`; migracija iz koraka 1 gore (Ratings/Questions) ostaje posebna i kreira se komandom `migrations add`.
- `POST /api/v1/Disputes/{id}/photos` (multipart, polje `file`): JPEG/PNG/WebP do 5 MB, najviše 5 slika po sporu; samo korisnik koji je otvorio spor, dok spor nije `Resolved`. Fajlovi idu u `uploads/disputes/{disputeId}/`.
- `DisputeDto` ima novo polje `Photos` (`/Disputes/mine` i `/Admin/disputes`).
- Frontend: forma „Otvori spor“ (stranica Pošiljke) ima izbor slika; stranica Sporovi prikazuje slike i omogućava dodavanje naknadno; admin vidi slike uz spor. Testovi: `Disputes/DisputePhotoTests.cs`, dva mapping testa.

## Ocene korisnika (prikaz)
- `GET /api/v1/Users/{id}/rating` (javno): ime, verifikovan, prosečna ocena, broj ocena, uspešne/otkazane aukcije, poslednjih 50 ocena (bez imena ocenjivača).
- `GET /Shipments/mine` sada vraća i `auctionStatus`, `rated` i `counterpartId`; dugme „Oceni“ se prikazuje samo kad je aukcija `Settled` i korisnik još nije ocenio.
- Frontend: prodavac sa ocenom na stranici aukcije, javna stranica `/users/:id`. Test: `Ratings/GetUserRatingQueryHandlerTests.cs`. Nema nove migracije za ovu izmenu.

## Obrada grešaka: pravi middleware
- `GlobalExceptionHandler` (`IExceptionHandler` + `UseExceptionHandler()`) zamenjen je klasom `Middleware/ExceptionHandlingMiddleware` (konvencionalni middleware sa `RequestDelegate`) i ekstenzijom `UseExceptionHandling()`. U `Program.cs` uklonjen je `AddExceptionHandler`; `AddProblemDetails` je ostao.
- Mapiranje statusa je isto kao ranije (400/401/403/404/409/500), uz tri ispravke: 503 za prolazne kvarove baze i timeout (ranije 409), `BadHttpRequestException` zadržava svoj status (npr. 413), 499 kad klijent prekine zahtev. Odgovor je sada `application/problem+json` (ranije `application/json`); frontend čita `title`/`detail`/`errors`, pa se ništa ne menja za njega.
- `AuctionPhaseMiddleware` (pravi middleware, posle autorizacije) sprovodi `[AllowedPhases]`; faza se kešira 5 s u `IMemoryCache`, a handleri i dalje sami proveravaju stanje u transakciji.
- Testovi: `tests/BidArena.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs` (17 slučajeva). Test projekat sada referencira `BidArena.Api`.
