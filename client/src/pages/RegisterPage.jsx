import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { errorMessage } from '../api/client';
import ErrorBanner from '../components/ErrorBanner';

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ displayName: '', email: '', password: '' });
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);

  const set = (key) => (event) => setForm((prev) => ({ ...prev, [key]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await register(form);
      navigate('/', { replace: true });
    } catch (err) {
      setError(errorMessage(err, 'Could not create the account.'));
      setBusy(false);
    }
  };

  return (
    <div className="auth-shell">
      <form className="card auth-card" onSubmit={submit}>
        <h1>Create your account</h1>
        <div className="card-sub">Start logging activities and tracking goals.</div>

        {error && <div style={{ marginBottom: 14 }}><ErrorBanner message={error} /></div>}

        <div className="field">
          <label htmlFor="reg-name">Display name</label>
          <input
            id="reg-name" className="input" required maxLength={60} autoComplete="name"
            value={form.displayName} onChange={set('displayName')}
          />
        </div>

        <div className="field">
          <label htmlFor="reg-email">Email</label>
          <input
            id="reg-email" type="email" className="input" required autoComplete="email"
            value={form.email} onChange={set('email')}
          />
        </div>

        <div className="field">
          <label htmlFor="reg-password">Password</label>
          <input
            id="reg-password" type="password" className="input" required minLength={8}
            autoComplete="new-password"
            value={form.password} onChange={set('password')}
          />
          <span className="field-hint">At least 8 characters.</span>
        </div>

        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Creating…' : 'Create account'}
        </button>

        <div className="auth-alt">
          Already have an account? <Link to="/login">Sign in</Link>
        </div>
      </form>
    </div>
  );
}
