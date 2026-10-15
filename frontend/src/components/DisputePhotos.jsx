// Prikaz slika priloženih uz spor (klik otvara punu sliku u novom tabu).
export default function DisputePhotos({ photos }) {
  if (!photos || photos.length === 0) return null;
  return (
    <div className="thumbs">
      {photos.map((p) => (
        <a key={p.id} href={p.url} target="_blank" rel="noreferrer">
          <img src={p.url} alt="Slika uz spor" />
        </a>
      ))}
    </div>
  );
}
