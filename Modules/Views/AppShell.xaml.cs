using MadinaEnterprises.Modules.Views;

namespace MadinaEnterprises
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(CreateAccountPage), typeof(CreateAccountPage));
            Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
            Routing.RegisterRoute(nameof(GinnersPage), typeof(GinnersPage));
            Routing.RegisterRoute(nameof(MillsPage), typeof(MillsPage));
            Routing.RegisterRoute(nameof(ContractsPage), typeof(ContractsPage));
            Routing.RegisterRoute(nameof(DeliveriesPage), typeof(DeliveriesPage));
            Routing.RegisterRoute(nameof(PaymentsPage), typeof(PaymentsPage));
            Routing.RegisterRoute(nameof(GinnerLedgerPage), typeof(GinnerLedgerPage));
            Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
        }

        public void ShowMainApp()
        {
            MainTabBar.IsVisible = true;
            Shell.Current.GoToAsync("//DashboardPage");
        }

        public void ShowLogin()
        {
            MainTabBar.IsVisible = false;
            Shell.Current.GoToAsync("//LoginPage");
        }
    }
}