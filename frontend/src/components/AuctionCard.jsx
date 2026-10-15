import { Link } from "react-router-dom";
import Countdown from "./Countdown.jsx";
import { STATUS_LABELS, categoryLabel, conditionLabel, money } from "../format.js";

export default function AuctionCard({ auction, children }) {
  return (
    <div className="card auction-card">
      <Link to={`/auctions/${auction.id}`} className="thumb">
        {auction.mainPhotoUrl ? <img src={auction.mainPhotoUrl} alt={auction.title} /> : <div className="no-photo">Nema slike</div>}
      </Link>
      <div className="card-body">
        <Link to={`/auctions/${auction.id}`}>
          <h3>{auction.title}</h3>
        </Link>
        <div className="muted small">
          {categoryLabel(auction.category)} · {conditionLabel(auction.condition)} · {auction.location}
        </div>
        <div className="price">{money(auction.currentPrice)} RSD</div>
        <div className="small">
          {auction.type === "Dutch" ? "Holandska" : "Engleska"} · {auction.bidCount} ponuda ·{" "}
          <span className={`badge ${auction.status}`}>{STATUS_LABELS[auction.status] ?? auction.status}</span>
        </div>
        {auction.status === "Active" && (
          <div className="small">
            Ističe za: <Countdown endTime={auction.endTime} />
          </div>
        )}
        {children}
      </div>
    </div>
  );
}
