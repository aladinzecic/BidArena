import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { get, post } from "../api.js";
import DisputePhotos from "../components/DisputePhotos.jsx";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";
import { STATUS_LABELS, dateTime, money } from "../format.js";

function Stats() {
  const [s, setS] = useState(null);
  useEffect(() => {
    get("/admin/stats").then(setS).catch(() => {});
  }, []);
  if (!s) return null;
  return (
    <div className="stats">
      <div className="card"><div className="muted small">Korisnici</div><strong>{s.totalUsers}</strong> <span className="small">({s.verifiedUsers} verifikovanih)</span></div>
      <div className="card"><div className="muted small">Promet</div><strong>{money(s.totalTurnover)} RSD</strong></div>
      <div className="card"><div className="muted small">Provizija</div><strong>{money(s.totalCommission)} RSD</strong></div>
      <div className="card"><div className="muted small">Plaćanja (na čekanju / isteklo)</div><strong>{s.pendingPayments} / {s.overduePayments}</strong></div>
      <div className="card"><div className="muted small">Otvoreni sporovi</div><strong>{s.openDisputes}</strong></div>
      <div className="card">
        <div className="muted small">Aukcije po statusu</div>
        {Object.entries(s.auctionsByStatus).map(([k, v]) => <div key={k} className="small">{STATUS_LABELS[k] ?? k}: {v}</div>)}
      </div>
    </div>
  );
}

function Users() {
  const [users, setUsers] = useState(null);
  const { run, error, ok, busy } = useAction();
  const load = useCallback(() => {
    get("/admin/users/unverified").then(setUsers).catch(() => setUsers([]));
  }, []);
  useEffect(load, [load]);

  async function verify(id) {
    await run(() => post(`/admin/users/${id}/verify`), "Korisnik je verifikovan.");
    load();
  }

  return (
    <>
      <h3>Neverifikovani korisnici</h3>
      <Notice error={error} ok={ok} />
      {!users ? <p>Učitavanje...</p> : users.length === 0 ? <p className="muted">Svi korisnici su verifikovani.</p> : (
        <table>
          <thead><tr><th>Ime</th><th>Email</th><th>Uloge</th><th></th></tr></thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>{u.displayName}</td>
                <td>{u.email}</td>
                <td>{[u.isBidder && "Kupac", u.isSeller && "Prodavac"].filter(Boolean).join(", ")}</td>
                <td><button disabled={busy} onClick={() => verify(u.id)}>Verifikuj</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );
}

function DisputeRow({ d, reload }) {
  const { run, error, ok, busy } = useAction();
  const [resolution, setResolution] = useState("");
  const [outcome, setOutcome] = useState("FavorSeller");

  const take = async () => {
    await run(() => post(`/admin/disputes/${d.id}/take`), "Spor je preuzet.");
    reload();
  };
  const resolve = async () => {
    await run(() => post(`/admin/disputes/${d.id}/resolve`, { resolution, outcome }), "Spor je rešen.");
    reload();
  };

  return (
    <div className="card">
      <div className="row between">
        <strong>{dateTime(d.createdAt)}</strong>
        <span className={`badge ${d.status}`}>{STATUS_LABELS[d.status] ?? d.status}</span>
      </div>
      <p>{d.reason}</p>
      <DisputePhotos photos={d.photos} />
      <Link to={`/auctions/${d.auctionId}`}>Aukcija</Link>
      {d.resolution && <p><strong>Odluka:</strong> {d.resolution}</p>}
      {d.status === "Open" && <p><button disabled={busy} onClick={take}>Preuzmi spor</button></p>}
      {d.status === "UnderReview" && (
        <div className="form inner">
          <textarea rows={3} placeholder="Obrazloženje odluke (min. 10 karaktera)" value={resolution} onChange={(e) => setResolution(e.target.value)} />
          <label>Ishod
            <select value={outcome} onChange={(e) => setOutcome(e.target.value)}>
              <option value="FavorSeller">U korist prodavca (aukcija se zaključuje)</option>
              <option value="RefundBuyer">Povraćaj novca kupcu (aukcija se otkazuje)</option>
            </select>
          </label>
          <button disabled={busy || resolution.trim().length < 10} onClick={resolve}>Reši spor</button>
        </div>
      )}
      <Notice error={error} ok={ok} />
    </div>
  );
}

function DisputesAdmin() {
  const [status, setStatus] = useState("");
  const [list, setList] = useState(null);
  const load = useCallback(() => {
    get(`/admin/disputes${status ? `?status=${status}` : ""}`).then(setList).catch(() => setList([]));
  }, [status]);
  useEffect(load, [load]);

  return (
    <>
      <h3>Sporovi</h3>
      <select value={status} onChange={(e) => setStatus(e.target.value)}>
        <option value="">Svi</option>
        <option value="Open">Otvoreni</option>
        <option value="UnderReview">U razmatranju</option>
        <option value="Resolved">Rešeni</option>
      </select>
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Nema sporova.</p> : list.map((d) => <DisputeRow key={d.id} d={d} reload={load} />)}
    </>
  );
}

export default function Admin() {
  return (
    <>
      <h2>Admin panel</h2>
      <Stats />
      <Users />
      <DisputesAdmin />
    </>
  );
}
