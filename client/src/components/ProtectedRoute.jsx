import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import Spinner from './Spinner';

export default function ProtectedRoute({ children }) {
  const { user, loading } = useAuth();
  const location = useLocation();

  // The loading gate is what makes a hard refresh work. Without it, every F5
  // momentarily has user === null while /auth/me is still in flight, the guard
  // fires, and an authenticated user gets bounced to /login — which presents as
  // "my session doesn't persist".
  if (loading) return <Spinner block label="Restoring your session" />;

  if (!user) return <Navigate to="/login" replace state={{ from: location }} />;

  return children;
}

export function PublicOnlyRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return <Spinner block label="Restoring your session" />;
  if (user) return <Navigate to="/" replace />;

  return children;
}
