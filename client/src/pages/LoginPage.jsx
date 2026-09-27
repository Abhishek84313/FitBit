import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { errorMessage } from '../api/client';
import ErrorBanner from '../components/ErrorBanner';
import ThemeToggle from '../components/ThemeToggle';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);

  const set = (key) => (event) => setForm((prev) => ({ ...prev, [key]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await login(form);
      navigate('/', { replace: true });
    } catch (err) {
      setError(errorMessage(err, 'Could not sign in.'));
      setBusy(false);
    }
  };

  return (
    <div className="auth-shell">
      <ThemeToggle className="auth-theme-toggle" />
      <form className="card auth-card" onSubmit={submit}>
        <div className="auth-brand">
          <span className="brand-dot" aria-hidden="true" />
          FitBit
        </div>
        <h1>Welcome back</h1>
        <div className="card-sub">Sign in to pick up your training log.</div>

        {error && <div style={{ marginBottom: 14 }}><ErrorBanner message={error} /></div>}

        <div className="field">
          <label htmlFor="login-email">Email</label>
          <input
            id="login-email" type="email" className="input" required autoComplete="email"
            value={form.email} onChange={set('email')}
          />
        </div>

        <div className="field">
          <label htmlFor="login-password">Password</label>
          <input
            id="login-password" type="password" className="input" required autoComplete="current-password"
            value={form.password} onChange={set('password')}
          />
        </div>

        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>

        <div className="auth-alt">
          New here? <Link to="/register">Create an account</Link>
        </div>
      </form>
    </div>
  );
}
