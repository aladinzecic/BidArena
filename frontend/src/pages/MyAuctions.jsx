import { useCallback, useEffect, useState } from "react";
import { get, post } from "../api.js";
import AuctionCard from "../components/AuctionCard.jsx";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";

function Actions({ auction, reload }) {
  const { run, error, ok, busy } = useAction();
  const act = (path, msg) => async () => {
    await run(() => post(`/auctions/${auction.id}/${path}`), msg);
    reload();
  };
  return (
    <div>
      <div className="row">
        {auction.status === "Draft" && <button disabled={busy} onClick={act("activate", "Aktivirano.")}>Aktiviraj</button>}
        {["Draft", "Scheduled", "Active"].includes(auction.status) && (
          <button className="danger" disabled={busy} onClick={act("cancel", "Otkazano.")}>Otkaži</button>
        )}
      </div>
      <Notice error={error} ok={ok} />
    </div>
  );
}

export default function MyAuctions() {
  const [list, setList] = useState(null);
  const [error, setError] = useState("");
  const load = useCallback(() => {
    get("/auctions/mine").then(setList).catch((e) => setError(e.message));
  }, []);
  useEffect(load, [load]);

  return (
    <>
      <h2>Moje aukcije</h2>
      <Notice error={error} />
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Još nemate aukcija. Napravite ih na stranici „Moji predmeti“.</p> : (
        <div className="grid">
          {list.map((a) => (
            <AuctionCard key={a.id} auction={a}>
              <Actions auction={a} reload={load} />
            </AuctionCard>
          ))}
        </div>
      )}
      <p className="muted small">Slanje predmeta za završene aukcije radite na stranici „Pošiljke“.</p>
    </>
  );
}
