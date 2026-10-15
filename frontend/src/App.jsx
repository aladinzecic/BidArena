import { useEffect, useState } from "react";
import { Link, NavLink, Route, Routes, useNavigate } from "react-router-dom";
import { useAuth } from "./auth.jsx";
import { get } from "./api.js";
import Protected from "./components/Protected.jsx";
import Login from "./pages/Login.jsx";
import Register from "./pages/Register.jsx";
import Auctions from "./pages/Auctions.jsx";
import AuctionDetails from "./pages/AuctionDetails.jsx";
import MyItems from "./pages/MyItems.jsx";
import MyAuctions from "./pages/MyAuctions.jsx";
import Watchlist from "./pages/Watchlist.jsx";
import Payments from "./pages/Payments.jsx";
import Shipments from "./pages/Shipments.jsx";
import Disputes from "./pages/Disputes.jsx";
import Notifications from "./pages/Notifications.jsx";
import Profile from "./pages/Profile.jsx";
import UserRatings from "./pages/UserRatings.jsx";
import Admin from "./pages/Admin.jsx";

function Toasts({ toasts }) {
  return (
    <div className="toasts">
      {toasts.map((t) => (
        <div key={t.id} className="toast">
          <strong>{t.type}</strong>
          <div>{t.message}</div>
        </div>
      ))}
    </div>
  );
}

function SidebarLink({ to, end, children }) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) => "navitem" + (isActive ? " active" : "")}
    >
      {children}
    </NavLink>
  );
}

export default function App() {
  const { user, hasRole, logout } = useAuth();
  const navigate = useNavigate();
  const [unread, setUnread] = useState(0);
  const [toasts, setToasts] = useState([]);

  // Broj nepročitanih obaveštenja; server se proverava periodično (polling), a nova
  // nepročitana obaveštenja se prikazuju kao toast.
  useEffect(() => {
    if (!user) {
      setUnread(0);
      return undefined;
    }
    let cancelled = false;
    let seen = null; // ID-jevi nepročitanih obaveštenja koja su već viđena (null = prvo učitavanje)

    const showToast = (n) => {
      const id = Math.random();
      setToasts((t) => [...t, { id, type: n.type, message: n.message }]);
      setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 6000);
    };

    const refresh = async () => {
      try {
        const list = await get("/notifications/mine");
        if (cancelled) return;
        const unreadList = list.filter((n) => !n.isRead);
        setUnread(unreadList.length);
        if (seen) unreadList.filter((n) => !seen.has(n.id)).forEach(showToast);
        seen = new Set(unreadList.map((n) => n.id));
      } catch {
        /* sledeći pokušaj kroz interval */
      }
    };

    refresh();
    const timer = setInterval(() => {
      if (!document.hidden) refresh();
    }, 15000);
    window.addEventListener("notifications-changed", refresh);

    return () => {
      cancelled = true;
      clearInterval(timer);
      window.removeEventListener("notifications-changed", refresh);
    };
  }, [user]);

  const primaryRole = hasRole("Admin") ? "Admin" : hasRole("Seller") ? "Seller" : "Bidder";

  return (
    <>
      <header className="nav">
        <Link to="/" className="brand">
          BidArena
        </Link>
        <div className="nav-right">
          {user ? (
            <>
              <NavLink to="/notifications">
                Obaveštenja{unread > 0 && <span className="pill">{unread}</span>}
              </NavLink>
              <span className="nav-user">
                <strong>{user.name}</strong>
                <span className="nav-role">{primaryRole}</span>
              </span>
              <button
                className="secondary"
                onClick={async () => {
                  await logout();
                  navigate("/login");
                }}
              >
                Odjava
              </button>
            </>
          ) : (
            <>
              <NavLink to="/login">Prijava</NavLink>
              <NavLink to="/register">Registracija</NavLink>
            </>
          )}
        </div>
      </header>

      <div className="layout">
        <aside className="sidebar">
          <h2>DISCOVER</h2>
          <SidebarLink to="/" end>Aukcije</SidebarLink>

          {user && (
            <>
              <h2>MOJE</h2>
              <SidebarLink to="/watchlist">Praćenje</SidebarLink>
              <SidebarLink to="/payments">Plaćanja</SidebarLink>
              <SidebarLink to="/shipments">Pošiljke</SidebarLink>
              <SidebarLink to="/disputes">Sporovi</SidebarLink>
              <SidebarLink to="/profile">Profil</SidebarLink>
            </>
          )}

          {hasRole("Seller") && (
            <>
              <h2>PRODAJA</h2>
              <SidebarLink to="/my/items">Moji predmeti</SidebarLink>
              <SidebarLink to="/my/auctions">Moje aukcije</SidebarLink>
            </>
          )}

          {hasRole("Admin") && (
            <>
              <h2>UPRAVLJANJE</h2>
              <SidebarLink to="/admin">Admin</SidebarLink>
            </>
          )}
        </aside>

        <main className="container">
          <Routes>
            <Route path="/" element={<Auctions />} />
            <Route path="/auctions/:id" element={<AuctionDetails />} />
            <Route path="/users/:id" element={<UserRatings />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/my/items" element={<Protected role="Seller"><MyItems /></Protected>} />
            <Route path="/my/auctions" element={<Protected role="Seller"><MyAuctions /></Protected>} />
            <Route path="/watchlist" element={<Protected><Watchlist /></Protected>} />
            <Route path="/payments" element={<Protected><Payments /></Protected>} />
            <Route path="/shipments" element={<Protected><Shipments /></Protected>} />
            <Route path="/disputes" element={<Protected><Disputes /></Protected>} />
            <Route path="/notifications" element={<Protected><Notifications /></Protected>} />
            <Route path="/profile" element={<Protected><Profile /></Protected>} />
            <Route path="/admin" element={<Protected role="Admin"><Admin /></Protected>} />
            <Route path="*" element={<p>Stranica ne postoji.</p>} />
          </Routes>
        </main>
      </div>
      <Toasts toasts={toasts} />
    </>
  );
}
