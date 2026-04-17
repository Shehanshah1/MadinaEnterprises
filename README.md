# Madina Enterprises – Cotton Brokerage App

A cross-platform cotton brokerage management system built in **.NET MAUI** for
**Madina Enterprises**. It manages **Ginners, Mills, Contracts, Deliveries,
Payments**, and the **Ginner Ledger**, backed by a local **SQLite database**
with optional **Supabase cloud sync** so data follows you across machines.

---

## Project structure

```
MadinaEnterprises/
├── Modules/
│   ├── Models/           # Data models (Contracts, Ginners, Mills, etc.)
│   ├── Services/         # CloudSyncService and other shared services
│   ├── Util/
│   └── Views/            # XAML pages + code-behind
├── Platforms/Windows/    # Package.appxmanifest, .appinstaller template
├── Resources/Raw/
│   ├── supabase_schema.sql  # Run this once in your Supabase project
│   └── cloudsync.json       # Optional baked-in Supabase credentials
├── scripts/              # PowerShell helpers for signing and publishing
├── .github/workflows/    # release.yml — CI build + sign + publish on tag
├── DatabaseService.cs    # SQLite CRUD + cloud push/pull
├── App.xaml.cs           # App startup, sleep/resume sync hooks
├── DEPLOYMENT.md         # MSIX signing + release how-to
└── SUPABASE_SETUP.md     # Cloud sync setup steps
```

---

## Features

- **Ginners, Mills, Contracts, Deliveries, Payments** – full CRUD with
  friendly error messages when foreign-key constraints would be violated
  (e.g. you can't delete a ginner that still has contracts).
- **Ginner Ledger** – deal-level payment records with date and mill due
  tracking.
- **Supabase cloud sync** – every add / update / delete is fire-and-forget
  pushed to Supabase; on app start and resume the local DB is reconciled
  with the cloud (last-write-wins). Works offline and catches up later.
- **Modernised UI** – refreshed design system across every page.
- **Persistent sidebar navigation** from the Dashboard.
- **Offline-first SQLite** – data is always local; the cloud is a mirror.
- **Auto-updating Windows installer** via MSIX + `.appinstaller` hosted on
  GitHub Releases. Users install once, updates arrive on launch.

---

## Technologies

- **.NET 9 / .NET MAUI**
- **C# + XAML**
- **SQLite** (via `Microsoft.Data.Sqlite`) for local storage
- **Supabase** (PostgREST over HTTPS) for cloud sync
- **GitHub Actions** for signed MSIX release builds

---

## Running locally

1. **Clone**

   ```bash
   git clone https://github.com/Shehanshah1/MadinaEnterprises.git
   cd MadinaEnterprises
   ```

2. **Build in Visual Studio 2022 (17.12+) with the .NET 9 MAUI workload**
   - Primary target: **Windows (`net9.0-windows10.0.19041.0`)**
   - Other targets available in the csproj: Android, iOS, MacCatalyst

3. **Run** – the app opens on `LoginPage`. After login everything is reachable
   from the sidebar on the Dashboard, including the **Cloud Sync** panel.

---

## Data storage

Local SQLite DB is created at:

```
%LOCALAPPDATA%\madina.db3
```

Local cloud-sync credentials (if entered in the app) are stored at:

```
%LOCALAPPDATA%\madina\cloudsync.json
```

---

## Cloud sync (Supabase)

See [`SUPABASE_SETUP.md`](SUPABASE_SETUP.md) for full setup. Quick version:

1. Create a free Supabase project.
2. Run [`Resources/Raw/supabase_schema.sql`](Resources/Raw/supabase_schema.sql)
   in the Supabase SQL Editor (creates the 5 tables + RLS policies).
3. Launch the app → **Cloud Sync** in the sidebar → paste your Project URL
   and anon key → **Save Credentials**, then **Pull from Cloud** or
   **Push to Cloud**.

Each device can point at the same Supabase project to stay in sync.

---

## Deployment & updates

The Windows build ships via **GitHub Releases** as a signed MSIX, with an
`.appinstaller` URL that makes Windows auto-update the app on launch.

- Full process (certs, secrets, tagging): [`DEPLOYMENT.md`](DEPLOYMENT.md).
- Cut a release by pushing a strictly-increasing four-part tag:

  ```bash
  git tag v1.0.2.0
  git push origin v1.0.2.0
  ```

- User-facing install URL (never changes):

  ```
  https://github.com/Shehanshah1/MadinaEnterprises/releases/latest/download/MadinaEnterprises.appinstaller
  ```

---

## Authentication

Simple hardcoded login (no signup / email / password reset):

- Username: `Anees`
- Password: `0000`

Change the constants `HardcodedUsername` / `HardcodedPassword` in
`Modules/Views/LoginPage.xaml.cs` if needed.

---

## Contributing

Open to collaboration. Nice-to-haves:

- Charting & analytics
- Data export (CSV / Excel)
- Supabase Auth instead of the single anon-key model
- Dark mode toggle

---

## License

Proprietary — developed for **Madina Enterprises**. Redistribution is subject
to owner permission.

---

## LinkedIn / CV

### LinkedIn Project Description

> Developed a cross-platform enterprise business management system for Madina Enterprises, a cotton brokerage firm, using .NET MAUI (C# / XAML). The application manages the full brokerage workflow — ginner and mill onboarding, contracts, deliveries, payments, and ledger tracking — backed by an offline-first SQLite database with Supabase cloud sync so data stays consistent across devices. Features an automated CI/CD release pipeline via GitHub Actions that builds, signs, and publishes MSIX packages to GitHub Releases with zero-touch auto-update for Windows clients.

### CV Bullet Points

- Architected a cross-platform cotton brokerage management system in C# / .NET MAUI handling 1,000+ yearly B2B transactions, covering the full lifecycle from contracts and deliveries to payments and ginner ledger tracking.
- Designed an offline-first SQLite data layer with Supabase cloud sync (last-write-wins reconciliation on app start/resume), ensuring zero data loss across multi-device deployments.
- Built an end-to-end CI/CD release pipeline with GitHub Actions — automated MSIX signing, versioned GitHub Releases, and `.appinstaller`-based auto-update, eliminating manual deployment overhead.
- **Tech Stack:** C#, .NET 9 MAUI, XAML, SQLite, Supabase (PostgREST), GitHub Actions, MSIX/Windows packaging

---

**Built with .NET MAUI**
