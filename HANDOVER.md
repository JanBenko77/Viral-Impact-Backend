# Viral Impact Backend — Technical Handover

This document is the technical handover for the Viral Impact backend, prepared for the next team
that takes over development. It describes what the system is, how to configure,
run, and deploy it, why it is built the way it is, and what to be aware of when extending it.

It assumes a developer who knows C# and .NET but has not seen this codebase before.

> A deeper justification of the architectural choices is in the graduation report
> (*Viral Impact Backend*, Section 3 — Theoretical Framework, and Section 4 — Process
> Documentation). This document focuses on operating and extending the system; it summarises
> the reasoning where it affects maintenance.

---

## 1. What the system is

A telemetry and account backend for the *Viral Impact* game. It has three moving parts:

| Component | Project / location | Role |
|-----------|-------------------|------|
| **REST API** | `ViralImpact.Api` | Authenticates requests, validates and stores telemetry, serves localization and dashboard data. The core of the system. |
| **Staff dashboard** | `DashboardApp` | Blazor WebAssembly site where staff log in and read per-user game stats and conversation history. |
| **Unity client** | *(separate Unity project, not in this solution)* | Captures conversation sessions and game stats in-game and POSTs them to the API with a JWT. |

Data flow: **Unity captures → API secures & stores → dashboard reads & displays.**

**Tech stack:** .NET 9.0 · ASP.NET Core Minimal APIs · Entity Framework Core 9 · ASP.NET Core
Identity · JWT Bearer auth · Blazor WebAssembly · Microsoft SQL Server (production) /
SQLite (available for local dev) · hosted on MonsterASP.NET.

---

## 2. Repository layout

```
ViralImpactBackend.sln
├── ViralImpact.Api/              ← the REST API
│   ├── Program.cs                ← startup, DI, auth, CORS, migration trigger
│   ├── Endpoints/                ← route groups (Client, Staff, Dashboard, Localization)
│   ├── Entities/                 ← EF entities (Auth/, Dashboard/)
│   ├── Dtos/                     ← request/response contracts
│   ├── Mapping/                  ← entity ↔ DTO extension methods
│   ├── Services/                 ← JwtTokenService
│   ├── Data/                     ← DbContexts, seeder, XLIFF importer, MigrateDb()
│   ├── Migrations/               ← EF migrations (UsersDb/, Staff/, root = Localization)
│   ├── XLIFF_EN.xlf / XLIFF_NL.xlf ← localization source files (copied to output)
│   └── appsettings.example.json  ← config template (real appsettings.json is NOT committed)
└── DashboardApp/                 ← Blazor WASM dashboard
    ├── Program.cs                ← reads ApiBaseUrl, registers HttpClient + services
    ├── Pages/                    ← Login, Index, Dashboard, Clients, UserDetail
    ├── Services/                 ← ApiService, AuthService, CustomAuthStateProvider, JwtParser
    ├── Models/                   ← DTOs mirroring the API responses
    └── wwwroot/appsettings.json  ← ApiBaseUrl (points at the API)
```

---

## 3. Configuration

### 3.1 API — `ViralImpact.Api/appsettings.json`

**This file is not in source control.** It is excluded via `.gitignore` and provided to the
host as a protected secret because it contains database passwords, the JWT signing key, and
the seed admin password. Copy `appsettings.example.json` to `appsettings.json` and fill in real
values. Keys:

| Key | Purpose | Notes |
|-----|---------|-------|
| `ConnectionStrings:UsersDb` | SQL Server connection for **both** the Users and Staff databases | See §6.1 — Staff currently shares this connection string. |
| `ConnectionStrings:LocalizationEntriesDb` | SQL Server connection for the localization database | |
| `Jwt:SecretKey` | HMAC-SHA256 signing key | **Minimum 32 characters.** Changing it invalidates every issued token. |
| `Jwt:Issuer` | Token issuer, validated on every request | Must match what the API issues. |
| `Jwt:Audience` | Token audience, validated on every request | |
| `Jwt:ExpirationMinutes` | Token lifetime | |
| `Cors:AllowedOrigins` | Allowed origins for the `UnityPolicy` CORS policy | Array of strings. |
| `StaffAdmin:Username` / `Password` / `Name` | Seed admin account created on first startup | **Change the password for production.** Defaults if omitted: `admin` / `Admin@123!` / `Administrator`. |

### 3.2 Dashboard — `DashboardApp/wwwroot/appsettings.json`

```json
{ "ApiBaseUrl": "http://gloviralimpact.runasp.net" }
```

Single setting: the base URL of the API. The dashboard refuses to start if it is missing.
Point it at `http://localhost:5026` for local development against a local API.

> **Note:** the API currently serves over **HTTP**, not HTTPS. The dashboard `ApiBaseUrl` must
> use the same scheme. Enabling HTTPS is a recommended next step (see §8).

---

## 4. Running locally

### Prerequisites
- **.NET 9.0 SDK**
- A reachable **SQL Server** instance (LocalDB, Express, or a remote server). The project also
  references the SQLite provider, so a context can be repointed at SQLite for quick local dev if
  preferred.
- **EF Core tools:** `dotnet tool install --global dotnet-ef`

### Steps
1. `cp ViralImpact.Api/appsettings.example.json ViralImpact.Api/appsettings.json` and fill in
   the connection strings and `Jwt:SecretKey`.
2. Run the API:
   ```
   cd ViralImpact.Api
   dotnet run
   ```
   API listens on `http://localhost:5026` (and `https://localhost:7016` via the `https` profile).
   On startup it **automatically** migrates the databases, imports the XLIFF files, and seeds the
   admin account (see §5).
3. Point the dashboard at the API in `DashboardApp/wwwroot/appsettings.json`, then:
   ```
   cd DashboardApp
   dotnet run
   ```
4. Log into the dashboard with the seeded `StaffAdmin` credentials.

---

## 5. Startup behaviour (`DataExtensions.MigrateDb`)

`await app.MigrateDb()` runs at the end of `Program.cs` on **every startup** and does the
following, in order, each in its own DI scope:

1. **Migrate** `LocalizationEntriesDb`.
2. **Import localization:** parses `XLIFF_EN.xlf` (language `en`) and `XLIFF_NL.xlf`
   (language `nl-NL`) from the application base directory. For each language it **deletes the
   existing rows for that language and re-imports**, so the XLIFF files are the source of truth.
3. **Migrate** `UsersDb`.
4. **Migrate** `StaffDb` (same connection as Users) and **seed** the admin account if it does
   not already exist.

If the host denies `CREATE DATABASE` (SQL error 262 - common on shared hosting where the
database is pre-created through the control panel), migration is skipped with a warning rather
than crashing. In that case **create the databases through the host panel first**; the migrations
then create the tables inside them.

**Implication:** you normally never run migrations by hand in production, deploying a build with
new migrations and restarting applies them. See §7 for adding migrations during development.

---

## 6. Data model & databases

### 6.1 Three DbContexts, two physical databases

| DbContext | Connection string used | Contains |
|-----------|------------------------|----------|
| `UsersDbContext` | `UsersDb` | Player accounts (Identity), `GameStats`, `ConversationSession`, `ConversationTurn` |
| `StaffDbContext` | `UsersDb` *(shared — see note)* | Caretaker/staff accounts (Identity) |
| `LocalizationEntriesDbContext` | `LocalizationEntriesDb` | `LocalizationEntry` rows |

> **Known nuance:** `StaffDbContext` is registered with the **`UsersDb`** connection string in
> `Program.cs`. Staff Identity tables therefore live in the same physical database as player
> data, but in a separate `DbContext` with its own migration history. The contexts are isolated
> at the code level (staff credentials are never queried through player-data code paths). If you
> want true physical separation, add a `StaffDb` connection string, point the context at it, and
> regenerate the Staff migration.

### 6.2 Core entities
- `ConversationSession` **1—∞** `ConversationTurn` (composition; turns cascade with the session).
- `ConversationSession` and `GameStats` each carry a `UserId` foreign key to the owning player.
- A `ConversationTurn` stores **localization keys** (NPC line, all player choices, chosen
  response, NPC reaction) — never raw dialogue text. Text is resolved at read time.

---

## 7. Adding / changing migrations

Each context keeps its migrations in a separate folder. Use the matching `-o` output path so the
structure stays consistent (run from `ViralImpact.Api/`):

```bash
# Users
dotnet ef migrations add <Name> --context UsersDbContext -o Migrations/UsersDb
# Staff
dotnet ef migrations add <Name> --context StaffDbContext -o Migrations/Staff
# Localization
dotnet ef migrations add <Name> --context LocalizationEntriesDbContext -o Migrations

# Apply (any context) — also happens automatically on startup
dotnet ef database update --context <UsersDbContext|StaffDbContext|LocalizationEntriesDbContext>
```

Always pass `--context`; the project has multiple contexts so EF cannot guess.

---

## 8. Updating localization

The dialogue text comes from the game's XLIFF export. To update it:

1. Replace `ViralImpact.Api/XLIFF_EN.xlf` and/or `ViralImpact.Api/XLIFF_NL.xlf` with the new
   exports. (They are marked `CopyToOutputDirectory=PreserveNewest` in the `.csproj`, so they
   ship with the build.)
2. Redeploy / restart the API. On startup the importer clears and re-imports that language.

**Format expectations** (XLIFF 2.0, namespace `urn:oasis:names:tc:xliff:document:2.0`): the
importer reads `file` → `group` → `unit` → `segment` → `target`, using `group/@name` as the
`GroupId`, `unit/@name` as the `UnitId`, and the `target` text as the value. Empty targets are
skipped. At read time the API returns a dictionary keyed `GroupId.UnitId` (e.g. `AC.AC-1-npc`).

---

## 9. API endpoints

15 endpoints across four route groups. Auth column = required JWT role.

| Group | Method | Route | Auth |
|-------|--------|-------|------|
| /client | POST | /register | — |
| /client | POST | /login | — |
| /client | POST | /gamestats | JWT (player) |
| /client | GET | /gamestats/{id} | JWT (player) |
| /client | POST | /conversations | JWT (player) |
| /staff | POST | /login | — |
| /staff | POST | /register | JWT (Admin) |
| /dashboard | GET | /users | JWT (staff) |
| /dashboard | GET | /users/{id} | JWT (staff) |
| /dashboard | GET | /users/{id}/stats | JWT (staff) |
| /dashboard | GET | /users/{id}/conversations | JWT (staff) |
| /dashboard | GET | /groups | JWT (staff) |
| /localization | GET | /{language} | — |
| /localization | GET | /{language}/{groupId} | — |
| /localization | GET | /{language}/{groupId}/{unitId} | — |

Two JWT token types are issued: **player** tokens (via `/client/login`) and **staff** tokens
(via `/staff/login`), with different role claims gating the groups above.

---

## 10. Deployment

### 10.1 API (MonsterASP.NET)
1. `cd ViralImpact.Api && dotnet publish -c Release`.
2. Upload the publish output to the host.
3. Provide a real `appsettings.json` on the server (it is **not** in the build/repo) with the
   production connection strings, JWT secret, CORS origins, and a strong `StaffAdmin:Password`.
4. Ensure `XLIFF_EN.xlf` and `XLIFF_NL.xlf` are present in the deployed folder (they are included
   in publish output automatically).
5. Pre-create the two databases in the host control panel if the account lacks `CREATE DATABASE`
   permission (see §5). Restart; migrations + seeding run on startup.

### 10.2 Dashboard (static Blazor WASM)
1. `cd DashboardApp && dotnet publish -c Release` → static site in `bin/Release/net9.0/publish/wwwroot`.
2. Set `wwwroot/appsettings.json` `ApiBaseUrl` to the deployed API URL **before** publishing
   (or edit it in the published output).
3. Host the `wwwroot` as a static site. **On IIS-based hosts the `web.config` must:**
   - register the `application/wasm` MIME type for `.wasm` files, and
   - add a SPA fallback rewrite so unknown routes serve `index.html`.
4. `index.html` must load the **Bootstrap JS bundle** before the Blazor script — without it the
   app shows a permanent loading spinner and the conversation accordions don't open (this was a
   real bug; see §11).

---

## 11. Known issues & gotchas

- **Staff DB shares the Users connection string** (§6.1) — intentional for now, document-worthy
  if you separate them.
- **Dashboard blank/forever-loading** → Bootstrap JS missing from `index.html`. Both the loading
  screen and the accordion dropdowns depend on it.
- **Localization keys containing dots** (e.g. `AC-1.5-npc`) are resolved by splitting the
  dictionary key on the **first** dot, not the last. Keep this in mind if you touch the dashboard's
  key resolution.
- **Timestamps from clients must be ISO 8601** (`DateTime.ToString("o")`). Unity's default
  `DateTime.ToString()` is not parsed by the API's JSON deserializer.
- **`Results.CreatedAtRoute` needs a real named route** — referencing a non-existent route name
  returns a 500. The conversations endpoint uses `Results.Created($"...")` to avoid this.
- **HTTP only** — the API is not yet served over HTTPS. The dashboard must match the scheme.
- **No token revocation** — JWTs are valid until they expire; there is no blocklist, so logging
  out or deactivating an account does not invalidate an already-issued token.

---

## 12. Design rationale (summary)

Condensed from the report; included so a maintainer understands *why* before changing things.

- **ASP.NET Core + Minimal APIs** — built-in Identity, EF Core, and JWT middleware in one stack;
  Minimal APIs keep the small endpoint surface low-boilerplate.
- **Strict DTO separation** (`CreateXDto` / `UpdateXDto` / `XDto`) — internal entities and EF
  navigation properties never cross the API boundary; the contract for Unity stays stable even if
  the data model changes.
- **JWT over session auth** — stateless, no server-side session store, aligns with REST; the
  trade-off (no built-in revocation) is noted above and in future work.
- **Separate DbContexts** — isolates staff credentials from player-data query paths.
- **Key-based localization** — storing localization keys instead of text means one stored
  conversation renders in both Dutch and English with no duplication.
- **SQLite (dev) → SQL Server (prod)** — EF Core abstracts both; switching is a provider +
  connection-string change, no query rewrites.

---

## 13. Recommended next steps

From the report's recommendations, in priority order:

1. **Enable HTTPS** on the API (host provides certificates) — most important before real data.
2. **Field-level input validation** — add data-annotation attributes (`[Required]`, `[Range]`,
   `[MaxLength]`) to the `Create*Dto` records.
3. **JWT revocation** — a JTI blocklist table checked during token validation, so accounts can be
   deactivated mid-session.
4. **Group-level analytics endpoint** — the data model already carries `GroupId`; expose
   aggregate views for behavioral scientists.
5. **Role-differentiated dashboard views** — caretakers vs. scientists see different data.
6. **In-game update/patching system** — descoped from this project; a version manifest checked by
   a Unity launcher is the suggested approach.
7. **Managed production hosting** (e.g. Azure SQL / App Service) before any large-scale use.