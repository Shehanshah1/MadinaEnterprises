# Madina Enterprises – Cotton Brokerage App

This is a cross-platform cotton brokerage management system developed in **.NET MAUI** for **Madina Enterprises**. The app provides modules to manage core entities such as **Ginners, Mills, Contracts, Deliveries, Payments**, and **Ginner Ledger**, all powered by a local **SQLite database**.

---

## 🏗 Project Structure

```
MadinaEnterprises/
├── Modules/
│   ├── Models/        # Data Models (e.g., Contracts, Ginners, Mills, etc.)
│   ├── Views/         # XAML UI Pages with Code-Behind logic
├── DatabaseService.cs # All database interaction (CRUD for each module)
├── App.xaml.cs        # Application startup and navigation
```

---

## 💡 Features

- **Ginners Management** – Create, update, delete cotton ginners with profile info.
- **Mills Management** – Track mills with addresses and owner data.
- **Contracts** – Manage contracts between ginners and mills with commission and bale info.
- **Deliveries** – Register and track dispatches, truck info, weights, and dates.
- **Payments** – Record total and paid amounts per contract.
- **Ginner Ledger** – Maintain deal-level payment records with date and mill due tracking.
- **Navigation Panel** – A persistent sidebar for quick page access.
- **Offline Data Storage** – Uses SQLite with local file persistence.

---

## ⚙ Technologies Used

- **.NET MAUI (Multi-platform App UI)**
- **C# with XAML**
- **SQLite for local data**
- **MVVM-like structure (code-behind with logical separation)**

---

## 🧩 How to Run

1. **Clone the repository**

```bash
git clone https://github.com/yourusername/MadinaEnterprises.git
cd MadinaEnterprises
```

2. **Build in Visual Studio 2022 or later**
   - Target: **.NET 7.0 or higher**
   - Platforms: **Windows / Android / iOS / macOS**

3. **Run the app**
   - The app launches with the `LoginPage`.
   - After login, access all modules via the left-side navigation.

---

## 📁 Data Storage

SQLite DB is created at runtime inside:

```
Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
```

The database file is named:

```
madina.db3
```

---

## 📌 Notes

- The app uses hardcoded navigation (`NavigationPage.PushAsync(...)`).
- All data operations are handled asynchronously via `DatabaseService.cs`.
- No cloud sync yet – ideal for offline-first businesses.

---

## 🔐 Authentication

This project uses a **simple hardcoded login** flow (no signup, no email verification, no forgot-password screen).

- Username: `Anees`
- Password: `0000`

You can change these constants in `Modules/Views/LoginPage.xaml.cs` (`HardcodedUsername` and `HardcodedPassword`).

---

## 📣 Contribution

Open to collaboration – feel free to fork and PR enhancements such as:

- Charting & analytics
- Data export (CSV/Excel)
- UI polish & animations
- Dark mode toggle

---

## 📃 License

This project is proprietary and developed for **Madina Enterprises**. Redistribution is subject to owner permission.

---

**Built with ❤️ using .NET MAUI**
