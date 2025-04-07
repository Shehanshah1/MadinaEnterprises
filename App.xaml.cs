using MadinaEnterprises.Modules.Views;

namespace MadinaEnterprises
{
    public partial class App : Application
    {
        // Instance of DatabaseService (guaranteed initialized)
        public static DatabaseService DatabaseService { get; private set; } = new();

        public App()
        {
            InitializeComponent();

            // Initialize the database service
            DatabaseService = new DatabaseService();

            // Set the startup page
            MainPage = new NavigationPage(new LoginPage());
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
    }
}
