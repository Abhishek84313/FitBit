# FitBit

Fitness tracking application built on RESTful services — activity logging, progress metrics and goal tracking wrapped in a clean, responsive interface.

**Stack:** C# .NET 10 Web API · React 19 (Vite) · MongoDB

---

## Prerequisites

| | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.x | `winget install --id Microsoft.DotNet.SDK.10 -e` |
| Node.js | 20.19+ / 22.12+ | built against v24 |
| MongoDB | 6.0+ | must be listening on `127.0.0.1:27017` |

The database `Fitbit` and its single collection `FitnessTracker` are used as-is; indexes are created automatically at startup.

## Running

Two terminals:

```powershell
# 1 — API  →  http://localhost:5099  (Swagger at /swagger)
dotnet run --project server\FitBit.Api --launch-profile http

# 2 — client  →  http://localhost:5173
cd client
npm install      # first time only
npm run dev
```

Open <http://localhost:5173>, register an account, and use **Seed sample data** (below) if you want a populated dashboard immediately.

## Architecture

```
server/FitBit.Api/
  Models/          UserDocument · ActivityDocument · GoalDocument · DocTypes
  Repositories/    MongoRepositoryBase (docType scoping) + one repo per doc type
  Services/        Auth · Activity · Goal · Dashboard · JwtToken · Seed · DateKey
  Controllers/     Auth · Activities · Goals · Dashboard · Dev (Development only)
client/src/
  api/             axios instance + interceptors, one module per resource
  context/         AuthContext (loading-gated session hydration)
  components/      TrendChart (hand-rolled SVG) · Meter · StatCard · tables · forms
  pages/           Login · Register · Dashboard · Activities · Goals
```

### One collection, three document shapes

Everything lives in `FitnessTracker`, discriminated by `docType`:

```js
{ _id, docType: "user",     email, displayName, passwordHash, createdAt }
{ _id, docType: "activity", userId, type, date, dateKey, durationMinutes,
                            distanceKm?, calories?, notes?, createdAt, updatedAt }
{ _id, docType: "goal",     userId, title, metric, targetValue, period,
                            startDate, endDate?, isActive, createdAt, updatedAt }
```

**The `docType` filter is a security boundary, not a convenience.** The driver does not inject it for you: its discriminator support is tied to `OfType<TDerived>()`, not to the collection's generic parameter, so `GetCollection<ActivityDocument>(…).Find(Empty)` issues a bare `find({})` and returns user documents — which, because `[BsonIgnoreExtraElements]` is required when three shapes share a collection, deserialize *successfully* into half-populated activities carrying a `passwordHash`.

So the filter cannot live at call sites. In `MongoRepositoryBase<T>` the `IMongoCollection<T>` is **private** and no member exposes it or an `IFindFluent`/`IQueryable` over it; every read and write routes through `Scoped()`, which ANDs in the repository's `docType`. Ownership is handled the same way — `userId` is part of the query, never a post-fetch `if`, and another user's document returns **404** rather than 403 (a 403 confirms the id exists).

### Dates

Activities store both `date` (a UTC instant, for sorting and range queries) and `dateKey` (the `"yyyy-MM-dd"` string of the calendar day the user meant). **All bucketing, grouping and goal-window comparison uses `dateKey`** — in IST (UTC+5:30) an activity logged before 5:30am local falls on the previous UTC day and would jump a column in the trend chart. `yyyy-MM-dd` sorts lexicographically, so string comparison is a correct range test.

### Goal progress

`currentValue`, `progressPercent` and `status` are computed on read, never stored — storing them would require invalidation on every activity write and guarantees the dashboard and the goals page eventually disagree. The dashboard reuses `GoalService.Project()`, so the two views cannot diverge.

Aggregation is in-memory LINQ rather than a `$group` pipeline: the trend must be zero-filled to exactly `days` entries (`$group` only returns days that have documents), a hand-built pipeline would reach around `Scoped()`, and a 7-day window is ~20 documents.

## API

| Verb | Route | Notes |
|---|---|---|
| POST | `/api/auth/register` | `201` · `409` if the email is taken |
| POST | `/api/auth/login` | `200` · `401` |
| GET | `/api/auth/me` | 🔒 |
| GET | `/api/activities` | 🔒 `?type=&from=&to=&page=&pageSize=&sort=` → `PagedResult` |
| GET | `/api/activities/types` | 🔒 static list |
| GET/POST/PUT/DELETE | `/api/activities[/{id}]` | 🔒 full CRUD |
| GET/POST/PUT/DELETE | `/api/goals[/{id}]` | 🔒 `?activeOnly=` |
| GET | `/api/dashboard/summary?days=7\|14\|30` | 🔒 totals, previous window, streak, zero-filled trend, goal progress |

🔒 = requires `Authorization: Bearer <jwt>`.

Login returns the **same 401** for an unknown email and a wrong password (and runs a dummy BCrypt verify when the user is missing) so neither the message nor the timing is a user-enumeration oracle.

### Development-only endpoints

Removed from the application model entirely outside `Development`:

| Verb | Route | |
|---|---|---|
| GET | `/api/dev/health` | Mongo reachability + document count |
| GET | `/api/dev/indexes` | confirms the partial unique email index without a shell |
| POST | `/api/dev/seed` | 🔒 ~18 activities over 14 days (with 2 deliberately empty) + 3 goals |
| DELETE | `/api/dev/reset` | 🔒 deletes **only the calling user's** documents |

Seed sample data:

```powershell
$base = "http://localhost:5099/api"
$reg = Invoke-RestMethod "$base/auth/register" -Method Post `
  -ContentType 'application/json; charset=utf-8' `
  -Body (@{ email="you@test.com"; password="Passw0rd!"; displayName="You" } | ConvertTo-Json)
Invoke-RestMethod "$base/dev/seed" -Method Post -Headers @{ Authorization = "Bearer $($reg.token)" }
```

> In Windows PowerShell 5.1 `curl` is an alias for `Invoke-WebRequest` and rejects `-X`/`-H`/`-d` — use `curl.exe` if you want real curl syntax.

## Notable implementation details

**Backend**
- `MapInboundClaims = false` so `sub` arrives verbatim; by default the JWT handler rewrites it to the WS-Federation `nameidentifier` URI and `FindFirst("sub")` returns null.
- `ClockSkew` tightened to 30s (the default is 5 minutes, which makes expiry tests lie).
- The unique email index is **partial** on `docType: "user"`. A plain unique `{email:1}` treats every activity and goal as `email: null`, and the *second activity insert* fails with `E11000`.
- `UseCors` runs before `UseAuthentication` — otherwise a preflight `OPTIONS` (no `Authorization` header) gets a 401 with no CORS headers and the browser reports a misleading CORS error.
- No `UseHttpsRedirection`: its 307s break the Vite dev proxy.
- Connection string uses `127.0.0.1`, not `localhost` — mongod binds IPv4 only and Windows resolves `localhost` to `::1` first, giving a 30s timeout that looks like the database being down.

**Frontend**
- The axios request interceptor reads the token from `localStorage`, not React state: it is registered once at module scope, so closing over state would capture `null` and every post-login request would go out unauthenticated.
- The 401 response interceptor **exempts `/auth/*`** — otherwise a wrong password on the login page redirects *to* the login page, remounting the form and wiping the error before it can be read.
- `ProtectedRoute` gates on `loading`. Without it every F5 momentarily has `user === null` while `/auth/me` is in flight and bounces an authenticated user to `/login`.
- Activity filters live in the URL (`useSearchParams`), so a filtered view is linkable and survives a refresh.
- Refetches dim the previous render instead of flashing a skeleton, which would cause a layout jump on every filter change.

**Chart** (`TrendChart.jsx`, hand-rolled SVG — 7 points in one series does not justify a charting library)
- Columns, not a line: with daily sums and genuinely empty days a line through zero implies a quantity that dipped, when the truth is "nothing was logged". Empty days render a 2px stub on the baseline.
- The SVG is drawn at its **measured pixel width** rather than a fixed `viewBox` scaled by `width:100%` — a fixed viewBox rescales the whole drawing, so 11px axis text renders ~17px on a wide card and ~5px at 375px.
- Columns capped at 24px with a 4px rounded top and square base; 2px surface gap between bands; solid hairline gridlines; only the peak is direct-labelled.
- Hover targets span the full band height and keyboard `Tab` opens the same panel; a **Table** toggle exposes every value without hover.
- Series colors (`#2a78d6` light / `#3987e5` dark) were validated against both surfaces, not eyeballed. Goal-status colors always ship with an icon and a text label, never color alone.

## Security notes for production

This is configured for local development. Before deploying:

1. **Move `Jwt:Key` out of `appsettings.json`** into user-secrets, environment variables or a key vault, and generate a fresh one.
2. **Enable MongoDB authentication** — the local server runs with `security` disabled.
3. Re-enable HTTPS and `UseHttpsRedirection`, and set `RequireHttpsMetadata = true`.
4. Narrow `Cors:AllowedOrigins` to the deployed frontend origin.
5. The JWT is kept in `localStorage`, which is readable by any XSS. That is a deliberate trade for a local app — an httpOnly refresh cookie (with the attendant CORS-credentials, SameSite and CSRF work) is the right call once this is exposed.
