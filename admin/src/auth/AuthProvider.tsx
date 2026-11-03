import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, onUnauthorized, type AdminAccount } from "../api/client";

type LoginResult = { ok: true } | { ok: false; message: string };

type AuthState = {
  account: AdminAccount | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<LoginResult>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

const loginErrors: Record<number, string> = {
  400: "Kiểm lại email và mật khẩu.",
  401: "Sai email hoặc mật khẩu. Sai 5 lần liên tiếp thì phải chờ 15 phút.",
  429: "Thử quá nhiều lần. Chờ một phút rồi thử lại.",
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const [account, setAccount] = useState<AdminAccount | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => onUnauthorized(() => setAccount(null)), []);

  useEffect(() => {
    let cancelled = false;
    api
      .GET("/api/admin/auth/me")
      .then(({ data }) => {
        if (!cancelled) setAccount(data ?? null);
      })
      .catch(() => {
        if (!cancelled) setAccount(null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (email: string, password: string): Promise<LoginResult> => {
    try {
      const { data, response } = await api.POST("/api/admin/auth/login", { body: { email, password } });
      if (data) {
        setAccount(data);
        return { ok: true };
      }
      return { ok: false, message: loginErrors[response.status] ?? "Máy chủ đang lỗi. Thử lại sau." };
    } catch {
      return { ok: false, message: "Không kết nối được máy chủ." };
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.POST("/api/admin/auth/logout");
    } finally {
      setAccount(null);
    }
  }, []);

  const value = useMemo(() => ({ account, loading, login, logout }), [account, loading, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const state = useContext(AuthContext);
  if (!state) throw new Error("useAuth must be used inside AuthProvider");
  return state;
}
