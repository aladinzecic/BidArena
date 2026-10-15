import { api } from "../api.js";

// Mora da se poklapa sa backendom (AddDisputePhotoCommand / AddItemPhotoCommand).
export const MAX_DISPUTE_PHOTOS = 5;
export const MAX_PHOTO_BYTES = 5 * 1024 * 1024;
export const PHOTO_TYPES = ["image/jpeg", "image/png", "image/webp"];

// Vraća poruku o grešci ili "" ako su fajlovi u redu. `attached` = koliko slika spor već ima.
export function checkPhotoFiles(files, attached = 0) {
  if (attached + files.length > MAX_DISPUTE_PHOTOS) {
    return `Spor može imati najviše ${MAX_DISPUTE_PHOTOS} slika.`;
  }
  for (const f of files) {
    if (!PHOTO_TYPES.includes(f.type)) return `„${f.name}“: dozvoljeni formati su JPEG, PNG i WebP.`;
    if (f.size > MAX_PHOTO_BYTES) return `„${f.name}“: slika može imati najviše 5 MB.`;
  }
  return "";
}

// Otprema slike jednu po jednu; vraća listu poruka o greškama (prazna lista = sve je uspelo).
export async function uploadDisputePhotos(disputeId, files) {
  const failed = [];
  for (const file of files) {
    const form = new FormData();
    form.append("file", file);
    try {
      await api(`/disputes/${disputeId}/photos`, { method: "POST", form });
    } catch (e) {
      failed.push(`${file.name}: ${e.message}`);
    }
  }
  return failed;
}
