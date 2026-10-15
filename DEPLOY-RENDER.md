# Besplatan hosting: Render + Neon + Cloudflare R2

Arhitektura: **Render** (API u Dockeru + frontend kao static site), **Neon** (PostgreSQL), **R2** (slike).
Frontend i API su za browser na istom domenu: static site prosleđuje `/api/*` ka API servisu.

## 1. Baza (Neon)
1. Napravi nalog i projekat na neon.tech, bazu ostavi na podrazumevanom imenu.
2. Iz "Connection details" uzmi **direct** host (bez `-pooler`), zbog EF migracija.
3. Složi connection string (Npgsql format):
   `Host=ep-xxxx.region.aws.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Timeout=30`

## 2. Slike (Cloudflare R2)
1. Cloudflare → R2 → napravi bucket (npr. `bidarena`). R2 traži unos načina plaćanja da bi se uključio, ali besplatni nivo važi.
2. Bucket → Settings → **Public access** → uključi `r2.dev` poddomen. Prepiši javni URL (`https://pub-....r2.dev`).
3. R2 → Manage API tokens → token sa pravom *Object Read & Write* za taj bucket. Zapiši Access Key ID i Secret.
4. `ServiceUrl` je `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`.

## 3. Render
1. Stavi projekat na GitHub (`.env` ne sme u repozitorijum).
2. Render → New → **Blueprint** → izaberi repozitorijum; učitaće `render.yaml`.
3. Za `bidarena-api` unesi vrednosti: `ConnectionStrings__DefaultConnection`, `R2__ServiceUrl`, `R2__Bucket`, `R2__AccessKeyId`, `R2__SecretAccessKey`, `R2__PublicBaseUrl`. `Jwt__Key` se generiše sam.
4. Ako Render ne da ime `bidarena-api`, promeni ga u `render.yaml` i u `destination` rewrite pravila.
5. Posle deploy-a otvori URL servisa `bidarena-web`. Migracije se primenjuju pri prvom startu API-ja.

## 4. Da aukcije ne "zaspu"
Besplatan Render servis spava posle 15 min bez saobraćaja, a tada ne rade ni radnici koji zatvaraju i pokreću aukcije.
Postavi besplatan monitor (npr. UptimeRobot) da na 5 minuta poziva `https://bidarena-api.onrender.com/health`.
Jedan stalno budan servis troši ~744 od 750 besplatnih sati mesečno.

## Ograničenja
- Prvi zahtev posle spavanja traje oko minut.
- Neon besplatan nivo ima mesečni limit računarskog vremena i uspavljuje bazu; proveri trenutne limite na njihovom sajtu.
- Stare slike sa lokalnog diska ne prelaze na R2 automatski.
