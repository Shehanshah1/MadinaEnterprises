namespace MadinaEnterprises.Modules.Views;

public partial class ContractsPage : ContentPage
{
	public ContractsPage()
	{
		InitializeComponent();
	}
    //*****************************************************************************
    //                       NAVIGATION BUTTONS
    //*****************************************************************************
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new DashboardPage());
    }
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new GinnersPage());
    }
    private async void OnMillsPageButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new MillsPage());
    }
    private async void OnContractsPageButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new ContractsPage());
    }
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new DeliveriesPage());
    }
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new PaymentsPage());
    }
    private async void OnLogOutButtonClicked(object sender, EventArgs e)
    {
        // Navigate back to the LoginView
        await App.NavigateToPage(new LoginPage());
    }
}