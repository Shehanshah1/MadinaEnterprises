# Deploying Madina Enterprises & Shipping Updates

This project ships the Windows build via **GitHub Releases** with a
**self-signed internal code-signing certificate**. Once a user installs the
app from the `.appinstaller` URL, Windows handles every future update
automatically — no `Install.ps1` per version.

---

## TL;DR

1. One-time: generate a signing cert (`scripts/New-SigningCertificate.ps1`).
2. One-time: add two GitHub secrets (`SIGNING_CERT_PFX_BASE64`, `SIGNING_CERT_PASSWORD`).
3. Every release: push a tag like `v1.0.1.0`. GitHub Actions builds, signs,
   and publishes automatically.
4. Users install once from the **stable** URL below; updates arrive on launch.

User-facing install URL (never changes):
```
https://github.com/Shehanshah1/MadinaEnterprises/releases/latest/download/MadinaEnterprises.appinstaller
```

---

## 1. One-time setup

### 1a. Generate the signing certificate (Windows, admin PowerShell)

```powershell
cd <repo root>
./scripts/New-SigningCertificate.ps1 -Password (Read-Host -AsSecureString "PFX password")
```

This creates (in the git-ignored `certs/` folder):

- `MadinaEnterprises.pfx` — private key. **Never commit. Never share.**
- `MadinaEnterprises.cer` — public cert. Safe to share; users install it to
  trust your signature.

It also installs the cert into your `CurrentUser\My` store so you can sign
builds locally with `-CertThumbprint`.

> The cert subject is `CN=Madina Enterprises`, matching `Publisher` in
> `Platforms/Windows/Package.appxmanifest`. If you ever change one, change
> both — they must match byte-for-byte or the install fails.

### 1b. Add the cert to GitHub secrets

Encode the PFX:

```powershell
$b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("./certs/MadinaEnterprises.pfx"))
$b64 | Set-Clipboard
```

In GitHub → **Settings → Secrets and variables → Actions → New repository secret**:

| Secret name | Value |
|---|---|
| `SIGNING_CERT_PFX_BASE64` | the clipboard contents from above |
| `SIGNING_CERT_PASSWORD`   | the PFX password you chose |

That's it — the workflow has everything it needs.

---

## 2. Cutting a release

```bash
git tag v1.0.1.0
git push origin v1.0.1.0
```

The `.github/workflows/release.yml` workflow then:

1. Installs the MAUI workload on a `windows-latest` runner.
2. Decodes the PFX from the secret.
3. Runs `scripts/Publish-MsixRelease.ps1`, which stamps the version into
   `Package.appxmanifest` and the `.appinstaller`, builds the MSIX, and
   signs it.
4. Creates a GitHub Release named `Madina Enterprises 1.0.1.0` and attaches:
   - `MadinaEnterprises_1.0.1.0_x64.msix`
   - `MadinaEnterprises.appinstaller`
   - `MadinaEnterprises.cer`
5. Marks the release as **Latest**, so `releases/latest/download/...` URLs
   resolve to it.

> Version rule: four-part, **strictly increasing**. Never reuse a number —
> Windows caches by version and will refuse to "re-update" to one it's seen.

### Manual runs

You can also trigger the workflow from the **Actions** tab
(`workflow_dispatch`), supplying a version. Useful for re-releasing if an
upload failed partway.

### Local-only builds (e.g. for testing before tagging)

```powershell
./scripts/Publish-MsixRelease.ps1 `
    -Version 1.0.1.0 `
    -GitHubRepo "Shehanshah1/MadinaEnterprises" `
    -CertThumbprint "<your-thumbprint>"
```

Outputs to `./publish/`. Nothing is uploaded.

---

## 3. First-time user install

Send users three things:

1. The public cert: `MadinaEnterprises.cer` (attached to every release).
2. A one-time command (right-click → Run as administrator):

   ```powershell
   Import-Certificate -FilePath .\MadinaEnterprises.cer `
                      -CertStoreLocation Cert:\LocalMachine\TrustedPeople
   ```

   This tells Windows to trust your self-signed cert. Only needed **once per
   machine, ever** — it covers all current and future versions signed by the
   same cert.

3. The install link:

   ```
   https://github.com/Shehanshah1/MadinaEnterprises/releases/latest/download/MadinaEnterprises.appinstaller
   ```

   Open it in a browser → Windows App Installer UI → **Install**.

From now on the user's machine auto-updates on app launch.

---

## 4. How updates reach users

With `Platforms/Windows/MadinaEnterprises.appinstaller` configured as:

```xml
<OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="false" />
<AutomaticBackgroundTask />
<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>
```

- Each launch, Windows silently hits the `/releases/latest/download/MadinaEnterprises.appinstaller` URL.
- If the version there is higher than what's installed, Windows downloads
  the new MSIX in the background.
- The new version is active on the **next** launch — the current session is
  not interrupted.
- `ForceUpdateFromAnyVersion="true"` means a user on 1.0.0 can jump straight
  to 1.5.0; they don't have to pass through intermediate versions.

If you ever need to force immediate updates (e.g. critical fix), change
`UpdateBlocksActivation` to `true` in the template and ship a new release.

---

## 5. Migrating existing users off the old `Install.ps1` install

Users who installed a previous build the manual way have **no update source
registered**. They'll keep running the old version forever unless you
migrate them. Two options:

- **Easiest:** have each user uninstall the app (Start menu → right-click →
  Uninstall), then open the `.appinstaller` URL above. After that they're on
  the auto-update track.
- **No reinstall needed:** skip — any v1.0.1.0+ MSIX they install through
  the `.appinstaller` URL will register the update source automatically.

---

## 6. Why not git / why not patch?

An MSIX is a signed, sealed archive. Modifying even one byte invalidates the
signature, and Windows refuses to load unsigned or tampered packages — this
is a security feature of the platform, not a limitation you can engineer
around. Every update is always a new MSIX with a higher version. The good
news: Windows uses **block-map diffing**, so even though you ship a full new
MSIX, only the changed blocks are transferred over the wire.

---

## 7. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `The signature is invalid` on install | User hasn't trusted the `.cer` yet (see §3), or you re-generated the cert. Re-run the `Import-Certificate` command. |
| Workflow fails on `dotnet workload install` | Runner image churn. Re-run the workflow; Microsoft occasionally breaks workloads transiently. |
| Updates never arrive on user machines | The new GitHub Release wasn't marked "Latest", or version wasn't bumped. Check the release page and `Package.appxmanifest`. |
| `releases/latest/download/...` 404s | No release is marked Latest. In the workflow, `make_latest: 'true'` handles this; check the release page if you triggered manually. |
| `This app package's publisher certificate could not be verified` | The cert was rotated. Ship the new `.cer` and have users import it once. |

Windows logs every App Installer update attempt to
`Event Viewer → Applications and Services Logs → Microsoft → Windows → AppXDeploymentServer`.
That's the first place to look when an update silently doesn't arrive.

---

## Files involved

| Path | Purpose |
|---|---|
| `Platforms/Windows/Package.appxmanifest` | App identity & version |
| `Platforms/Windows/MadinaEnterprises.appinstaller` | Update manifest template |
| `scripts/New-SigningCertificate.ps1` | One-time cert generation |
| `scripts/Publish-MsixRelease.ps1` | Build + sign + stage a release |
| `.github/workflows/release.yml` | CI: runs the above on tag push |
