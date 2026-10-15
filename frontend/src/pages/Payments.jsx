import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { get, post } from "../api.js";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";
import { STATUS_LABELS, dateTime, money } from "../format.js";

export default function Payments() {
  const [list, setList] = useState(null);
  const { run, error, ok, busy } = useAction();
  const [loadError, setLoadError] = useState("");

  const load = useCallback(() => {
    get("/payments/mine").then(setList).catch((e) => setLoadError(e.message));
  }, []);
  useEffect(load, [load]);

  async function pay(id) {
    await run(() => post(`/payments/${id}/pay`), "Plaćanje je izvršeno.");
    load();
  }

  return (
    <>
      <h2>Moja plaćanja</h2>
      <Notice error={loadError || error} ok={ok} />
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Nemate obaveza za plaćanje.</p> : (
        <table>
          <thead><tr><th>Aukcija</th><th>Iznos</th><th>Provizija</th><th>Status</th><th>Rok</th><th></th></tr></thead>
          <tbody>
            {list.map((p) => (
              <tr key={p.id}>
                <td><Link to={`/auctions/${p.auctionId}`}>Otvori</Link></td>
                <td>{money(p.amount)} RSD</td>
                <td>{money(p.commission)} RSD</td>
                <td><span className={`badge ${p.status}`}>{STATUS_LABELS[p.status] ?? p.status}</span></td>
                <td>{dateTime(p.dueDate)}</td>
                <td>{p.status === "Pending" && <button disabled={busy} onClick={() => pay(p.id)}>Plati</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );
}
