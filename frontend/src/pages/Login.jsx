import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth.jsx";
import Notice from "../components/Notice.jsx";
import useAction from "../components/useAction.js";

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { run, error, busy } = useAction();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  async function submit(e) {
    e.preventDefault();
    const ok = await run(async () => {
      await login(email, password);
      return true;
    });
    if (ok) navigate(location.state?.from || "/");
  }

  return (
    <form className="card form narrow" onSubmit={submit}>
      <h2>Prijava</h2>
      <label>Email<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
      <label>Lozinka<input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></label>
      <Notice error={error} />
      <button disabled={busy}>Prijavi se</button>
      <p className="small">Nemate nalog? <Link to="/register">Registrujte se</Link></p>
    </form>
  );
}
