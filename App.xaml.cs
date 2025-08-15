using MadinaEnterprises.Modules.Views;
using static System.Net.Mime.MediaTypeNames;

namespace MadinaEnterprises
{
    public partial class App : Application
    {
        // Instance of DatabaseService (guaranteed initialized)
        public static DatabaseService DatabaseService { get; private set; }

        // Shell instance for navigation
        public static AppShell AppShell { get; private set; }

        public App()
        {
            InitializeComponent();

            // Initialize the database service
            DatabaseService = new DatabaseService();

            // Create and set the shell
            AppShell = new AppShell();
            MainPage = AppShell;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        // Navigation helpers using Shell
        public static async Task NavigateToPage(Page page)
        {
            string pageName = page.GetType().Name;
            await Shell.Current.GoToAsync($"//{pageName}");
        }

        public static async Task NavigateToPageByName(string pageName)
        {
            await Shell.Current.GoToAsync($"//{pageName}");
        }

        // Back navigation helper
        public static async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        // Show main app after login
        public static void ShowMainApp()
        {
            AppShell?.ShowMainApp();
        }

        // Show login page
        public static void ShowLogin()
        {
            AppShell?.ShowLogin();
        }
    }
}