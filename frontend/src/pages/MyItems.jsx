import { useCallback, useEffect, useState } from "react";
import { del, get, post, api, put } from "../api.js";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";
import { CATEGORIES, CONDITIONS, categoryLabel, conditionLabel, toIsoUtc } from "../format.js";

const emptyItem = { title: "", descriptionMarkdown: "", category: "", condition: "Novo", location: "" };

function ItemForm({ initial = emptyItem, submitLabel, onSubmit, onCancel }) {
  const [f, setF] = useState(initial);
  const { run, error, busy } = useAction();
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value });

  async function submit(e) {
    e.preventDefault();
    const ok = await run(async () => {
      await onSubmit(f);
      return true;
    });
    if (ok && !onCancel) setF(emptyItem);
  }

  return (
    <form className="card form" onSubmit={submit}>
      <label>Naziv<input value={f.title} onChange={set("title")} required /></label>
      <label>Opis (Markdown)<textarea rows={4} value={f.descriptionMarkdown} onChange={set("descriptionMarkdown")} required /></label>
      <div className="row">
        <label>Kategorija
          <select value={f.category} onChange={set("category")} required>
            <option value="" disabled>Izaberite kategoriju</option>
            {CATEGORIES.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
        </label>
        <label>Stanje
          <select value={f.condition} onChange={set("condition")}>
            {CONDITIONS.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
        </label>
        <label>Lokacija<input value={f.location} onChange={set("location")} required /></label>
      </div>
      <Notice error={error} />
      <div className="row">
        <button disabled={busy}>{submitLabel}</button>
        {onCancel && <button type="button" className="secondary" onClick={onCancel}>Odustani</button>}
      </div>
    </form>
  );
}

function AuctionForm({ item, onDone }) {
  const now = new Date();
  const local = (d) => new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  const [f, setF] = useState({
    type: "English",
    startingPrice: "",
    reservePrice: "",
    bidIncrement: "",
    startTime: local(now),
    endTime: local(new Date(now.getTime() + 3 * 24 * 3600 * 1000)),
  });
  const { run, error, busy } = useAction();
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value });
  const dutch = f.type === "Dutch";

  async function submit(e) {
    e.preventDefault();
    const ok = await run(() =>
      post("/auctions", {
        itemId: item.id,
        type: f.type,
        startingPrice: Number(f.startingPrice),
        reservePrice: f.reservePrice ? Number(f.reservePrice) : null,
        bidIncrement: Number(f.bidIncrement),
        startTime: toIsoUtc(f.startTime),
        endTime: toIsoUtc(f.endTime),
      })
    );
    if (ok) onDone();
  }

  return (
    <form className="card form inner" onSubmit={submit}>
      <strong>Nova aukcija za „{item.title}“</strong>
      <div className="row">
        <label>Tip
          <select value={f.type} onChange={set("type")}>
            <option value="English">Engleska (raste)</option>
            <option value="Dutch">Holandska (pada)</option>
          </select>
        </label>
        <label>Početna cena<input type="number" step="0.01" min="0.01" value={f.startingPrice} onChange={set("startingPrice")} required /></label>
        <label>{dutch ? "Donja granica cene (obavezno)" : "Rezervna cena (opciono)"}
          <input type="number" step="0.01" value={f.reservePrice} onChange={set("reservePrice")} required={dutch} /></label>
        <label>{dutch ? "Pad cene po minutu" : "Minimalni korak"}
          <input type="number" step="0.01" min="0.01" value={f.bidIncrement} onChange={set("bidIncrement")} required /></label>
      </div>
      <div className="row">
        <label>Početak<input type="datetime-local" value={f.startTime} onChange={set("startTime")} required /></label>
        <label>Kraj<input type="datetime-local" value={f.endTime} onChange={set("endTime")} required /></label>
      </div>
      <p className="muted small">Aukcija se pravi kao nacrt; aktivirajte je na stranici „Moje aukcije“. Kreiranje zahteva verifikovan nalog.</p>
      <Notice error={error} />
      <button disabled={busy}>Napravi aukciju</button>
    </form>
  );
}

function ItemRow({ item, reload }) {
  const [mode, setMode] = useState(null); // "edit" | "auction"
  const { run, error, ok, busy } = useAction();

  async function upload(e) {
    const file = e.target.files?.[0];
    if (!file) return;
    const form = new FormData();
    form.append("file", file);
    await run(() => api(`/items/${item.id}/photos`, { method: "POST", form }), "Slika je dodata.");
    e.target.value = "";
    reload();
  }

  async function removePhoto(photoId) {
    await run(() => del(`/items/${item.id}/photos/${photoId}`));
    reload();
  }

  async function removeItem() {
    if (!window.confirm("Obrisati predmet?")) return;
    await run(() => del(`/items/${item.id}`));
    reload();
  }

  return (
    <div className="card">
      <div className="row between">
        <div>
          <h3>{item.title}</h3>
          <div className="muted small">{categoryLabel(item.category)} · {conditionLabel(item.condition)} · {item.location}</div>
        </div>
        <div className="row">
          <button onClick={() => setMode(mode === "auction" ? null : "auction")}>Napravi aukciju</button>
          <button className="secondary" onClick={() => setMode(mode === "edit" ? null : "edit")}>Izmeni</button>
          <button className="danger" onClick={removeItem} disabled={busy}>Obriši</button>
        </div>
      </div>
      <div className="thumbs">
        {item.photos.map((p) => (
          <span key={p.id} className="photo-wrap">
            <img src={p.url} alt="" />
            <button className="x" title="Ukloni sliku" onClick={() => removePhoto(p.id)}>×</button>
          </span>
        ))}
      </div>
      <label className="small">Dodaj sliku (JPEG/PNG/WebP, do 5 MB)
        <input type="file" accept="image/jpeg,image/png,image/webp" onChange={upload} />
      </label>
      <Notice error={error} ok={ok} />
      {mode === "edit" && (
        <ItemForm
          initial={item}
          submitLabel="Sačuvaj"
          onCancel={() => setMode(null)}
          onSubmit={async (f) => {
            await put(`/items/${item.id}`, f);
            setMode(null);
            reload();
          }}
        />
      )}
      {mode === "auction" && <AuctionForm item={item} onDone={() => setMode(null)} />}
    </div>
  );
}

export default function MyItems() {
  const [items, setItems] = useState(null);
  const [error, setError] = useState("");

  const load = useCallback(() => {
    get("/items/mine")
      .then((d) => setItems(Array.isArray(d) ? d : d.items ?? []))
      .catch((e) => setError(e.message));
  }, []);
  useEffect(load, [load]);

  return (
    <>
      <h2>Moji predmeti</h2>
      <h3>Novi predmet</h3>
      <ItemForm submitLabel="Dodaj predmet" onSubmit={async (f) => { await post("/items", f); load(); }} />
      <Notice error={error} />
      <h3>Lista</h3>
      {!items ? <p>Učitavanje...</p> : items.length === 0 ? <p className="muted">Nemate predmeta.</p> : items.map((i) => <ItemRow key={i.id} item={i} reload={load} />)}
    </>
  );
}
