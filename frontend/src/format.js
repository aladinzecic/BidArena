export function parseUtc(value) {
  if (!value) return null;
  const s = String(value);
  return new Date(/[zZ]|[+-]\d\d:?\d\d$/.test(s) ? s : s + "Z");
}

export const money = (n) =>
  new Intl.NumberFormat("sr-RS", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(n ?? 0));

export const dateTime = (v) => {
  const d = parseUtc(v);
  return d ? d.toLocaleString("sr-RS") : "-";
};

export const STATUS_LABELS = {
  Draft: "Nacrt",
  Scheduled: "Zakazana",
  Active: "Aktivna",
  Ended: "Završena",
  Settled: "Zaključena",
  Disputed: "U sporu",
  Cancelled: "Otkazana",
  Pending: "Na čekanju",
  Paid: "Plaćeno",
  Overdue: "Isteklo",
  Refunded: "Vraćeno",
  Open: "Otvoren",
  UnderReview: "U razmatranju",
  Resolved: "Rešen",
};

export const CONDITIONS = [
  ["Novo", "Novo"],
  ["KaoNovo", "Kao novo"],
  ["Koriscen", "Korišćeno"],
  ["ZaPopravku", "Za popravku"],
];

export const conditionLabel = (c) => CONDITIONS.find(([v]) => v === c)?.[1] ?? c;

// Mora da se poklapa sa enumom ItemCategory u backendu (vrednost, prikazani naziv).
export const CATEGORIES = [
  ["Satovi", "Satovi"],
  ["Nakit", "Nakit"],
  ["Elektronika", "Elektronika"],
  ["Racunari", "Računari"],
  ["Odeca", "Odeća"],
  ["Obuca", "Obuća"],
  ["Namestaj", "Nameštaj"],
  ["DomIBasta", "Dom i bašta"],
  ["Umetnost", "Umetnost"],
  ["Kolekcionarstvo", "Kolekcionarstvo"],
  ["Knjige", "Knjige"],
  ["Muzika", "Muzika"],
  ["Vozila", "Vozila"],
  ["Sport", "Sport"],
  ["Igracke", "Igračke"],
  ["Ostalo", "Ostalo"],
];

export const categoryLabel = (c) => CATEGORIES.find(([v]) => v === c)?.[1] ?? c;

export const toIsoUtc = (localValue) => (localValue ? new Date(localValue).toISOString() : null);
