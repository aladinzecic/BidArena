import { useState } from "react";

// Pomoćnik za akcije sa porukom o grešci/uspehu: const { run, error, ok, busy } = useAction();
export default function useAction() {
  const [error, setError] = useState("");
  const [ok, setOk] = useState("");
  const [busy, setBusy] = useState(false);

  async function run(fn, successMessage = "") {
    setError("");
    setOk("");
    setBusy(true);
    try {
      const result = await fn();
      if (successMessage) setOk(successMessage);
      return result;
    } catch (e) {
      setError(e.message || "Došlo je do greške");
      return undefined;
    } finally {
      setBusy(false);
    }
  }

  return { run, error, ok, busy, setError, setOk };
}
