import { NavLink, Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import ThemeToggle from './ThemeToggle';

export default function NavBar() {
  const { user, logout } = useAuth();

  return (
    <header className="appbar">
      <div className="appbar-inner">
        <Link to="/" className="brand">
          <span className="brand-dot" aria-hidden="true" />
          FitBit
        </Link>

        <nav className="nav" aria-label="Main">
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/activities">Activities</NavLink>
          <NavLink to="/goals">Goals</NavLink>
        </nav>

        <div className="spacer" />

        <ThemeToggle />
        {user?.displayName && (
          <span className="user-chip">
            <span className="avatar" aria-hidden="true">{user.displayName.charAt(0).toUpperCase()}</span>
            {user.displayName}
          </span>
        )}
        <button type="button" className="btn btn-sm btn-ghost" onClick={logout}>
          Sign out
        </button>
      </div>
    </header>
  );
}
