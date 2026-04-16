using MadinaEnterprises.Modules.Models;
using MadinaEnterprises.Modules.Util;
using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Views;

public partial class GinnerLedgerPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;

    private List<Ginners> _allGinners = new();
    private Ginners? _selectedGinner;

    private ObservableCollection<Contracts> _contractsView = new();
    private ObservableCollection<Deliveries> _deliveriesView = new();
    private ObservableCollection<Payment> _paymentsView = new();

    // Unselected / selected tab button colors.
    private static readonly Color TabActiveBg = Color.FromArgb("#8AA800");
    private static readonly Color TabActiveFg = Colors.White;
    private static readonly Color TabInactiveBg = Color.FromArgb("#FFFFFF");
    private static readonly Color TabInactiveFg = Color.FromArgb("#334155");

    public GinnerLedgerPage()
    {
        InitializeComponent();
        _ = LoadGinners();
    }

    // ======================== Ginner selection ========================

    private async Task LoadGinners()
    {
        try
        {
            _allGinners = await _db.GetAllGinners();
            ginnerListView.ItemsSource = new ObservableCollection<Ginners>(_allGinners);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load ginners: {ex.Message}", "OK");
        }
    }

    private void OnGinnerSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = (e.NewTextValue ?? "").ToLowerInvariant();
        IEnumerable<Ginners> filtered = _allGinners;
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = _allGinners.Where(g =>
                (g.GinnerName ?? "").ToLowerInvariant().Contains(query) ||
                (g.GinnerID ?? "").ToLowerInvariant().Contains(query) ||
                (g.Contact ?? "").ToLowerInvariant().Contains(query) ||
                (g.Station ?? "").ToLowerInvariant().Contains(query));
        }
        ginnerListView.ItemsSource = new ObservableCollection<Ginners>(filtered);
    }

    private async void OnGinnerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Ginners g) return;

        _selectedGinner = g;
        ginnerListView.SelectedItem = null; // clear selection so it can be re-selected later

        GinnerNameHeader.Text = $"{g.GinnerName}  ({g.GinnerID})";
        GinnerInfoHeader.Text = $"Contact: {g.Contact}   |   Station: {g.Station}   |   NTN: {g.NTN}";

        SelectionView.IsVisible = false;
        DetailView.IsVisible = true;

        await LoadGinnerData();
        ShowContractsTab();
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        _selectedGinner = null;
        DetailView.IsVisible = false;
        SelectionView.IsVisible = true;
    }

    // ======================== Data load ========================

    private async Task LoadGinnerData()
    {
        if (_selectedGinner == null) return;

        try
        {
            var allContracts = await _db.GetAllContracts();
            var contracts = allContracts.Where(c => c.GinnerID == _selectedGinner.GinnerID).ToList();

            var contractIds = contracts.Select(c => c.ContractID).ToHashSet();

            var allDeliveries = await _db.GetAllDeliveries();
            var deliveries = allDeliveries.Where(d => contractIds.Contains(d.ContractID)).ToList();

            var allPayments = await _db.GetAllPayments();
            var payments = allPayments.Where(p => contractIds.Contains(p.ContractID)).ToList();

            _contractsView = new ObservableCollection<Contracts>(contracts);
            _deliveriesView = new ObservableCollection<Deliveries>(deliveries);
            _paymentsView = new ObservableCollection<Payment>(payments);

            BindableLayout.SetItemsSource(ContractsGrid, _contractsView);
            BindableLayout.SetItemsSource(DeliveriesGrid, _deliveriesView);
            BindableLayout.SetItemsSource(PaymentsGrid, _paymentsView);

            ContractsEmptyLabel.IsVisible = contracts.Count == 0;
            DeliveriesEmptyLabel.IsVisible = deliveries.Count == 0;
            PaymentsEmptyLabel.IsVisible = payments.Count == 0;

            UpdateDashboard(contracts, deliveries, payments);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load ginner data: {ex.Message}", "OK");
        }
    }

    private void UpdateDashboard(List<Contracts> contracts, List<Deliveries> deliveries, List<Payment> payments)
    {
        double totalDealAmount = 0;
        double totalCommission = 0;
        foreach (var c in contracts)
        {
            var millWeightKg = deliveries.Where(d => d.ContractID == c.ContractID).Sum(d => d.MillWeight);
            var actualAmount = RateCalculation.AmountFromKg(millWeightKg, c.PricePerBatch);
            totalDealAmount += actualAmount;
            totalCommission += actualAmount * (c.CommissionPercentage / 100);
        }

        double totalPaid = payments.Sum(p => p.AmountPaid);
        double totalDue = payments.Sum(p => p.TotalAmount - p.AmountPaid);
        int totalBales = contracts.Sum(c => c.TotalBales);
        double avgCommission = contracts.Any() ? contracts.Average(c => c.CommissionPercentage) : 0;

        KpiContractsLabel.Text = contracts.Count.ToString();
        KpiBalesLabel.Text = totalBales.ToString();
        KpiDealAmountLabel.Text = $"Rs.{totalDealAmount:N2}";
        KpiCommissionLabel.Text = $"Rs.{totalCommission:N2}";
        KpiPaidLabel.Text = $"Rs.{totalPaid:N2}";
        KpiDueLabel.Text = $"Rs.{totalDue:N2}";
        KpiDeliveriesLabel.Text = deliveries.Count.ToString();
        KpiAvgCommissionLabel.Text = $"{avgCommission:F2}%";
    }

    private async void OnReloadClicked(object sender, EventArgs e)
    {
        await LoadGinnerData();
    }

    // ======================== Tab switching ========================

    private void ShowContractsTab()  => SelectTab(ContractsTab,  TabContractsBtn);
    private void OnTabContractsClicked(object sender, EventArgs e)  => SelectTab(ContractsTab,  TabContractsBtn);
    private void OnTabDashboardClicked(object sender, EventArgs e)  => SelectTab(DashboardTab,  TabDashboardBtn);
    private void OnTabDeliveriesClicked(object sender, EventArgs e) => SelectTab(DeliveriesTab, TabDeliveriesBtn);
    private void OnTabPaymentsClicked(object sender, EventArgs e)   => SelectTab(PaymentsTab,   TabPaymentsBtn);

    private void SelectTab(View tabContent, Button tabButton)
    {
        ContractsTab.IsVisible = tabContent == ContractsTab;
        DashboardTab.IsVisible = tabContent == DashboardTab;
        DeliveriesTab.IsVisible = tabContent == DeliveriesTab;
        PaymentsTab.IsVisible = tabContent == PaymentsTab;

        foreach (var btn in new[] { TabContractsBtn, TabDashboardBtn, TabDeliveriesBtn, TabPaymentsBtn })
        {
            bool active = btn == tabButton;
            btn.BackgroundColor = active ? TabActiveBg : TabInactiveBg;
            btn.TextColor = active ? TabActiveFg : TabInactiveFg;
        }
    }

    // ======================== Save handlers ========================

    private async void OnSaveContractsClicked(object sender, EventArgs e)
    {
        if (_contractsView.Count == 0)
        {
            await DisplayAlert("Nothing to save", "There are no contracts to save.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Confirm", $"Save changes to {_contractsView.Count} contract(s)?", "Yes", "No");
        if (!confirm) return;

        int ok = 0, fail = 0;
        foreach (var c in _contractsView)
        {
            try
            {
                await _db.UpdateContract(c);
                ok++;
            }
            catch
            {
                fail++;
            }
        }

        await DisplayAlert("Saved", $"Contracts saved: {ok}. Failed: {fail}.", "OK");
        await LoadGinnerData();
    }

    private async void OnSaveDeliveriesClicked(object sender, EventArgs e)
    {
        if (_deliveriesView.Count == 0)
        {
            await DisplayAlert("Nothing to save", "There are no deliveries to save.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Confirm", $"Save changes to {_deliveriesView.Count} delivery record(s)?", "Yes", "No");
        if (!confirm) return;

        int ok = 0, fail = 0;
        foreach (var d in _deliveriesView)
        {
            try
            {
                await _db.UpdateDelivery(d);
                ok++;
            }
            catch
            {
                fail++;
            }
        }

        await DisplayAlert("Saved", $"Deliveries saved: {ok}. Failed: {fail}.", "OK");
        await LoadGinnerData();
    }

    private async void OnSavePaymentsClicked(object sender, EventArgs e)
    {
        if (_paymentsView.Count == 0)
        {
            await DisplayAlert("Nothing to save", "There are no payments to save.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Confirm", $"Save changes to {_paymentsView.Count} payment record(s)?", "Yes", "No");
        if (!confirm) return;

        int ok = 0, fail = 0;
        foreach (var p in _paymentsView)
        {
            try
            {
                await _db.UpdatePayment(p);
                ok++;
            }
            catch
            {
                fail++;
            }
        }

        await DisplayAlert("Saved", $"Payments saved: {ok}. Failed: {fail}.", "OK");
        await LoadGinnerData();
    }

    // ======================== Navigation ========================

    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private void OnLogOutButtonClicked(object sender, EventArgs e) => App.Logout();
}
