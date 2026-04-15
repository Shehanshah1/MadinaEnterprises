using MadinaEnterprises.Modules.Services;
using MadinaEnterprises.Modules.Views;

namespace MadinaEnterprises
{
    public partial class App : Application
    {
        public static DatabaseService DatabaseService { get; private set; } = null!;

        // Task that resolves once the initial cloud pull finishes (or is skipped).
        // Pages can `await App.InitialSyncTask` if they need the freshest data.
        public static Task InitialSyncTask { get; private set; } = Task.CompletedTask;

        public App()
        {
            InitializeComponent();

            DatabaseService = new DatabaseService();

            // Kick off Supabase config load + initial pull without blocking the UI thread.
            InitialSyncTask = InitializeCloudSyncAsync();

            // Set the startup page
            MainPage = new NavigationPage(new LoginPage());
        }

        private static async Task InitializeCloudSyncAsync()
        {
            try
            {
                await CloudConfig.LoadAsync();
                if (CloudConfig.IsConfigured)
                {
                    await DatabaseService.SyncFromCloudAsync();
                }
            }
            catch
            {
                // Sync should never crash the app. Errors are surfaced in the UI via
                // DatabaseService.Cloud.LastError.
            }
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Flush any outstanding local changes to the cloud so the next device
            // sees them. Fire-and-forget so sleep isn't blocked.
            if (DatabaseService?.Cloud.IsEnabled == true)
            {
                _ = DatabaseService.PushAllToCloudAsync();
            }
        }

        protected override void OnResume()
        {
            // Pull on resume so the app refreshes with edits made on other devices.
            if (DatabaseService?.Cloud.IsEnabled == true)
            {
                _ = DatabaseService.SyncFromCloudAsync();
            }
        }

        // Navigation helper
        public static async Task NavigateToPage(Page page)
        {
            if (Current?.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PushAsync(page);
            }
        }

        // Back navigation helper
        public static async Task GoBack()
        {
            if (Current?.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PopAsync();
            }
        }

        // Logout: reset navigation stack to LoginPage
        public static void Logout()
        {
            if (Current != null)
            {
                Current.MainPage = new NavigationPage(new LoginPage());
            }
        }
    }
}
