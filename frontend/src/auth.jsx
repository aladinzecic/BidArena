import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { api, decodeJwt, get, getTokens, setSessionExpiredHandler, setTokens } from "./api.js";

const AuthContext = createContext(null);
export const useAuth = () => useContext(AuthContext);

function userFromToken() {
  const tokens = getTokens();
  const claims = tokens && decodeJwt(tokens.accessToken);
  if (!claims) return null;
  const roles = [].concat(claims.role || []);
  return { id: claims.sub, name: claims.name, email: claims.email, roles };
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(userFromToken);
  const [profile, setProfile] = useState(null);

  const loadProfile = useCallback(async () => {
    try {
      setProfile(await get("/users/me"));
    } catch {
      setProfile(null);
    }
  }, []);

  useEffect(() => {
    setSessionExpiredHandler(() => {
      setUser(null);
      setProfile(null);
    });
  }, []);

  useEffect(() => {
    if (user) loadProfile();
    else setProfile(null);
  }, [user, loadProfile]);

  const login = useCallback(async (email, password) => {
    const res = await api("/auth/login", { method: "POST", body: { email, password }, auth: false });
    setTokens(res);
    setUser(userFromToken());
  }, []);

  const register = useCallback(async (data) => {
    await api("/auth/register", { method: "POST", body: data, auth: false });
  }, []);

  const logout = useCallback(async () => {
    const tokens = getTokens();
    try {
      if (tokens?.refreshToken) {
        await api("/auth/logout", { method: "POST", body: { refreshToken: tokens.refreshToken }, auth: false });
      }
    } catch {
      /* odjava na klijentu uvek uspeva */
    }
    setTokens(null);
    setUser(null);
    setProfile(null);
  }, []);

  const value = useMemo(
    () => ({
      user,
      profile,
      reloadProfile: loadProfile,
      login,
      register,
      logout,
      hasRole: (r) => !!user?.roles.includes(r),
    }),
    [user, profile, loadProfile, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
