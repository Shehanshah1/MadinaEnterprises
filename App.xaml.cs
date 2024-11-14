using MadinaEnterprises.Modules.Views;
namespace MadinaEnterprises

{
    public partial class App : Application
    {
        // Instance of DatabaseService
        public static DatabaseService DatabaseService { get; private set; }

        public App()
        {
            InitializeComponent();

            // Initialize the database service
            DatabaseService = new DatabaseService();


            // Setting up the initial page to LoginView using a NavigationPage
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




        // A helper method to simplify navigation
        public static async Task NavigateToPage(Page page)
        {
            if (Current.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PushAsync(page);
            }
        }
        // A helper method for navigating back
        public static async Task GoBack()
        {
            if (Current.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PopAsync();
            }
        }
    }
}
