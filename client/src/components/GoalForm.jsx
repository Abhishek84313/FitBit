import { useState } from 'react';
import { errorMessage } from '../api/client';
import { todayKey } from '../utils/date';
import { METRIC_LABELS, PERIOD_LABELS } from '../utils/format';
import ErrorBanner from './ErrorBanner';

const METRICS = ['durationMinutes', 'distanceKm', 'calories', 'sessions'];
const PERIODS = ['daily', 'weekly', 'monthly', 'custom'];

export default function GoalForm({ onSubmit }) {
  const [form, setForm] = useState({
    title: '',
    metric: 'durationMinutes',
    targetValue: 45,
    period: 'daily',
    startDate: todayKey(),
    endDate: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  const set = (key) => (event) => setForm((prev) => ({ ...prev, [key]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError(null);

    try {
      await onSubmit({
        title: form.title.trim(),
        metric: form.metric,
        targetValue: Number(form.targetValue),
        period: form.period,
        startDate: form.startDate,
        endDate: form.endDate === '' ? null : form.endDate,
        isActive: true,
      });
      setForm((prev) => ({ ...prev, title: '', targetValue: 45, endDate: '' }));
    } catch (err) {
      setError(errorMessage(err, 'Could not create the goal.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <form className="card" onSubmit={submit}>
      <div className="card-head">
        <div>
          <h2>New goal</h2>
          <div className="card-sub">
            Progress is computed from your logged activities, so it updates itself.
          </div>
        </div>
      </div>

      {error && <div style={{ marginBottom: 14 }}><ErrorBanner message={error} /></div>}

      <div className="form-grid">
        <div className="field" style={{ gridColumn: '1 / -1' }}>
          <label htmlFor="gf-title">Title</label>
          <input
            id="gf-title" className="input" required maxLength={80}
            placeholder="e.g. Run 25 km this week"
            value={form.title} onChange={set('title')}
          />
        </div>

        <div className="field">
          <label htmlFor="gf-metric">Metric</label>
          <select id="gf-metric" className="input" value={form.metric} onChange={set('metric')}>
            {METRICS.map((m) => <option key={m} value={m}>{METRIC_LABELS[m]}</option>)}
          </select>
        </div>

        <div className="field">
          <label htmlFor="gf-target">Target</label>
          <input
            id="gf-target" type="number" className="input" min="0.1" step="0.1" required
            value={form.targetValue} onChange={set('targetValue')}
          />
        </div>

        <div className="field">
          <label htmlFor="gf-period">Period</label>
          <select id="gf-period" className="input" value={form.period} onChange={set('period')}>
            {PERIODS.map((p) => <option key={p} value={p}>{PERIOD_LABELS[p]}</option>)}
          </select>
        </div>

        <div className="field">
          <label htmlFor="gf-start">Start date</label>
          <input id="gf-start" type="date" className="input" required value={form.startDate} onChange={set('startDate')} />
        </div>

        {form.period === 'custom' && (
          <div className="field">
            <label htmlFor="gf-end">End date</label>
            <input id="gf-end" type="date" className="input" required value={form.endDate} onChange={set('endDate')} />
            <span className="field-hint">Required for a custom range.</span>
          </div>
        )}
      </div>

      <div className="form-actions">
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? 'Creating…' : 'Create goal'}
        </button>
      </div>
    </form>
  );
}
