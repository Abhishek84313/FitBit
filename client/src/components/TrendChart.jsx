import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { compact, duration, km } from '../utils/format';
import { formatKey } from '../utils/date';

// The SVG is rendered at its MEASURED pixel width rather than a fixed viewBox
// scaled by width:100%. With a fixed viewBox the whole drawing is scaled, so
// 11px axis text renders ~17px on a wide card and ~5px at 375px — illegible on
// a phone. Drawing 1:1 keeps type at its nominal size at every width and makes
// the 24px bar cap an actual 24px.
const HEIGHT = 260;               // includes the x-axis label band, so the card
const PAD = { top: 22, right: 12, bottom: 34, left: 46 };   // grows no scrollbar

const MAX_BAR_W = 24;   // cap the mark; the band's leftover is air
const BAR_GAP = 2;      // surface-colored separation, never a stroke
const ZERO_STUB = 2;    // an empty day is present without reading as a value
const CAP_R = 4;        // rounded top only

const METRICS = {
  durationMinutes: { label: 'Active minutes', format: duration },
  activities: { label: 'Activities', format: (v) => String(v) },
  distanceKm: { label: 'Distance', format: km },
  calories: { label: 'Calories', format: (v) => `${compact(v)} kcal` },
};

/**
 * Picks a round tick step first, then derives the axis top from it — so ticks
 * land on 0/50/100/150 rather than 0/38/75/113/150.
 */
function axisScale(peak, targetTicks = 4) {
  if (peak <= 0) return { max: 10, step: 5 };

  const raw = peak / targetTicks;
  const magnitude = 10 ** Math.floor(Math.log10(raw));
  const normalized = raw / magnitude;
  const step = (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5 ? 2.5 : normalized <= 5 ? 5 : 10)
    * magnitude;

  return { max: Math.ceil(peak / step) * step, step };
}

/** A column with two rounded top corners and a square base anchored to the axis. */
function columnPath(x, y, w, h, r) {
  const radius = Math.min(r, w / 2, h);
  return [
    `M ${x} ${y + h}`,
    `L ${x} ${y + radius}`,
    `Q ${x} ${y} ${x + radius} ${y}`,
    `L ${x + w - radius} ${y}`,
    `Q ${x + w} ${y} ${x + w} ${y + radius}`,
    `L ${x + w} ${y + h}`,
    'Z',
  ].join(' ');
}

export default function TrendChart({ trend, metric = 'durationMinutes' }) {
  const wrapRef = useRef(null);
  const [width, setWidth] = useState(640);
  const [active, setActive] = useState(null);
  const config = METRICS[metric] ?? METRICS.durationMinutes;

  useLayoutEffect(() => {
    const element = wrapRef.current;
    if (!element) return undefined;

    const measure = () => setWidth(Math.max(280, element.clientWidth));
    measure();

    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  // A width change can move the hovered band out from under the pointer.
  useEffect(() => setActive(null), [width, trend]);

  const plotW = Math.max(80, width - PAD.left - PAD.right);
  const plotH = HEIGHT - PAD.top - PAD.bottom;

  const { bars, ticks, max } = useMemo(() => {
    const values = trend.map((point) => Number(point[metric]) || 0);
    const peak = Math.max(...values, 0);
    const { max: top, step } = axisScale(peak);

    const bandW = plotW / Math.max(trend.length, 1);
    const barW = Math.min(MAX_BAR_W, Math.max(5, bandW - BAR_GAP * 2));
    const peakIndex = values.indexOf(peak);

    const computed = trend.map((point, i) => {
      const value = values[i];
      const bandX = PAD.left + bandW * i;
      return {
        point,
        value,
        bandX,
        bandW,
        barX: bandX + (bandW - barW) / 2,
        barW,
        height: top > 0 ? (value / top) * plotH : 0,
        // Direct-label only the peak. A number on every column is chaos and goes
        // unread; the axis and the tooltip carry the rest.
        isPeak: i === peakIndex && value > 0,
      };
    });

    const computedTicks = [];
    for (let value = 0; value <= top + 1e-9; value += step) {
      computedTicks.push({ value, y: PAD.top + plotH - (value / top) * plotH });
    }

    return { bars: computed, ticks: computedTicks, max: top };
  }, [trend, metric, plotW, plotH]);

  const activeBar = active === null ? null : bars[active];
  // Keep the tooltip inside the card at either edge.
  const tipLeft = activeBar
    ? Math.min(Math.max(activeBar.bandX + activeBar.bandW / 2, 92), Math.max(width - 92, 92))
    : 0;

  return (
    <div className="chart-wrap" ref={wrapRef} style={{ position: 'relative' }}>
      <svg
        width={width}
        height={HEIGHT}
        viewBox={`0 0 ${width} ${HEIGHT}`}
        role="img"
        aria-label={`${config.label} for each of the last ${trend.length} days. Switch to the table view for exact values.`}
      >
        {/* Gridlines: solid 1px hairlines, one step off the surface, recessive */}
        {ticks.map((tick) => (
          <line
            key={tick.value}
            x1={PAD.left}
            x2={PAD.left + plotW}
            y1={tick.y}
            y2={tick.y}
            stroke={tick.value === 0 ? 'var(--baseline)' : 'var(--grid)'}
            strokeWidth="1"
          />
        ))}

        {ticks.map((tick) => (
          <text
            key={`tick-${tick.value}`}
            x={PAD.left - 9}
            y={tick.y + 4}
            textAnchor="end"
            fontSize="11"
            fill="var(--text-muted)"
            style={{ fontVariantNumeric: 'tabular-nums' }}
          >
            {max >= 10000 ? compact(tick.value) : tick.value.toLocaleString()}
          </text>
        ))}

        {bars.map((bar, i) => {
          const isZero = bar.value === 0;
          const h = isZero ? ZERO_STUB : Math.max(bar.height, CAP_R);
          const y = PAD.top + plotH - h;

          return (
            <g key={bar.point.dateKey}>
              {isZero ? (
                <rect x={bar.barX} y={y} width={bar.barW} height={h} fill="var(--grid)" />
              ) : (
                <path
                  d={columnPath(bar.barX, y, bar.barW, h, CAP_R)}
                  fill="var(--series-1)"
                  opacity={active !== null && active !== i ? 0.55 : 1}
                />
              )}

              {bar.isPeak && (
                <text
                  x={bar.barX + bar.barW / 2}
                  y={y - 7}
                  textAnchor="middle"
                  fontSize="11"
                  fontWeight="600"
                  /* Text wears ink tokens, never the series color */
                  fill="var(--text-secondary)"
                >
                  {config.format(bar.value)}
                </text>
              )}

              <text
                x={bar.bandX + bar.bandW / 2}
                y={HEIGHT - 12}
                textAnchor="middle"
                fontSize="11"
                fill="var(--text-muted)"
              >
                {bar.point.weekday}
              </text>

              {/* Full-band-height hit target, wider than the column itself, so
                  hovering is forgiving. Keyboard reaches the same panel. */}
              <rect
                className="chart-band"
                x={bar.bandX}
                y={PAD.top}
                width={bar.bandW}
                height={plotH}
                tabIndex={0}
                role="button"
                aria-label={`${formatKey(bar.point.dateKey)}: ${config.format(bar.value)}`}
                onMouseEnter={() => setActive(i)}
                onMouseLeave={() => setActive(null)}
                onFocus={() => setActive(i)}
                onBlur={() => setActive(null)}
              />
            </g>
          );
        })}
      </svg>

      {activeBar && (
        <div className="chart-tooltip" style={{ left: tipLeft, top: 0, transform: 'translateX(-50%)' }}>
          <div className="tip-day">{formatKey(activeBar.point.dateKey)}</div>
          <dl>
            <dt>Activities</dt>
            <dd>{activeBar.point.activities}</dd>
            <dt>Active minutes</dt>
            <dd>{duration(activeBar.point.durationMinutes)}</dd>
            <dt>Distance</dt>
            <dd>{km(activeBar.point.distanceKm)}</dd>
            <dt>Calories</dt>
            <dd>{compact(activeBar.point.calories)}</dd>
          </dl>
        </div>
      )}
    </div>
  );
}
