namespace MadinaEnterprises.Modules.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        LoadDashboardData();
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

    //*****************************************************************************
    //                       LOAD DASHBOARD DATA
    //*****************************************************************************
    private void LoadDashboardData()
    {
        try
        {
            // Fetch data from the database
            decimal totalCommission = GetTotalCommission();
            decimal paymentDue = GetPaymentDue();
            decimal paymentMade = GetPaymentMade();
            int balesSold = GetBalesSold();
            int totalGinners = GetTotalGinners();

            // Update the labels in the UI
            TotalCommissionLabel.Text = $"${totalCommission:F2}";
            PaymentDueLabel.Text = $"${paymentDue:F2}";
            PaymentMadeLabel.Text = $"${paymentMade:F2}";
            BalesSoldLabel.Text = balesSold.ToString();
            TotalGinnersLabel.Text = totalGinners.ToString();
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during data loading
            DisplayAlert("Error", $"Failed to load dashboard data: {ex.Message}", "OK");
        }
    }

    //*****************************************************************************
    //                       DATABASE METHODS
    //*****************************************************************************
    private decimal GetTotalCommission()
    {
        // Replace with your database logic
        // Example: return database.CalculateTotalCommission();
        return 10500.75M; // Placeholder value
    }

    private decimal GetPaymentDue()
    {
        // Replace with your database logic
        // Example: return database.CalculatePaymentDue();
        return 2500.50M; // Placeholder value
    }

    private decimal GetPaymentMade()
    {
        // Replace with your database logic
        // Example: return database.CalculatePaymentMade();
        return 8000.00M; // Placeholder value
    }

    private int GetBalesSold()
    {
        // Replace with your database logic
        // Example: return database.GetTotalBalesSold();
        return 350; // Placeholder value
    }

    private int GetTotalGinners()
    {
        // Replace with your database logic
        // Example: return database.GetTotalGinnersCount();
        return 30; // Placeholder value
    }
}
