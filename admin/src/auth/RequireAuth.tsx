import { Navigate, Outlet, useLocation } from "react-router";
import { useAuth } from "./AuthProvider";

export function RequireAuth() {
  const { account, loading } = useAuth();
  const location = useLocation();

  if (loading) return <p className="status">Đang tải...</p>;
  if (!account) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <Outlet />;
}
