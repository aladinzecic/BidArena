import { useState } from "react";
import { PHOTO_TYPES, checkPhotoFiles } from "./disputePhotos.js";

// Izbor slika za spor. `files` / `setFiles` drži roditelj; `attached` je broj već priloženih slika.
export default function DisputePhotoPicker({ files, setFiles, attached = 0, disabled = false }) {
  const [error, setError] = useState("");

  function onChange(e) {
    const picked = [...e.target.files];
    e.target.value = "";
    if (picked.length === 0) return;
    const next = [...files, ...picked];
    const problem = checkPhotoFiles(next, attached);
    setError(problem);
    if (!problem) setFiles(next);
  }

  return (
    <div className="picker">
      <label className="small">
        Slike (opciono, JPEG/PNG/WebP do 5 MB)
        <input type="file" multiple accept={PHOTO_TYPES.join(",")} onChange={onChange} disabled={disabled} />
      </label>
      {error && <p className="error small">{error}</p>}
      {files.length > 0 && (
        <ul className="picker-list">
          {files.map((f, i) => (
            <li key={`${f.name}-${i}`}>
              <span>{f.name}</span>
              <button type="button" className="secondary" disabled={disabled} onClick={() => setFiles(files.filter((_, j) => j !== i))}>Ukloni</button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
