import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { get } from "../api.js";
import { useAuth } from "../auth.jsx";
import DisputePhotoPicker from "../components/DisputePhotoPicker.jsx";
import DisputePhotos from "../components/DisputePhotos.jsx";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";
import { MAX_DISPUTE_PHOTOS, uploadDisputePhotos } from "../components/disputePhotos.js";
import { STATUS_LABELS, dateTime } from "../format.js";

function DisputeCard({ d, canAddPhotos, reload }) {
  const { run, error, ok, busy, setError, setOk } = useAction();
  const [files, setFiles] = useState([]);
  const attached = d.photos?.length ?? 0;

  async function upload() {
    await run(async () => {
      const failed = await uploadDisputePhotos(d.id, files);
      if (failed.length) setError(`Nije otpremljeno ${failed.length} od ${files.length} slika: ${failed[0]}`);
      else setOk("Slike su dodate.");
      setFiles([]);
    });
    reload();
  }

  return (
    <div className="card">
      <div className="row between">
        <strong>Spor od {dateTime(d.createdAt)}</strong>
        <span className={`badge ${d.status}`}>{STATUS_LABELS[d.status] ?? d.status}</span>
      </div>
      <p>{d.reason}</p>
      <DisputePhotos photos={d.photos} />
      {d.resolution && <p><strong>Odluka:</strong> {d.resolution}</p>}
      {canAddPhotos && attached < MAX_DISPUTE_PHOTOS && (
        <div className="form inner">
          <DisputePhotoPicker files={files} setFiles={setFiles} attached={attached} disabled={busy} />
          <button disabled={busy || files.length === 0} onClick={upload}>Dodaj slike</button>
        </div>
      )}
      <Notice error={error} ok={ok} />
      <Link to={`/auctions/${d.auctionId}`}>Aukcija</Link>
    </div>
  );
}

export default function Disputes() {
  const { user } = useAuth();
  const [list, setList] = useState(null);
  const [error, setError] = useState("");
  const load = useCallback(() => {
    get("/disputes/mine").then(setList).catch((e) => setError(e.message));
  }, []);
  useEffect(load, [load]);

  return (
    <>
      <h2>Sporovi</h2>
      <Notice error={error} />
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Nemate sporova.</p> : list.map((d) => (
        <DisputeCard
          key={d.id}
          d={d}
          reload={load}
          // Slike može da dodaje samo onaj ko je otvorio spor, dok spor nije rešen.
          canAddPhotos={d.status !== "Resolved" && !!user && d.openedByUserId?.toLowerCase() === user.id?.toLowerCase()}
        />
      ))}
    </>
  );
}
