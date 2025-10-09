using MadinaEnterprises.Modules.Models;
using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Views;

public partial class GinnersPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ObservableCollection<Ginners> _ginnersList;

    // cache for searching
    private List<Ginners> _allGinners = new();

    public GinnersPage()
    {
        InitializeComponent();
        _databaseService = App.DatabaseService!;
        _ginnersList = new ObservableCollection<Ginners>();
        GinnersListView.ItemsSource = _ginnersList;
        _ = LoadGinners();
    }

    //****************************************************************************
    //                       NAVIGATION BUTTONS
    //****************************************************************************
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());

    //****************************************************************************
    //                             DATA LOADING
    //****************************************************************************
    private async Task LoadGinners()
    {
        _ginnersList.Clear();
        var ginners = await _databaseService.GetAllGinners();

        _allGinners = ginners.ToList(); // cache for search

        foreach (var ginner in ginners)
            _ginnersList.Add(ginner);
    }

    //****************************************************************************
    //                       GINNER MANAGEMENT
    //****************************************************************************
    private async void OnAddGinnerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GinnerIDEntry.Text) || string.IsNullOrWhiteSpace(GinnerNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Ginner ID and Name are required fields.", "OK");
            return;
        }

        // Check for duplicate GinnerID
        var existingGinners = await _databaseService.GetAllGinners();
        if (existingGinners.Any(g => g.GinnerID == GinnerIDEntry.Text))
        {
            await DisplayAlert("Duplicate GinnerID", "A ginner with this Ginner ID already exists.", "OK");
            return;
        }

        var ginner = new Ginners
        {
            GinnerID = GinnerIDEntry.Text,
            GinnerName = GinnerNameEntry.Text,
            Contact = GinnerContactEntry.Text ?? "",
            Address = GinnerAddressEntry.Text ?? "",
            IBAN = GinnerIBANEntry.Text ?? "",
            NTN = GinnerNTNEntry.Text ?? "",
            STN = GinnerSTNEntry.Text ?? "",
            BankAddress = GinnerBankAddressEntry.Text ?? "",
            ContactPerson = GinnerContactPersonEntry.Text ?? "",
            Station = GinnerStationEntry.Text ?? ""
        };

        await _databaseService.AddGinner(ginner);
        await DisplayAlert("Success", "Ginner profile created successfully.", "OK");
        ClearEntries();
        await LoadGinners();
    }

    private async void OnUpdateGinnerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GinnerIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Ginner ID is required for updating.", "OK");
            return;
        }

        // Ensure GinnerID exists before updating
        var allGinners = await _databaseService.GetAllGinners();
        var existingGinner = allGinners.FirstOrDefault(g => g.GinnerID == GinnerIDEntry.Text);
        if (existingGinner == null)
        {
            await DisplayAlert("Not Found", "No ginner found with this Ginner ID to update.", "OK");
            return;
        }

        var ginner = new Ginners
        {
            GinnerID = GinnerIDEntry.Text,
            GinnerName = GinnerNameEntry.Text,
            Contact = GinnerContactEntry.Text ?? "",
            Address = GinnerAddressEntry.Text ?? "",
            IBAN = GinnerIBANEntry.Text ?? "",
            NTN = GinnerNTNEntry.Text ?? "",
            STN = GinnerSTNEntry.Text ?? "",
            BankAddress = GinnerBankAddressEntry.Text ?? "",
            ContactPerson = GinnerContactPersonEntry.Text ?? "",
            Station = GinnerStationEntry.Text ?? ""
        };

        await _databaseService.UpdateGinner(ginner);
        await DisplayAlert("Success", "Ginner profile updated successfully.", "OK");
        ClearEntries();
        await LoadGinners();
    }

    private async void OnDeleteGinnerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GinnerIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Ginner ID is required for deletion.", "OK");
            return;
        }

        await _databaseService.DeleteGinner(GinnerIDEntry.Text);
        await DisplayAlert("Success", "Ginner profile deleted successfully.", "OK");
        ClearEntries();
        await LoadGinners();
    }

    private void OnGinnerSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is Ginners selectedGinner)
        {
            GinnerIDEntry.Text = selectedGinner.GinnerID;
            GinnerNameEntry.Text = selectedGinner.GinnerName;
            GinnerContactEntry.Text = selectedGinner.Contact;
            GinnerAddressEntry.Text = selectedGinner.Address;
            GinnerIBANEntry.Text = selectedGinner.IBAN;
            GinnerNTNEntry.Text = selectedGinner.NTN;
            GinnerSTNEntry.Text = selectedGinner.STN;
            GinnerBankAddressEntry.Text = selectedGinner.BankAddress;
            GinnerContactPersonEntry.Text = selectedGinner.ContactPerson;
            GinnerStationEntry.Text = selectedGinner.Station;
        }
    }

    private void ClearEntries()
    {
        GinnerIDEntry.Text = "";
        GinnerNameEntry.Text = "";
        GinnerContactEntry.Text = "";
        GinnerAddressEntry.Text = "";
        GinnerIBANEntry.Text = "";
        GinnerNTNEntry.Text = "";
        GinnerSTNEntry.Text = "";
        GinnerBankAddressEntry.Text = "";
        GinnerContactPersonEntry.Text = "";
        GinnerStationEntry.Text = "";
        GinnersListView.SelectedItem = null;
        GinnerSearchBar.Text = string.Empty;
    }

    //****************************************************************************
    //                               SEARCH
    //****************************************************************************
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? string.Empty).Trim().ToLowerInvariant();

        _ginnersList.Clear();
        IEnumerable<Ginners> src = _allGinners;

        if (!string.IsNullOrEmpty(q))
        {
            src = _allGinners.Where(g =>
                (g.GinnerName ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (g.GinnerID ?? string.Empty).ToLowerInvariant().Contains(q));
        }

        foreach (var g in src)
            _ginnersList.Add(g);
    }
}
