import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { get } from "../api.js";
import Notice from "../components/Notice.jsx";
import Stars, { StarRow } from "../components/Stars.jsx";
import { dateTime } from "../format.js";

// Javna stranica reputacije korisnika (dostupna i neprijavljenima).
export default function UserRatings() {
  const { id } = useParams();
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    setData(null);
    get(`/users/${id}/rating`).then(setData).catch((e) => setError(e.message));
  }, [id]);

  if (error) return <Notice error={error} />;
  if (!data) return <p>Učitavanje...</p>;

  return (
    <>
      <h2>
        {data.displayName} {data.isVerified && <span className="badge Active">Verifikovan</span>}
      </h2>
      <div className="card">
        <p><Stars value={data.averageRating} count={data.ratingsCount} /></p>
        <p className="small muted">
          Uspešnih aukcija: {data.successfulAuctionsCount} · Otkazanih: {data.cancelledAuctionsCount}
        </p>
      </div>

      <h3>Poslednje ocene</h3>
      {data.recentRatings.length === 0 ? (
        <p className="muted">Još nema ocena.</p>
      ) : (
        data.recentRatings.map((r, i) => (
          <div className="card" key={i}>
            <div className="row between">
              <StarRow value={r.score} />
              <span className="small muted">{dateTime(r.createdAt)}</span>
            </div>
            {r.comment && <p>{r.comment}</p>}
          </div>
        ))
      )}
    </>
  );
}
