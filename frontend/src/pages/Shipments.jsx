import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { get, post } from "../api.js";
import Notice from "../components/Notice.jsx";
import Stars from "../components/Stars.jsx";
import useAction from "../components/useAction.js";
import DisputePhotoPicker from "../components/DisputePhotoPicker.jsx";
import { uploadDisputePhotos } from "../components/disputePhotos.js";
import { dateTime } from "../format.js";

function ShipmentCard({ s, reload }) {
  const { run, error, ok, busy, setError, setOk } = useAction();
  const [tracking, setTracking] = useState("");
  const [reason, setReason] = useState("");
  const [files, setFiles] = useState([]);
  const [score, setScore] = useState(5);
  const [comment, setComment] = useState("");
  const [panel, setPanel] = useState(null); // "dispute" | "rate"
  const isSeller = s.role === "Seller";

  // Ocenjivanje je moguće tek kada je aukcija zaključena (Settled), jednom po korisniku i aukciji.
  const canRate = s.auctionStatus === "Settled" && !s.rated;
  const [counterpart, setCounterpart] = useState(null);
  useEffect(() => {
    if (!s.counterpartId) return;
    get(`/users/${s.counterpartId}/rating`).then(setCounterpart).catch(() => setCounterpart(null));
  }, [s.counterpartId]);

  const act = (fn, msg) => async () => {
    await run(fn, msg);
    reload();
  };

  // Prvo se otvara spor, pa se priložene slike otpremaju jedna po jedna.
  // Ako neka slika ne uspe, spor ostaje otvoren, a slike se mogu dodati naknadno u delu "Sporovi".
  const submitDispute = async () => {
    await run(async () => {
      const { id } = await post(`/auctions/${s.auctionId}/disputes`, { reason });
      const failed = files.length ? await uploadDisputePhotos(id, files) : [];
      if (failed.length) {
        setError(`Spor je otvoren, ali ${failed.length} od ${files.length} slika nije otpremljeno (${failed[0]}). Slike možete dodati naknadno u delu „Sporovi“.`);
      } else {
        setOk(files.length ? "Spor je otvoren, slike su priložene." : "Spor je otvoren.");
      }
      setFiles([]);
    });
    reload();
  };

  return (
    <div className="card">
      <div className="row between">
        <h3>{s.itemTitle}</h3>
        <span className="badge">{isSeller ? "Vi prodajete" : "Vi kupujete"}</span>
      </div>
      <p className="small">
        Rok za slanje: {dateTime(s.dueDate)}
        {s.shippedAt && <> · Poslato: {dateTime(s.shippedAt)} (praćenje: {s.trackingNumber})</>}
        {s.deliveredAt && <> · Isporučeno: {dateTime(s.deliveredAt)}</>}
      </p>
      <p><Link to={`/auctions/${s.auctionId}`}>Detalji aukcije</Link></p>

      {isSeller && !s.shippedAt && (
        <div className="row">
          <input placeholder="Broj za praćenje pošiljke" value={tracking} onChange={(e) => setTracking(e.target.value)} />
          <button disabled={busy || !tracking.trim()} onClick={act(() => post(`/auctions/${s.auctionId}/ship`, { trackingNumber: tracking }), "Označeno kao poslato.")}>
            Označi kao poslato
          </button>
        </div>
      )}
      {!isSeller && s.shippedAt && !s.deliveredAt && (
        <button disabled={busy} onClick={act(() => post(`/auctions/${s.auctionId}/confirm-delivery`), "Prijem je potvrđen.")}>Potvrdi prijem</button>
      )}

      <div className="row">
        <button className="secondary" onClick={() => setPanel(panel === "dispute" ? null : "dispute")}>Otvori spor</button>
        {canRate && (
          <button className="secondary" onClick={() => setPanel(panel === "rate" ? null : "rate")}>Oceni {isSeller ? "kupca" : "prodavca"}</button>
        )}
      </div>

      {panel === "dispute" && (
        <div className="form inner">
          <textarea rows={3} placeholder="Razlog spora" value={reason} onChange={(e) => setReason(e.target.value)} />
          <DisputePhotoPicker files={files} setFiles={setFiles} disabled={busy} />
          <button disabled={busy || reason.trim().length < 10} onClick={submitDispute}>Pošalji spor</button>
        </div>
      )}
      {s.rated && <p className="success small">✔ Ocenili ste {isSeller ? "kupca" : "prodavca"} za ovu aukciju.</p>}
      {!s.rated && s.auctionStatus !== "Settled" && (
        <p className="muted small">Ocenjivanje {isSeller ? "kupca" : "prodavca"} postaje moguće kada kupac potvrdi prijem i aukcija se zaključi.</p>
      )}
      {panel === "rate" && canRate && (
        <div className="form inner">
          {counterpart && (
            <p className="small">
              Ocenjujete: <Link to={`/users/${counterpart.id}`}>{counterpart.displayName}</Link>{" "}
              <Stars value={counterpart.averageRating} count={counterpart.ratingsCount} />
            </p>
          )}
          <label>Ocena
            <select value={score} onChange={(e) => setScore(Number(e.target.value))}>
              {[5, 4, 3, 2, 1].map((n) => <option key={n} value={n}>{"★".repeat(n)} ({n})</option>)}
            </select>
          </label>
          <textarea rows={2} placeholder="Komentar (opciono)" value={comment} onChange={(e) => setComment(e.target.value)} />
          <button disabled={busy} onClick={act(() => post(`/auctions/${s.auctionId}/ratings`, { score, comment: comment || null }), "Hvala na oceni.")}>Pošalji ocenu</button>
          <p className="muted small">Ocena 1–5 zvezdica; prosek se računa iz svih ocena koje korisnik dobije.</p>
        </div>
      )}
      <Notice error={error} ok={ok} />
    </div>
  );
}

export default function Shipments() {
  const [list, setList] = useState(null);
  const [error, setError] = useState("");
  const load = useCallback(() => {
    get("/shipments/mine").then(setList).catch((e) => setError(e.message));
  }, []);
  useEffect(load, [load]);

  return (
    <>
      <h2>Pošiljke</h2>
      <Notice error={error} />
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Nema pošiljki. Pošiljka nastaje nakon uplate.</p> : list.map((s) => <ShipmentCard key={s.id} s={s} reload={load} />)}
    </>
  );
}
