<div align="center">

```
███╗   ███╗ █████╗ ██████╗ ██╗███╗   ██╗ █████╗
████╗ ████║██╔══██╗██╔══██╗██║████╗  ██║██╔══██╗
██╔████╔██║███████║██║  ██║██║██╔██╗ ██║███████║
██║╚██╔╝██║██╔══██║██║  ██║██║██║╚██╗██║██╔══██║
██║ ╚═╝ ██║██║  ██║██████╔╝██║██║ ╚████║██║  ██║
╚═╝     ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝

███████╗███╗   ██╗████████╗███████╗██████╗ ██████╗ ██████╗ ██╗███████╗███████╗███████╗
██╔════╝████╗  ██║╚══██╔══╝██╔════╝██╔══██╗██╔══██╗██╔══██╗██║██╔════╝██╔════╝██╔════╝
█████╗  ██╔██╗ ██║   ██║   █████╗  ██████╔╝██████╔╝██████╔╝██║███████╗█████╗  ███████╗
██╔══╝  ██║╚██╗██║   ██║   ██╔══╝  ██╔══██╗██╔═══╝ ██╔══██╗██║╚════██║██╔══╝  ╚════██║
███████╗██║ ╚████║   ██║   ███████╗██║  ██║██║     ██║  ██║██║███████║███████╗███████║
╚══════╝╚═╝  ╚═══╝   ╚═╝   ╚══════╝╚═╝  ╚═╝╚═╝     ╚═╝  ╚═╝╚═╝╚══════╝╚══════╝╚══════╝
```

### Cotton Brokerage Management — Desktop Edition

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/.NET_MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/maui/)
[![SQLite](https://img.shields.io/badge/SQLite-Offline--First-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Supabase](https://img.shields.io/badge/Supabase-Cloud_Sync-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)](https://supabase.com/)
[![Windows](https://img.shields.io/badge/Windows-10%2B-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-CI%2FCD-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)](https://github.com/features/actions)
[![License](https://img.shields.io/badge/License-Proprietary-red?style=for-the-badge)](./LICENSE)

*A production-grade, offline-first Windows desktop application for managing every facet of a cotton brokerage operation — from ginner contracts to mill deliveries, payments, and real-time cloud sync.*

</div>

---

## 📋 Table of Contents

- [✨ Overview](#-overview)
- [🚀 Features](#-features)
- [🏗️ Architecture](#️-architecture)
- [📂 Project Structure](#-project-structure)
- [🗄️ Data Model](#️-data-model)
- [🛠️ Tech Stack](#️-tech-stack)
- [⚡ Getting Started](#-getting-started)
- [☁️ Cloud Sync Setup](#️-cloud-sync-setup)
- [📦 Deployment & Auto-Updates](#-deployment--auto-updates)
- [🔐 Authentication](#-authentication)
- [📊 Export Capabilities](#-export-capabilities)
- [🗺️ Application Flow](#️-application-flow)
- [🤝 Contributing](#-contributing)
- [📜 License](#-license)

---

## ✨ Overview

**Madina Enterprises** is a purpose-built desktop application that handles the full lifecycle of a cotton brokerage business:

```
Ginner (Supplier) ──► Contract ──► Mill (Buyer)
                         │
              ┌──────────┴──────────┐
              │                     │
          Deliveries            Payments
          (Shipments)         (Transactions)
              │
          Ginner Ledger
        (Deal-level view)
```

Data lives **locally first** in SQLite — the app works perfectly with zero internet. When connectivity is available, changes are silently mirrored to a **Supabase cloud database**, keeping every machine in sync.

---

## 🚀 Features

### 🏢 Core Business Modules

| Module | Capabilities |
|--------|-------------|
| **👨‍🌾 Ginners** | Add, edit, delete cotton suppliers with full contact & banking details |
| **🏭 Mills** | Manage cotton mill buyers with owner and address info |
| **📄 Contracts** | Create deals linking ginners to mills with price, bales, commission |
| **🚚 Deliveries** | Track shipments — factory weight, mill weight, truck & driver info |
| **💰 Payments** | Record and reconcile payments per contract |
| **📒 Ginner Ledger** | Ginner-level view of all deals, deliveries, and outstanding amounts |

### ☁️ Offline-First Cloud Sync

```
  ┌─────────────────────────────────────────────────────────┐
  │                    Your Machine                         │
  │                                                         │
  │   App  ──►  SQLite DB  ──►  Background Sync  ──►  ☁️   │
  │             (always up)    (fire-and-forget)   Supabase │
  │                                                         │
  │   ✅ Works offline   ✅ Never blocks UI   ✅ Auto-sync  │
  └─────────────────────────────────────────────────────────┘
```

- Every write is **instantly committed to local SQLite** — zero network latency in the UI
- Changes are **pushed to Supabase asynchronously** in the background
- On app start & resume, the local DB is **reconciled from the cloud** (last-write-wins)
- Sync errors are **captured silently** — data is never lost

### 📊 Dashboard KPIs

> At a glance, always know where your business stands.

- 💵 **Total Commission Earned**
- 📈 **Total Paid vs Total Due**
- 📦 **Bales Sold** across all contracts
- 🧮 **Average Commission Rate**
- 👥 **Active Ginners & Mills count**

### 📤 Data Export

- **Excel Workbooks** — master export (Contracts, Deliveries, Payments, Summary sheets) + per-ginner workbooks with calculated columns (kg → maund conversions, rate × weight)
- **Word Documents** — formatted contract documents with buyer/seller/terms

### 🔄 Auto-Updating Windows Installer

- Ship once via **MSIX + `.appinstaller`** hosted on GitHub Releases
- Windows checks for updates on every launch — users always run the latest version
- No manual download or update process ever needed

### 🧭 Persistent Sidebar Navigation

Every page is reachable from a persistent sidebar on the Dashboard — Ginners, Mills, Contracts, Deliveries, Payments, Ginner Ledger, Cloud Sync, and Logout.

### 🛡️ Smart Foreign-Key Validation

Deleting a ginner with active contracts? The app catches the SQLite FK violation and shows a friendly, human-readable message — never a raw crash:

> *"Cannot delete this ginner because 3 contracts are still linked to them."*

---

## 🏗️ Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                        .NET MAUI App                               │
│                                                                    │
│  ┌──────────────┐   ┌──────────────┐   ┌────────────────────────┐ │
│  │    Views     │   │    Models    │   │       Services         │ │
│  │  (9 Pages)   │   │ (5 Entities) │   │ CloudSyncService       │ │
│  │  XAML + C#   │──►│  Ginners     │   │ CloudConfig            │ │
│  │              │   │  Mills       │   │ ExportHelper           │ │
│  │ LoginPage    │   │  Contracts   │   │ RateCalculation        │ │
│  │ Dashboard    │   │  Deliveries  │   └────────────────────────┘ │
│  │ Ginners      │   │  Payments    │             │                 │
│  │ Mills        │   └──────────────┘             │                 │
│  │ Contracts    │                                ▼                 │
│  │ Deliveries   │   ┌───────────────────────────────────────────┐ │
│  │ Payments     │   │          DatabaseService (Singleton)       │ │
│  │ Ledger       │──►│  SQLite CRUD + Cloud Push/Pull            │ │
│  │ CloudSync    │   │  FK validation + friendly error messages   │ │
│  └──────────────┘   └───────────────────────────────────────────┘ │
│                                  │              │                  │
│                                  ▼              ▼                  │
│                          📁 SQLite DB     ☁️ Supabase             │
│                         %LOCALAPPDATA%     PostgREST API           │
│                           madina.db3                               │
└────────────────────────────────────────────────────────────────────┘
```

**Key design principles:**
- **Singleton DatabaseService** — one source of truth for all data access
- **Offline-first** — local SQLite always wins; cloud is a non-blocking mirror
- **Non-blocking cloud** — all HTTP calls are fire-and-forget; the UI never waits on the network
- **Config with fallbacks** — credentials load from local file → packaged asset → environment variables

---

## 📂 Project Structure

```
MadinaEnterprises/
│
├── 📄 DatabaseService.cs          # SQLite CRUD + cloud orchestration (894 lines)
├── 📄 App.xaml(.cs)               # App entry point, lifecycle sync hooks
├── 📄 MauiProgram.cs              # MAUI DI & font registration
│
├── 📁 Modules/
│   ├── 📁 Models/                 # Core data entities
│   │   ├── Ginners.cs
│   │   ├── Mills.cs
│   │   ├── Contracts.cs
│   │   ├── Deliveries.cs
│   │   └── Payment.cs
│   │
│   ├── 📁 Services/               # Cloud sync infrastructure
│   │   ├── CloudSyncService.cs    # PostgREST HTTP client (upsert/delete/fetch)
│   │   └── CloudConfig.cs         # Credential loader (file > asset > env var)
│   │
│   ├── 📁 Util/                   # Business logic
│   │   ├── RateCalculation.cs     # kg ↔ maund conversion & pricing
│   │   └── ExportHelper.cs        # Excel & Word export (740 lines)
│   │
│   └── 📁 Views/                  # UI pages (XAML + code-behind)
│       ├── LoginPage.xaml(.cs)
│       ├── DashboardPage.xaml(.cs)
│       ├── GinnersPage.xaml(.cs)
│       ├── MillsPage.xaml(.cs)
│       ├── ContractsPage.xaml(.cs)
│       ├── DeliveriesPage.xaml(.cs)
│       ├── PaymentsPage.xaml(.cs)
│       ├── GinnerLedgerPage.xaml(.cs)
│       └── CloudSyncPage.xaml(.cs)
│
├── 📁 Platforms/
│   ├── 📁 Windows/
│   │   ├── Package.appxmanifest          # App identity & MSIX metadata
│   │   ├── MadinaEnterprises.appinstaller # Auto-update manifest template
│   │   └── app.manifest
│   ├── 📁 Android/
│   ├── 📁 iOS/
│   └── 📁 MacCatalyst/
│
├── 📁 Resources/
│   ├── 📁 Raw/
│   │   ├── supabase_schema.sql    # One-time Supabase setup (5 tables + RLS)
│   │   └── cloudsync.json         # Optional baked-in credentials
│   └── 📁 Styles/
│       ├── Colors.xaml
│       └── Styles.xaml
│
├── 📁 scripts/
│   ├── New-SigningCertificate.ps1  # Generate self-signed MSIX cert
│   └── Publish-MsixRelease.ps1    # Build, sign, stage release
│
├── 📁 .github/workflows/
│   └── release.yml                # CI: build → sign → publish on tag
│
├── 📄 DEPLOYMENT.md               # Full MSIX release guide
├── 📄 SUPABASE_SETUP.md           # Cloud sync setup walkthrough
└── 📄 MadinaEnterprises.csproj   # Multi-target build config
```

---

## 🗄️ Data Model

```
┌─────────────────────┐        ┌─────────────────────┐
│       GINNERS        │        │        MILLS         │
├─────────────────────┤        ├─────────────────────┤
│ GinnerID        (PK)│        │ MillID          (PK) │
│ GinnerName          │        │ MillName             │
│ Contact             │        │ Address              │
│ IBAN                │        │ OwnerName            │
│ Address             │        └──────────┬───────────┘
│ NTN / STN           │                   │
│ BankAddress         │                   │
│ ContactPerson       │                   │
│ Station             │                   │
└──────────┬──────────┘                   │
           │                              │
           │        ┌─────────────────────▼──────────────────────┐
           └───────►│                CONTRACTS                    │
                    ├─────────────────────────────────────────────┤
                    │ ContractID          (PK)                    │
                    │ GinnerID            (FK → Ginners)          │
                    │ MillID              (FK → Mills)            │
                    │ TotalBales                                  │
                    │ PricePerBatch                               │
                    │ TotalAmount                                 │
                    │ CommissionPercentage                        │
                    │ DateCreated                                 │
                    │ DeliveryNotes / PaymentNotes / Description  │
                    └──────────┬──────────────────────────────────┘
                               │
              ┌────────────────┴────────────────┐
              │                                 │
   ┌──────────▼──────────┐          ┌───────────▼─────────┐
   │      DELIVERIES      │          │       PAYMENTS       │
   ├─────────────────────┤          ├─────────────────────┤
   │ DeliveryID      (PK)│          │ PaymentID       (PK) │
   │ ContractID  (FK→Con)│          │ ContractID  (FK→Con) │
   │ Amount              │          │ TotalAmount          │
   │ TotalBales          │          │ AmountPaid           │
   │ FactoryWeight       │          │ TotalBales           │
   │ MillWeight          │          │ Date                 │
   │ TruckNumber         │          │ TransactionID        │
   │ DriverContact       │          └─────────────────────┘
   │ DepartureDate       │
   │ DeliveryDate        │
   └─────────────────────┘
```

All 5 tables are **mirrored in Supabase** with `updated_at` timestamps and RLS policies. Foreign key `RESTRICT` on delete prevents orphaned records, with friendly error messages surfaced in the UI.

---

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **UI Framework** | .NET 9 + .NET MAUI | Cross-platform desktop/mobile app |
| **Language** | C# 12 + XAML | App logic and UI markup |
| **Local Database** | SQLite via `Microsoft.Data.Sqlite` | Offline-first data storage |
| **ORM** | sqlite-net-pcl | Model-to-table mapping |
| **Cloud Sync** | Supabase PostgREST | REST API over hosted PostgreSQL |
| **Excel Export** | ClosedXML | Multi-sheet workbooks with formulas |
| **Word Export** | DocumentFormat.OpenXml | Contract document generation |
| **CI/CD** | GitHub Actions | Automated MSIX build, sign & release |
| **Installer** | MSIX + .appinstaller | Windows auto-updating packages |
| **Scripts** | PowerShell 7 | Certificate generation & release staging |

### Package Versions

```xml
<PackageReference Include="ClosedXML"                     Version="0.105.0" />
<PackageReference Include="DocumentFormat.OpenXml"        Version="3.3.0"   />
<PackageReference Include="Microsoft.Data.Sqlite"         Version="9.0.0"   />
<PackageReference Include="sqlite-net-pcl"                Version="1.9.172" />
```

---

## ⚡ Getting Started

### Prerequisites

| Requirement | Version |
|-------------|---------|
| Visual Studio | 2022 (17.12 or later) |
| .NET SDK | 9.0 |
| .NET MAUI Workload | Latest |
| Windows | 10 version 1903+ (build 18362+) |

### Installation

**1. Clone the repository**

```bash
git clone https://github.com/Shehanshah1/MadinaEnterprises.git
cd MadinaEnterprises
```

**2. Install the MAUI workload** *(if not already installed)*

```bash
dotnet workload install maui
```

**3. Open in Visual Studio**

Open `MadinaEnterprises.sln`, set the target to **Windows Machine**, and press **F5**.

> 💡 The primary build target is `net9.0-windows10.0.19041.0`. Android, iOS, and MacCatalyst targets are present in the csproj but Windows is the production target.

**4. Launch**

The app starts at `LoginPage`. Use the credentials below to log in. The local SQLite database is created automatically at `%LOCALAPPDATA%\madina.db3` on first run.

### Data Locations

| Data | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\madina.db3` |
| Cloud credentials | `%LOCALAPPDATA%\madina\cloudsync.json` |

---

## ☁️ Cloud Sync Setup

> Full walkthrough: [`SUPABASE_SETUP.md`](SUPABASE_SETUP.md)

**Quick setup (3 steps):**

**1.** Create a free project at [supabase.com](https://supabase.com)

**2.** Run the schema in the Supabase SQL Editor:

```sql
-- File: Resources/Raw/supabase_schema.sql
-- Creates: ginners, mills, contracts, deliveries, payments tables + RLS policies
```

**3.** In the app → **Dashboard** → **Cloud Sync** → paste your Project URL and anon key → **Save Credentials**

```
Supabase URL:  https://xxxxxxxxxxxx.supabase.co
Anon Key:      eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Then click **Push to Cloud** or **Pull from Cloud** to sync.

### Credential Resolution Order

The app loads Supabase credentials from the first available source:

```
1️⃣  %LOCALAPPDATA%\madina\cloudsync.json   ← user-configured in app
2️⃣  Resources/Raw/cloudsync.json           ← baked into the package
3️⃣  MADINA_SUPABASE_URL env var            ← for CI/CD / automation
    MADINA_SUPABASE_ANON_KEY env var
```

---

## 📦 Deployment & Auto-Updates

> Full guide: [`DEPLOYMENT.md`](DEPLOYMENT.md)

The Windows release pipeline is fully automated:

```
git tag v1.0.2.0 && git push origin v1.0.2.0
         │
         ▼
  GitHub Actions (release.yml)
         │
         ├─ Setup .NET 9 + MAUI workloads
         ├─ Decode PFX from GitHub secret
         ├─ Run Publish-MsixRelease.ps1
         │    ├─ Stamp version in Package.appxmanifest
         │    ├─ Build MSIX (net9.0-windows)
         │    ├─ Sign with self-signed cert
         │    └─ Stage release artifacts
         │
         └─ Publish GitHub Release
              ├─ MadinaEnterprises.msix
              ├─ MadinaEnterprises.appinstaller
              └─ MadinaEnterprises.cer (public cert)
```

### Versioning Rules

Versions must be strictly increasing four-part numbers:

```bash
git tag v1.0.0.0   # ✅ Initial release
git tag v1.0.1.0   # ✅ Patch
git tag v1.1.0.0   # ✅ Minor
git tag v1.0.0.0   # ❌ Already exists — Windows will refuse the update
```

### User Install URL

Users install **once** from this URL — Windows handles all future updates automatically:

```
https://github.com/Shehanshah1/MadinaEnterprises/releases/latest/download/MadinaEnterprises.appinstaller
```

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `SIGNING_CERT_PFX_BASE64` | Base64-encoded PFX signing certificate |
| `SIGNING_CERT_PASSWORD` | PFX certificate password |

Generate a self-signed cert with the included helper:

```powershell
.\scripts\New-SigningCertificate.ps1
```

---

## 🔐 Authentication

The app uses a single hardcoded enterprise login — no signup, no password reset, no email.

| Field | Value |
|-------|-------|
| Username | `Anees` |
| Password | `0000` |

To change the credentials, edit the constants in `Modules/Views/LoginPage.xaml.cs`:

```csharp
private const string HardcodedUsername = "Anees";
private const string HardcodedPassword = "0000";
```

---

## 📊 Export Capabilities

### Excel Export (via ClosedXML)

**Master Workbook** — one file with 4 sheets:

| Sheet | Contents |
|-------|----------|
| Contracts | All contracts with ginner & mill names |
| Deliveries | All shipments with calculated weights |
| Payments | All payment records with balances |
| Summary | Aggregated KPIs and totals |

**Per-Ginner Workbooks** — one file per ginner showing their full deal history.

> Unit conversions are applied automatically: `1 maund = 37.3242 kg`

### Word Export (via DocumentFormat.OpenXml)

Generates a formatted `.docx` contract document per deal, including buyer/seller details, bale counts, price terms, and delivery notes.

---

## 🗺️ Application Flow

```
  ┌───────────────┐
  │   LoginPage   │  ← Anees / 0000
  └───────┬───────┘
          │
          ▼
  ┌───────────────┐     Sidebar Navigation
  │  DashboardPage│─────────────────────────────────────┐
  │  (KPI cards)  │                                     │
  └───────────────┘                                     │
                                                        │
       ┌───────────┬────────────┬──────────┬────────────┼──────────┬────────────────┐
       ▼           ▼            ▼          ▼            ▼          ▼                ▼
  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌─────────────┐ ┌──────────┐
  │ Ginners │ │  Mills  │ │Contracts │ │Deliveries│ │ Payments │ │GinnerLedger │ │CloudSync │
  │  CRUD   │ │  CRUD   │ │   CRUD   │ │   CRUD   │ │   CRUD   │ │ Deal View   │ │Push/Pull │
  └─────────┘ └─────────┘ └──────────┘ └──────────┘ └──────────┘ └─────────────┘ └──────────┘
```

---

## 🤝 Contributing

Contributions and feedback are welcome! The following features are on the roadmap:

| Feature | Status |
|---------|--------|
| 📊 Charting & analytics dashboard | Planned |
| 📁 CSV export | Planned |
| 🔐 Supabase Auth (multi-user) | Planned |
| 🌙 Dark mode toggle | Planned |
| 📱 Android / iOS UI polish | Planned |

To contribute:

1. Fork the repository
2. Create a branch: `git checkout -b feature/your-feature`
3. Commit your changes with clear messages
4. Push and open a Pull Request

---

## 📜 License

**Proprietary** — developed exclusively for **Madina Enterprises**.

Redistribution, modification, or commercial use outside of Madina Enterprises requires explicit written permission from the owner.

---

<div align="center">

**Built with ❤️ using .NET MAUI**

[![.NET MAUI](https://img.shields.io/badge/Powered_by-.NET_MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/maui/)

*Madina Enterprises — Connecting Ginners & Mills since day one.*

</div>
