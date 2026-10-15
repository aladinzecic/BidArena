import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../auth.jsx";

export default function Protected({ role, children }) {
  const { user, hasRole } = useAuth();
  const location = useLocation();
  if (!user) return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  if (role && !hasRole(role)) return <p className="error">Nemate pristup ovoj stranici.</p>;
  return children;
}
