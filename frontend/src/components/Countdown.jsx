import { useEffect, useState } from "react";
import { parseUtc } from "../format.js";

export default function Countdown({ endTime }) {
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  const end = parseUtc(endTime);
  if (!end) return null;
  let diff = Math.floor((end.getTime() - now) / 1000);
  if (diff <= 0) return <span>Završeno</span>;

  const d = Math.floor(diff / 86400);
  diff %= 86400;
  const h = Math.floor(diff / 3600);
  diff %= 3600;
  const m = Math.floor(diff / 60);
  const s = diff % 60;
  const pad = (n) => String(n).padStart(2, "0");

  return (
    <span className={d === 0 && h === 0 && m < 5 ? "countdown urgent" : "countdown"}>
      {d > 0 ? `${d}d ` : ""}
      {pad(h)}:{pad(m)}:{pad(s)}
    </span>
  );
}
