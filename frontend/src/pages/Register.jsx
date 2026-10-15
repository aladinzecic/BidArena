import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth.jsx";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";

export default function Register() {
  const { register, login } = useAuth();
  const navigate = useNavigate();
  const { run, error, busy } = useAction();
  const [f, setF] = useState({ email: "", password: "", displayName: "", isSeller: false, isBidder: true });
  const set = (k) => (e) => setF({ ...f, [k]: e.target.type === "checkbox" ? e.target.checked : e.target.value });

  async function submit(e) {
    e.preventDefault();
    if (!f.isSeller && !f.isBidder) return;
    const ok = await run(async () => {
      await register(f);
      await login(f.email, f.password);
      return true;
    });
    if (ok) navigate("/profile");
  }

  return (
    <form className="card form narrow" onSubmit={submit}>
      <h2>Registracija</h2>
      <label>Ime za prikaz<input value={f.displayName} onChange={set("displayName")} required /></label>
      <label>Email<input type="email" value={f.email} onChange={set("email")} required /></label>
      <label>Lozinka<input type="password" value={f.password} onChange={set("password")} required minLength={8} /></label>
      <label className="check"><input type="checkbox" checked={f.isBidder} onChange={set("isBidder")} /> Želim da se nadmećem (kupac)</label>
      <label className="check"><input type="checkbox" checked={f.isSeller} onChange={set("isSeller")} /> Želim da prodajem (prodavac)</label>
      {!f.isSeller && !f.isBidder && <p className="error">Izaberite bar jednu ulogu.</p>}
      <Notice error={error} />
      <button disabled={busy}>Napravi nalog</button>
      <p className="small">Već imate nalog? <Link to="/login">Prijavite se</Link></p>
      <p className="muted small">Nakon registracije admin mora da verifikuje nalog, a za nadmetanje je potrebno dodati način plaćanja.</p>
    </form>
  );
}
