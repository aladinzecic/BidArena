import { useCallback, useEffect, useState } from "react";
import { del, get } from "../api.js";
import AuctionCard from "../components/AuctionCard.jsx";
import Notice from "../components/Notice.jsx";

export default function Watchlist() {
  const [list, setList] = useState(null);
  const [error, setError] = useState("");
  const load = useCallback(() => {
    get("/watchlist/mine").then(setList).catch((e) => setError(e.message));
  }, []);
  useEffect(load, [load]);

  async function remove(id) {
    try {
      await del(`/watchlist/${id}`);
      load();
    } catch (e) {
      setError(e.message);
    }
  }

  return (
    <>
      <h2>Praćene aukcije</h2>
      <Notice error={error} />
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Ne pratite nijednu aukciju.</p> : (
        <div className="grid">
          {list.map((a) => (
            <AuctionCard key={a.id} auction={a}>
              <button className="secondary" onClick={() => remove(a.id)}>Ukloni</button>
            </AuctionCard>
          ))}
        </div>
      )}
    </>
  );
}
