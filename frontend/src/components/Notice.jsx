export default function Notice({ error, ok }) {
  if (error) return <p className="error">{error}</p>;
  if (ok) return <p className="success">{ok}</p>;
  return null;
}
