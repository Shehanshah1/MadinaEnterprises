# Deploying Madina Enterprises & Shipping Updates

This doc explains how to get the Windows build into users' hands and — more
importantly — how to push future updates without asking every user to re-run
`Install.ps1`.

---

## TL;DR

- The `.msix` file you already have is a sealed, signed package. You cannot
  "git pull" or "patch" it. Every update is a **new MSIX with a bumped
  version**.
- To get automatic updates on sideloaded installs, ship an
  **App Installer (`.appinstaller`) file** alongside the MSIX and host both
  at a stable HTTPS URL. Windows polls that URL and updates the app silently.
- For anything larger than a handful of users, consider the **Microsoft Store**
  (or Microsoft Intune if you manage company PCs). Both give you updates for
  free.

---

## 1. One-time: prepare a hosting location

Pick any HTTPS location that serves files with their correct MIME types:

| Host | Good for | Notes |
|---|---|---|
| **GitHub Releases** | Free, simple | Public repo; drop `.msix` + `.appinstaller` as release assets |
| **Azure Blob Storage (static website)** | Private/commercial | Cheap, fast |
| **Amazon S3 + CloudFront** | Private/commercial | Same |
| **IIS / Nginx on your own server** | On-prem | Make sure `.msix` and `.appinstaller` MIME types are registered |
| **SharePoint / OneDrive for Business** | Internal only | Use the "Direct download" URL form |

Whatever you choose, you need a stable base URL, e.g.
`https://downloads.madinaenterprises.com/app/`.

> ⚠️ The URL **must be HTTPS** and must not change between releases — users'
> installs remember it.

---

## 2. One-time: code signing

MSIX packages must be signed. Options:

1. **Self-signed cert** (`CN=Madina Enterprises`, matching
   `Package.appxmanifest`). Users must install the `.cer` once as a Trusted
   Root — this is what your current `Install.ps1` does. The App Installer
   auto-update flow still works after that first trust.
2. **Code-signing cert from a public CA** (DigiCert, Sectigo, etc., ~$200/yr).
   No user-side cert install required. Strongly recommended for external
   customers.

Put the cert's thumbprint somewhere you can pass to the build script (Windows
Credential Manager, CI secret, etc.).

---

## 3. Cutting a release

On a Windows machine with the .NET MAUI + Windows SDK workloads installed:

```powershell
# from the repo root
./scripts/Publish-MsixRelease.ps1 `
    -Version 1.0.1.0 `
    -BaseUri "https://downloads.madinaenterprises.com/app/" `
    -CertThumbprint "<your-cert-thumbprint>"
```

The script:

1. Stamps the version into `Platforms/Windows/Package.appxmanifest`.
2. Runs `dotnet publish` to produce a signed `MadinaEnterprises_<ver>_x64.msix`.
3. Generates a matching `MadinaEnterprises.appinstaller` pointing at your
   hosting URL.
4. Drops both into `./publish/`.

Upload the entire contents of `./publish/` to your hosting base URL.

> The **first** release version must match the `.appinstaller`'s MainPackage
> version. Every subsequent release bumps both the `AppInstaller` `Version`
> attribute **and** `MainPackage` `Version`. The script does this for you.

---

## 4. How end users install (once)

Send them this URL (not the `.msix` directly):

```
https://downloads.madinaenterprises.com/app/MadinaEnterprises.appinstaller
```

Windows opens the App Installer UI → they click **Install** → done. From now
on, Windows owns the update lifecycle for this app.

If you're using a self-signed cert, they still have to import the `.cer` into
`Trusted People` or `Trusted Root Certification Authorities` the first time
(your existing `Install.ps1` already does this). After that, updates work
without cert prompts because new versions are signed by the same cert.

---

## 5. How updates reach users

With the `.appinstaller` in place, `Platforms/Windows/MadinaEnterprises.appinstaller`
is configured with:

```xml
<OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="false" />
<AutomaticBackgroundTask />
<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>
```

That means:

- **Every launch**, Windows fetches the `.appinstaller` in the background.
- If a newer MainPackage version is listed, Windows downloads and stages it.
- The update applies on the **next** launch (the current session is not
  interrupted — `UpdateBlocksActivation="false"`).
- `ForceUpdateFromAnyVersion` lets you skip versions (e.g. 1.0.0 → 1.2.0
  directly).

Flip `UpdateBlocksActivation` to `true` if you'd rather force users to take
critical updates immediately.

### Why not git / patching?

An MSIX is a cryptographically signed ZIP of your compiled binaries plus a
manifest. Windows verifies the signature and contents on install. You can't
modify one in place — any change breaks the signature, and Windows refuses
to load it. That's by design and is the reason MSIX is considered safe. So
"updates" always mean "ship a new MSIX, Windows replaces the old one
atomically."

---

## 6. Alternative: Microsoft Store

If your userbase is wider than a few machines, publish through the Microsoft
Store. You upload the same MSIX, Microsoft signs and distributes it, and
updates are 100% automatic without you hosting anything. Cost: a one-time
developer account fee.

---

## 7. Alternative: Intune / MDM

For company-managed PCs, push the MSIX through Intune. IT sets an update
policy; users get updates during normal device sync. This is the standard
enterprise route.

---

## 8. Troubleshooting

| Symptom | Likely cause |
|---|---|
| "App installation failed with error message: The signature is invalid" | MSIX signed with a different cert than last time, or cert not trusted on the machine. |
| Updates never arrive | `.appinstaller` URL is not HTTPS, returns wrong MIME type, or `MainPackage Version` wasn't bumped. |
| "This app package's publisher certificate could not be verified" | Self-signed cert isn't in Trusted Root / Trusted People on that machine. Run `Install.ps1` once. |
| Update check fails silently | Check Event Viewer → `Applications and Services Logs → Microsoft → Windows → AppXDeploymentServer`. |

---

## Files in this repo that support deployment

- `Platforms/Windows/Package.appxmanifest` — app identity & version
- `Platforms/Windows/MadinaEnterprises.appinstaller` — update manifest template
- `scripts/Publish-MsixRelease.ps1` — one-shot build/sign/stage script
