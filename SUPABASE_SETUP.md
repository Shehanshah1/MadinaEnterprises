# Cloud Sync Setup (Supabase)

Madina Enterprises now syncs its local SQLite database to [Supabase](https://supabase.com) so your Ginners, Mills, Contracts, Deliveries, and Payments data survives reinstalls and is shared between the machines you run the app on.

## One-time Supabase setup

1. Go to <https://supabase.com> and create a free account.
2. Create a new project. Pick any name and a strong database password (you won't need it for the app).
3. Wait for the project to finish provisioning (~1 minute).
4. Open **SQL Editor** in the left nav, click **New query**, paste the contents of [`Resources/Raw/supabase_schema.sql`](Resources/Raw/supabase_schema.sql) into the editor, and click **Run**. This creates the 5 tables the app uses and enables the RLS policies that let the app read/write.
5. Open **Project Settings → API** and copy:
   - **Project URL** (looks like `https://abcdef.supabase.co`)
   - **anon public** key (a long JWT starting with `eyJ...`)

## Connect the app to your project

You have two options. Both work; pick whichever is more convenient.

### Option A — Enter credentials inside the app (recommended)

1. Launch the app and log in.
2. From the Dashboard sidebar, click **Cloud Sync**.
3. Paste the Project URL and Anon Key, click **Save Credentials**.
4. Click **Pull from Cloud** to load any data that's already up there, or **Push to Cloud** to upload what you have locally.

The values are saved to `%LOCALAPPDATA%\madina\cloudsync.json` on your machine, so each device can point at the same (or different) Supabase project.

### Option B — Ship the app with credentials baked in

Edit `Resources/Raw/cloudsync.json` before building:

```json
{
  "url": "https://your-project.supabase.co",
  "anonKey": "eyJhbGciOi..."
}
```

These values are only used if the local override in option A is not present.

### Option C — Environment variables

If both of the above are empty, the app will read `MADINA_SUPABASE_URL` and `MADINA_SUPABASE_ANON_KEY` from the environment.

## How sync behaves

- **On app start** — if credentials are configured, the app pulls every row from Supabase and merges it into local SQLite (cloud wins on conflicts), then pushes any rows that only exist locally.
- **On every Add / Update / Delete** — the change is fire-and-forget pushed to Supabase immediately. If you're offline the write still succeeds locally; it'll be re-pushed next time you hit **Push to Cloud** or put the app to sleep and resume.
- **On sleep / resume** — the app pushes pending local changes and pulls remote changes so you switch devices seamlessly.
- **Conflict resolution** — last-write-wins (merge-duplicates). If two devices edit the same record while offline, whichever syncs last is kept.

## Troubleshooting

- Open **Cloud Sync** in the app — the status panel shows whether the connection is configured, when the last sync happened, and the last error message (if any).
- If a pull fails with a `401`/`403` error, re-check your anon key and make sure you ran the RLS policy block at the bottom of `supabase_schema.sql`.
- If a push fails with a schema error, re-run `supabase_schema.sql` — the column names must be exactly the snake_case names listed there.
- The local database file lives at `%LOCALAPPDATA%\madina.db3` on Windows. If it ever gets blown away again, just launch the app and it will pull everything back from Supabase.
