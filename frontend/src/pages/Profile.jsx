import { post } from "../api.js";
import { useAuth } from "../auth.jsx";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";

export default function Profile() {
  const { profile, reloadProfile } = useAuth();
  const { run, error, ok, busy } = useAction();

  if (!profile) return <p>Učitavanje...</p>;

  async function addPayment() {
    await run(() => post("/users/me/payment-method"), "Način plaćanja je dodat.");
    reloadProfile();
  }

  const roles = [profile.isBidder && "Kupac", profile.isSeller && "Prodavac", profile.isAdmin && "Admin"].filter(Boolean).join(", ");

  return (
    <div className="card narrow">
      <h2>{profile.displayName}</h2>
      <p>{profile.email} · {roles}</p>
      <p>
        Verifikacija: {profile.isVerified ? <span className="success">✔ verifikovan</span> : <span className="warn">čeka verifikaciju admina</span>}
      </p>
      <p>
        Način plaćanja: {profile.hasPaymentMethod ? <span className="success">✔ dodat</span> : (
          <>
            <span className="warn">nije dodat</span>{" "}
            <button onClick={addPayment} disabled={busy}>Dodaj (simulacija)</button>
          </>
        )}
      </p>
      <p>
        Ocena: {profile.ratingsCount > 0 ? `${profile.averageRating.toFixed(2)} ★ (${profile.ratingsCount})` : "još nema ocena"}
      </p>
      <p className="small muted">
        Uspešnih aukcija: {profile.successfulAuctionsCount} · Otkazanih: {profile.cancelledAuctionsCount}
      </p>
      <Notice error={error} ok={ok} />
    </div>
  );
}
