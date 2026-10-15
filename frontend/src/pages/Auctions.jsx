import { useEffect, useState } from "react";
import { get } from "../api.js";
import AuctionCard from "../components/AuctionCard.jsx";
import Notice from "../components/Notice.jsx";
import { CATEGORIES, STATUS_LABELS } from "../format.js";

const STATUSES = ["Active", "Scheduled", "Ended", "Settled", "Cancelled"];

export default function Auctions() {
  const [filters, setFilters] = useState({ search: "", category: "", status: "Active" });
  const [applied, setApplied] = useState(filters);
  const [page, setPage] = useState(1);
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    const q = new URLSearchParams({ status: applied.status, page, pageSize: 12 });
    if (applied.search) q.set("search", applied.search);
    if (applied.category) q.set("category", applied.category);
    get(`/auctions?${q}`)
      .then((d) => {
        setData(d);
        setError("");
      })
      .catch((e) => setError(e.message));
  }, [applied, page]);

  // Cene holandskih aukcija padaju, pa se lista povremeno osvežava.
  useEffect(() => {
    if (applied.status !== "Active") return undefined;
    const id = setInterval(() => setApplied((a) => ({ ...a })), 30000);
    return () => clearInterval(id);
  }, [applied.status]);

  function submit(e) {
    e.preventDefault();
    setPage(1);
    setApplied(filters);
  }

  return (
    <>
      <h2>Aukcije</h2>
      <form className="filters" onSubmit={submit}>
        <input placeholder="Pretraga po nazivu" value={filters.search} onChange={(e) => setFilters({ ...filters, search: e.target.value })} />
        <select value={filters.category} onChange={(e) => setFilters({ ...filters, category: e.target.value })}>
          <option value="">Sve kategorije</option>
          {CATEGORIES.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
        </select>
        <select value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}>
          {STATUSES.map((s) => <option key={s} value={s}>{STATUS_LABELS[s]}</option>)}
        </select>
        <button>Filtriraj</button>
      </form>
      <Notice error={error} />
      {!data ? <p>Učitavanje...</p> : data.items.length === 0 ? <p className="muted">Nema aukcija za izabrane filtere.</p> : (
        <div className="grid">
          {data.items.map((a) => <AuctionCard key={a.id} auction={a} />)}
        </div>
      )}
      {data && data.totalPages > 1 && (
        <div className="pager">
          <button disabled={page <= 1} onClick={() => setPage(page - 1)}>‹ Prethodna</button>
          <span>{page} / {data.totalPages}</span>
          <button disabled={page >= data.totalPages} onClick={() => setPage(page + 1)}>Sledeća ›</button>
        </div>
      )}
    </>
  );
}
