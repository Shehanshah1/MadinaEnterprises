using MadinaEnterprises.Modules.Models;
using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Views;

public partial class GinnersPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ObservableCollection<Ginners> _ginnersList;

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
    //                       GINNER MANAGEMENT
    //****************************************************************************

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
            GinnerName = GinnerNameEntry.Text,
            Contact = GinnerContactEntry.Text,
            Address = GinnerAddressEntry.Text,
            IBAN = GinnerIBANEntry.Text,
            NTN = GinnerNTNEntry.Text,
            STN = GinnerSTNEntry.Text
        };
        await _databaseService.AddGinner(ginner);
        await DisplayAlert("Success", "Ginner profile created successfully.", "OK");
        ClearEntries();
        await LoadGinners();
    }

    private async Task LoadGinners()
    {
        _ginnersList.Clear();
        var ginners = await _databaseService.GetAllGinners();

        foreach (var ginner in ginners)
        {
            _ginnersList.Add(ginner);
        }
    }

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
            GinnerName = GinnerNameEntry.Text,
            Contact = GinnerContactEntry.Text,
            Address = GinnerAddressEntry.Text,
            IBAN = GinnerIBANEntry.Text,
            NTN = GinnerNTNEntry.Text,
            STN = GinnerSTNEntry.Text
        };

       await _databaseService.UpdateGinner(ginner);
       await DisplayAlert("Success", "Ginner profile created successfully.", "OK");
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
        await DisplayAlert("Success", "Ginner profile created successfully.", "OK");
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
        }
    }

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
