/** 1284 -> "1,284"; 12900 -> "12.9K"; 4200000 -> "4.2M" */
export function compact(value) {
  const n = Number(value) || 0;
  if (Math.abs(n) >= 1_000_000) return `${trim(n / 1_000_000)}M`;
  if (Math.abs(n) >= 10_000) return `${trim(n / 1_000)}K`;
  return n.toLocaleString(undefined, { maximumFractionDigits: 1 });
}

function trim(n) {
  return Number(n.toFixed(1)).toString();
}

/** 430 -> "7h 10m"; 45 -> "45m" */
export function duration(minutes) {
  const m = Math.round(Number(minutes) || 0);
  if (m < 60) return `${m}m`;
  const hours = Math.floor(m / 60);
  const rest = m % 60;
  return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`;
}

export function km(value) {
  const n = Number(value) || 0;
  return `${n.toLocaleString(undefined, { maximumFractionDigits: 1 })} km`;
}

export function percent(value) {
  return `${Number(value ?? 0).toLocaleString(undefined, { maximumFractionDigits: 1 })}%`;
}

/**
 * Signed change between two windows. Returns null when there is no prior data to
 * compare against — showing "+100%" against a zero baseline is noise, not signal.
 */
export function delta(current, previous) {
  const a = Number(current) || 0;
  const b = Number(previous) || 0;
  if (b === 0) return null;

  const change = ((a - b) / b) * 100;
  // Round first, then read the direction off the rounded figure: a raw +0.3%
  // displays as "+0%", and pairing that with a green up-arrow claims a rise the
  // number doesn't show. Flat is its own state.
  const rounded = Math.round(change);
  const direction = rounded > 0 ? 'up' : rounded < 0 ? 'down' : 'flat';

  return {
    value: change,
    label: `${direction === 'up' ? '+' : direction === 'down' ? '−' : '±'}${Math.abs(rounded)}%`,
    direction,
  };
}

/** Formats a goal's value pair for the meter's caption, e.g. "18.4 / 30 km". */
export function metricValue(metric, value) {
  const n = Number(value) || 0;
  switch (metric) {
    case 'durationMinutes':
      return duration(n);
    case 'distanceKm':
      return `${n.toLocaleString(undefined, { maximumFractionDigits: 1 })} km`;
    case 'calories':
      return `${compact(n)} kcal`;
    case 'sessions':
      return `${n} ${n === 1 ? 'session' : 'sessions'}`;
    default:
      return String(n);
  }
}

export const METRIC_LABELS = {
  durationMinutes: 'Active minutes',
  distanceKm: 'Distance',
  calories: 'Calories',
  sessions: 'Sessions',
};

export const PERIOD_LABELS = {
  daily: 'Today',
  weekly: 'This week',
  monthly: 'This month',
  custom: 'Custom range',
};
