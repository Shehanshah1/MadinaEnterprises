using MadinaEnterprises.Modules.Models;
using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Views;

public partial class GinnersPage : ContentPage
{
    // DatabaseService instance for handling database operations
    private readonly DatabaseService _databaseService;
    // Observable collection for displaying the list of Ginners in the UI
    private readonly ObservableCollection<Ginners> _ginnersList;

    public GinnersPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _ginnersList = new ObservableCollection<Ginners>();
        // Binding the ListView to the observable collection
        GinnersListView.ItemsSource = _ginnersList;
        LoadGinners(); // Load ginners when the page is initialized
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
        await App.NavigateToPage(new LoginPage());
    }
    private async void OnLedgerButtonClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new GinnerLedgerPage());
    }

    //*****************************************************************************
    //                       GINNER MANAGEMENT
    //*****************************************************************************


    // Event handler for adding a new ginner
    private async void OnAddGinnerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GinnerIDEntry.Text) || string.IsNullOrWhiteSpace(GinnerNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Ginner ID and Name are required fields.", "OK");
            return;
        }

        var ginner = new Ginners
        {
            GinnerID = GinnerIDEntry.Text,
            Name = GinnerNameEntry.Text,
            Contact = GinnerContactEntry.Text,
            Address = GinnerAddressEntry.Text,
            IBAN = GinnerIBANEntry.Text,
            NTN = GinnerNTNEntry.Text,
            STN = GinnerSTNEntry.Text
        };

        bool isAdded = _databaseService.AddGinner(ginner);

        if (isAdded)
        {
            await DisplayAlert("Success", "Ginner added successfully.", "OK");
            ClearEntries(); // Clear input fields
            LoadGinners(); // Refresh the list of ginners
        }
        else
        {
            await DisplayAlert("Error", "Failed to add Ginner. It may already exist.", "OK");
        }
    }

    // Event handler for loading all ginners into the list
    private void LoadGinners()
    {
        _ginnersList.Clear();
        var ginners = _databaseService.GetAllGinners();

        foreach (var ginner in ginners)
        {
            _ginnersList.Add(ginner);
        }
    }

    // Event handler for updating an existing ginner
    private async void OnUpdateGinnerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GinnerIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Ginner ID is required for updating.", "OK");
            return;
        }

        var ginner = new Ginners
        {
            GinnerID = GinnerIDEntry.Text,
            Name = GinnerNameEntry.Text,
            Contact = GinnerContactEntry.Text,
            Address = GinnerAddressEntry.Text,
            IBAN = GinnerIBANEntry.Text,
            NTN = GinnerNTNEntry.Text,
            STN = GinnerSTNEntry.Text
        };

        bool isUpdated = _databaseService.UpdateGinner(ginner);

        if (isUpdated)
        {
            await DisplayAlert("Success", "Ginner updated successfully.", "OK");
            ClearEntries();
            LoadGinners();
        }
        else
        {
            await DisplayAlert("Error", "Failed to update Ginner. Ginner not found.", "OK");
        }
    }

    // Event handler for deleting a ginner
    private async void OnDeleteGinnerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GinnerIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Ginner ID is required for deletion.", "OK");
            return;
        }

        bool isDeleted = _databaseService.DeleteGinner(GinnerIDEntry.Text);

        if (isDeleted)
        {
            await DisplayAlert("Success", "Ginner deleted successfully.", "OK");
            ClearEntries();
            LoadGinners();
        }
        else
        {
            await DisplayAlert("Error", "Failed to delete Ginner. Ginner not found.", "OK");
        }
    }

    // Clear the input fields
    private void ClearEntries()
    {
        GinnerIDEntry.Text = string.Empty;
        GinnerNameEntry.Text = string.Empty;
        GinnerContactEntry.Text = string.Empty;
        GinnerAddressEntry.Text = string.Empty;
        GinnerIBANEntry.Text = string.Empty;
        GinnerNTNEntry.Text = string.Empty;
        GinnerSTNEntry.Text = string.Empty;
    }
}
