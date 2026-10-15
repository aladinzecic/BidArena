import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { del, get, post } from "../api.js";
import { useAuth } from "../auth.jsx";
import Countdown from "../components/Countdown.jsx";
import Notice from "../components/Notice.jsx";
import Stars from "../components/Stars.jsx";
import useAction from "../components/useAction.js";
import { STATUS_LABELS, categoryLabel, conditionLabel, dateTime, money } from "../format.js";

export default function AuctionDetails() {
  const { id } = useParams();
  const { user, hasRole, profile } = useAuth();
  const [auction, setAuction] = useState(null);
  const [bids, setBids] = useState([]);
  const [loadError, setLoadError] = useState("");
  const [photo, setPhoto] = useState(0);
  const [amount, setAmount] = useState("");
  const [proxyMax, setProxyMax] = useState("");
  const [watching, setWatching] = useState(false);
  const [seller, setSeller] = useState(null);
  const [questions, setQuestions] = useState([]);
  const [questionText, setQuestionText] = useState("");
  const [answerDrafts, setAnswerDrafts] = useState({});
  const bidAction = useAction();
  const watchAction = useAction();
  const sellerAction = useAction();
  const questionAction = useAction();
  const answerAction = useAction();
  const amountTouched = useRef(false);

  const load = useCallback(async () => {
    try {
      const [a, b] = await Promise.all([get(`/auctions/${id}`), get(`/auctions/${id}/bids`)]);
      setAuction(a);
      setBids(b);
      setLoadError("");
      if (!amountTouched.current) setAmount(String(a.minimumNextBid));
    } catch (e) {
      setLoadError(e.message);
    }
  }, [id]);

  useEffect(() => {
    amountTouched.current = false;
    load();
  }, [load]);

  // Pitanja i odgovori (javno vidljivi).
  const loadQuestions = useCallback(() => {
    get(`/auctions/${id}/questions`).then(setQuestions).catch(() => {});
  }, [id]);

  useEffect(() => {
    loadQuestions();
  }, [loadQuestions]);

  // Periodično osvežavanje (polling) dok je aukcija zakazana ili aktivna: nove ponude, produženje,
  // pad cene holandske aukcije, kraj/otkazivanje i nova pitanja. Pauzira se dok je tab skriven.
  useEffect(() => {
    const status = auction?.status;
    if (status !== "Active" && status !== "Scheduled") return undefined;
    const delay = status === "Active" ? 4000 : 15000;
    const timer = setInterval(() => {
      if (document.hidden) return;
      load();
      loadQuestions();
    }, delay);
    return () => clearInterval(timer);
  }, [auction?.status, load, loadQuestions]);

  // Reputacija prodavca (javna): ime, prosečna ocena, broj uspešnih i otkazanih aukcija.
  const sellerId = auction?.sellerId;
  useEffect(() => {
    if (!sellerId) return;
    get(`/users/${sellerId}/rating`).then(setSeller).catch(() => setSeller(null));
  }, [sellerId]);

  // Provera da li je aukcija na listi praćenja.
  useEffect(() => {
    if (!user) return;
    get("/watchlist/mine")
      .then((list) => setWatching(list.some((a) => a.id === id)))
      .catch(() => {});
  }, [user, id]);


  if (loadError) return <Notice error={loadError} />;
  if (!auction) return <p>Učitavanje...</p>;

  const isDutch = auction.type === "Dutch";
  const isOwner = user && auction.sellerId === user.id;
  const isLeader = user && auction.currentWinnerId === user.id;
  const canBid = user && hasRole("Bidder") && !isOwner && auction.status === "Active";
  const canAsk =
    user && !isOwner && (hasRole("Bidder") || hasRole("Seller")) &&
    ["Scheduled", "Active"].includes(auction.status);

  async function placeBid(e) {
    e.preventDefault();
    const body = { amount: Number(amount), proxyMaxAmount: !isDutch && proxyMax ? Number(proxyMax) : null };
    const res = await bidAction.run(
      () => post(`/auctions/${id}/bids`, body),
      isDutch ? "Prihvatili ste cenu, aukcija se zatvara." : "Ponuda je prihvaćena."
    );
    if (res) {
      amountTouched.current = false;
      load();
    }
  }

  async function toggleWatch() {
    await watchAction.run(async () => {
      if (watching) await del(`/watchlist/${id}`);
      else await post(`/watchlist/${id}`);
      setWatching(!watching);
    });
  }

  async function askQuestion(e) {
    e.preventDefault();
    const res = await questionAction.run(
      () => post(`/auctions/${id}/questions`, { text: questionText }),
      "Pitanje je poslato prodavcu."
    );
    if (res) {
      setQuestionText("");
      loadQuestions();
    }
  }

  // Odgovara samo prodavac aukcije (server to dodatno proverava).
  async function answerQuestion(e, questionId) {
    e.preventDefault();
    const res = await answerAction.run(
      () => post(`/auctions/${id}/questions/${questionId}/answer`, { text: answerDrafts[questionId] ?? "" }),
      "Odgovor je objavljen."
    );
    if (res !== undefined) {
      setAnswerDrafts((d) => ({ ...d, [questionId]: "" }));
      loadQuestions();
    }
  }

  const sellerDo = (path, msg) => async () => {
    await sellerAction.run(() => post(`/auctions/${id}/${path}`), msg);
    load();
  };

  return (
    <div className="details">
      <div className="details-media">
        {auction.photos.length > 0 ? (
          <>
            <img className="main-photo" src={auction.photos[photo]?.url} alt={auction.title} />
            <div className="thumbs">
              {auction.photos.map((p, i) => (
                <img key={p.id} src={p.url} alt="" className={i === photo ? "sel" : ""} onClick={() => setPhoto(i)} />
              ))}
            </div>
          </>
        ) : <div className="no-photo big">Nema slika</div>}
      </div>

      <div className="details-info">
        <h2>{auction.title}</h2>
        <div className="muted small">
          {categoryLabel(auction.category)} · {conditionLabel(auction.condition)} · {auction.location}
        </div>
        {seller && (
          <div className="small">
            Prodavac: <Link to={`/users/${seller.id}`}>{seller.displayName}</Link>
            {seller.isVerified && <> <span className="badge Active">Verifikovan</span></>}
            {" "}<Stars value={seller.averageRating} count={seller.ratingsCount} />
            <span className="muted"> · uspešnih: {seller.successfulAuctionsCount}, otkazanih: {seller.cancelledAuctionsCount}</span>
          </div>
        )}
        <p>
          <span className={`badge ${auction.status}`}>{STATUS_LABELS[auction.status] ?? auction.status}</span>{" "}
          <span className="badge">{isDutch ? "Holandska aukcija" : "Engleska aukcija"}</span>
        </p>

        <div className="price-box">
          <div className="muted small">{isDutch ? "Trenutna cena (pada svakog minuta)" : "Trenutna cena"}</div>
          <div className="price big">{money(auction.currentPrice)} RSD</div>
          {auction.status === "Active" && <div>Ističe za: <Countdown endTime={auction.endTime} /></div>}
          <div className="small muted">Ističe: {dateTime(auction.endTime)} · {auction.bidCount} ponuda</div>
          {!isDutch && auction.hasReservePrice && (
            <div className="small">{auction.reserveMet ? "✔ Rezervna cena je dostignuta" : "Rezervna cena još nije dostignuta"}</div>
          )}
          {isLeader && auction.status === "Active" && <div className="success">Vi trenutno vodite.</div>}
          {!isDutch && <div className="small muted">Anti-snipe: ponuda u poslednjih {auction.antiSnipeWindowMinutes} min produžava aukciju za {auction.antiSnipeExtensionMinutes} min.</div>}
        </div>

        {canBid && (
          <form className="card form" onSubmit={placeBid}>
            {isDutch ? (
              <>
                <p>Prvi koji prihvati trenutnu cenu pobeđuje.</p>
                <button disabled={bidAction.busy}>Prihvati cenu {money(auction.minimumNextBid)} RSD</button>
              </>
            ) : (
              <>
                <label>
                  Vaša ponuda (min. {money(auction.minimumNextBid)})
                  <input type="number" step="0.01" min={auction.minimumNextBid} value={amount}
                    onChange={(e) => { amountTouched.current = true; setAmount(e.target.value); }} required />
                </label>
                <label>
                  Maksimum za automatsko nadmetanje (opciono)
                  <input type="number" step="0.01" value={proxyMax} onChange={(e) => setProxyMax(e.target.value)} />
                </label>
                <button disabled={bidAction.busy}>Licitiraj</button>
              </>
            )}
            <Notice error={bidAction.error} ok={bidAction.ok} />
            {profile && (!profile.isVerified || !profile.hasPaymentMethod) && (
              <p className="warn small">
                {!profile.isVerified && "Nalog još nije verifikovan. "}
                {!profile.hasPaymentMethod && "Dodajte način plaćanja na stranici Profil."}
              </p>
            )}
          </form>
        )}
        {!user && auction.status === "Active" && <p className="muted">Prijavite se da biste se nadmetali.</p>}

        {user && (
          <p>
            <button className="secondary" onClick={toggleWatch} disabled={watchAction.busy}>
              {watching ? "★ Prestani da pratiš" : "☆ Prati aukciju"}
            </button>
            <Notice error={watchAction.error} />
          </p>
        )}

        {isOwner && (
          <div className="card">
            <strong>Upravljanje aukcijom</strong>
            <div className="row">
              {auction.status === "Draft" && <button onClick={sellerDo("activate", "Aukcija je aktivirana.")}>Aktiviraj</button>}
              {["Draft", "Scheduled", "Active"].includes(auction.status) && (
                <button className="danger" onClick={sellerDo("cancel", "Aukcija je otkazana.")}>Otkaži</button>
              )}
            </div>
            <Notice error={sellerAction.error} ok={sellerAction.ok} />
          </div>
        )}

        <h3>Opis</h3>
        <pre className="description">{auction.descriptionMarkdown}</pre>

        <h3>Istorija ponuda</h3>
        {bids.length === 0 ? <p className="muted">Još nema ponuda.</p> : (
          <table>
            <thead><tr><th>Iznos</th><th>Vreme</th><th></th></tr></thead>
            <tbody>
              {bids.map((b) => (
                <tr key={b.id}>
                  <td>{money(b.amount)} RSD</td>
                  <td>{dateTime(b.createdAt)}</td>
                  <td className="small muted">{b.isProxy ? "automatska" : ""}{user && b.bidderId === user.id ? " (vi)" : ""}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <h3>Pitanja i odgovori</h3>
        {questions.length === 0 ? <p className="muted">Još nema pitanja.</p> : questions.map((q) => (
          <div key={q.id} className="card qa-item">
            <div className="small muted">
              {q.askedByName}{user && q.askedByUserId === user.id ? " (vi)" : ""} · {dateTime(q.askedAt)}
            </div>
            <p className="qa-question">{q.questionText}</p>
            {q.answerText ? (
              <div className="qa-answer">
                <div className="small muted">Odgovor prodavca · {dateTime(q.answeredAt)}</div>
                <p>{q.answerText}</p>
              </div>
            ) : isOwner ? (
              <form className="form" onSubmit={(e) => answerQuestion(e, q.id)}>
                <textarea rows={2} maxLength={2000} placeholder="Vaš odgovor" required
                  value={answerDrafts[q.id] ?? ""}
                  onChange={(e) => setAnswerDrafts((d) => ({ ...d, [q.id]: e.target.value }))} />
                <div><button disabled={answerAction.busy}>Odgovori</button></div>
              </form>
            ) : <p className="muted small">Čeka se odgovor prodavca.</p>}
          </div>
        ))}
        {isOwner && <Notice error={answerAction.error} ok={answerAction.ok} />}

        {canAsk && (
          <form className="card form" onSubmit={askQuestion}>
            <label>
              Postavite pitanje prodavcu
              <textarea rows={3} minLength={5} maxLength={1000} required value={questionText}
                onChange={(e) => setQuestionText(e.target.value)} />
            </label>
            <div><button disabled={questionAction.busy}>Pošalji pitanje</button></div>
            <Notice error={questionAction.error} ok={questionAction.ok} />
          </form>
        )}
        {!user && <p className="muted small">Prijavite se da biste postavili pitanje.</p>}
      </div>
    </div>
  );
}
