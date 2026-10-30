import { Navigate, Route, Routes } from "react-router";
import { AuthProvider } from "./auth/AuthProvider";
import { RequireAuth } from "./auth/RequireAuth";
import { AdminLayout } from "./layout/AdminLayout";
import { LoginPage } from "./pages/LoginPage";
import { PlaceholderPage } from "./pages/PlaceholderPage";

export function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<AdminLayout />}>
            <Route index element={<PlaceholderPage title="Dashboard" task="#28" />} />
            <Route path="puzzles" element={<PlaceholderPage title="Câu đố" task="#29" />} />
            <Route path="puzzles/:puzzleId" element={<PlaceholderPage title="Câu đố" task="#29" />} />
            <Route path="accounts" element={<PlaceholderPage title="Tài khoản quản trị" task="#27" />} />
            <Route path="profile" element={<PlaceholderPage title="Đổi mật khẩu" task="#27" />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  );
}
