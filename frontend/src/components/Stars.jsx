// Prikaz ocene: ★★★★☆ 4.25 (12). Bez ocena prikazuje "još nema ocena".
export function StarRow({ value }) {
  const full = Math.round(value);
  return <span className="stars" aria-label={`${value} od 5`}>{"★".repeat(full)}{"☆".repeat(5 - full)}</span>;
}

export default function Stars({ value, count }) {
  if (!count) return <span className="muted small">još nema ocena</span>;
  return (
    <span>
      <StarRow value={value} /> <strong>{value.toFixed(2)}</strong> <span className="muted small">({count})</span>
    </span>
  );
}
