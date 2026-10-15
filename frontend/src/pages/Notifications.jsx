import { useCallback, useEffect, useState } from "react";
import { get, post } from "../api.js";
import Notice from "../components/Notice.jsx";
import { dateTime } from "../format.js";

export default function Notifications() {
  const [list, setList] = useState(null);
  const [error, setError] = useState("");
  const load = useCallback(() => {
    get("/notifications/mine").then(setList).catch((e) => setError(e.message));
  }, []);
  useEffect(load, [load]);

  async function markRead(id) {
    try {
      await post(`/notifications/${id}/read`);
      load();
      window.dispatchEvent(new Event("notifications-changed"));
    } catch (e) {
      setError(e.message);
    }
  }

  return (
    <>
      <h2>Obaveštenja</h2>
      <Notice error={error} />
      {!list ? <p>Učitavanje...</p> : list.length === 0 ? <p className="muted">Nemate obaveštenja.</p> : list.map((n) => (
        <div key={n.id} className={n.isRead ? "card" : "card unread"}>
          <div className="row between">
            <span className="badge">{n.type}</span>
            <span className="muted small">{dateTime(n.createdAt)}</span>
          </div>
          <p>{n.message}</p>
          {!n.isRead && <button className="secondary" onClick={() => markRead(n.id)}>Označi kao pročitano</button>}
        </div>
      ))}
    </>
  );
}
