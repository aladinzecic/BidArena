# BidArena frontend (React + Vite)

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173
```

Dev server prosleđuje `/api` i `/uploads` na backend. Podrazumevani backend je
`http://localhost:5266` (launchSettings); za Docker (port 8080):

```bash
VITE_API_URL=http://localhost:8080 npm run dev      # PowerShell: $env:VITE_API_URL="http://localhost:8080"; npm run dev
```

Produkcija: `npm run build`, a `Dockerfile` + `nginx.conf` služe `dist` i proksiraju API (servis `api`).
Za rad iz browsera sa drugog origin-a dodaj ga u `Cors__AllowedOrigins__0`.

Stranice: prijava/registracija (JWT + automatski refresh), lista aukcija sa filterima, detalji sa ponudama koje se osvežavaju periodično
(polling), moji predmeti (CRUD + slike + pravljenje aukcije), moje aukcije, praćenje, plaćanja, pošiljke
(slanje, potvrda prijema, spor, ocena), sporovi, obaveštenja (+ toast, polling na 15 s), profil (način plaćanja, ocena),
admin panel (statistika, verifikacija, sporovi sa ishodom).
