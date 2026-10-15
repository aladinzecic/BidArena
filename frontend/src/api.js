// Tanak klijent za BidArena API: JWT u localStorage, automatski refresh na 401.
const BASE = "/api/v1";
const KEY = "bidarena.tokens";

export function getTokens() {
  try {
    return JSON.parse(localStorage.getItem(KEY)) || null;
  } catch {
    return null;
  }
}

export function setTokens(t) {
  if (t) localStorage.setItem(KEY, JSON.stringify(t));
  else localStorage.removeItem(KEY);
}

export function decodeJwt(token) {
  try {
    const part = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const json = decodeURIComponent(
      atob(part)
        .split("")
        .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
        .join("")
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

export class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
  }
}

let refreshing = null;
let onSessionExpired = () => {};
export function setSessionExpiredHandler(fn) {
  onSessionExpired = fn;
}

async function refreshTokens() {
  const tokens = getTokens();
  if (!tokens?.refreshToken) return false;
  if (!refreshing) {
    refreshing = fetch(`${BASE}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: tokens.refreshToken }),
    })
      .then(async (res) => {
        if (!res.ok) return false;
        setTokens(await res.json());
        return true;
      })
      .catch(() => false)
      .finally(() => {
        refreshing = null;
      });
  }
  return refreshing;
}

async function parseError(res) {
  let message = `Greška ${res.status}`;
  try {
    const data = await res.json();
    if (data.errors) {
      // ASP.NET validation problem details: { errors: { Field: [msgs] } }
      message = Object.values(data.errors).flat().join(" ");
    } else {
      message = data.detail || data.title || message;
    }
  } catch {
    /* telo nije JSON */
  }
  if (res.status === 429) message = "Previše zahteva, pokušajte ponovo za minut.";
  if (res.status === 403 && message.startsWith("Greška")) message = "Nemate dozvolu za ovu akciju.";
  return new ApiError(message, res.status);
}

export async function api(path, { method = "GET", body, form, auth = true } = {}) {
  const doFetch = () => {
    const headers = {};
    const tokens = getTokens();
    if (auth && tokens?.accessToken) headers.Authorization = `Bearer ${tokens.accessToken}`;
    let payload;
    if (form) {
      payload = form;
    } else if (body !== undefined) {
      headers["Content-Type"] = "application/json";
      payload = JSON.stringify(body);
    }
    return fetch(`${BASE}${path}`, { method, headers, body: payload });
  };

  let res = await doFetch();

  if (res.status === 401 && auth && getTokens()) {
    if (await refreshTokens()) {
      res = await doFetch();
    } else {
      setTokens(null);
      onSessionExpired();
    }
  }

  if (!res.ok) throw await parseError(res);
  if (res.status === 204) return null;
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export const get = (p) => api(p);
export const post = (p, body) => api(p, { method: "POST", body });
export const put = (p, body) => api(p, { method: "PUT", body });
export const del = (p) => api(p, { method: "DELETE" });
