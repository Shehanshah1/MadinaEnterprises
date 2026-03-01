using MadinaEnterprises.Modules.Models;
using Microsoft.Maui.Controls;
using MadinaEnterprises.Modules.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Views;

public partial class PaymentsPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private List<Contracts> contracts = new();
    private List<Payment> payments = new();
    private List<Deliveries> deliveries = new();
    private ObservableCollection<Payment> filteredPayments = new();
    private bool _isSettingSuggestedTotal;
    private bool _isTotalAmountManuallyEdited;

    public PaymentsPage()
    {
        InitializeComponent();
        _ = LoadData();
    }

    private async Task LoadData()
    {
        contracts = await _db.GetAllContracts();
        payments = await _db.GetAllPayments();
        deliveries = await _db.GetAllDeliveries();

        contractPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();
        paymentPicker.ItemsSource = payments.Select(p => p.PaymentID).ToList();

        filteredPayments = new ObservableCollection<Payment>(payments);
        paymentListView.ItemsSource = filteredPayments;
    }

    private void PopulateForm(Payment p)
    {
        paymentIDEntry.Text = p.PaymentID;
        contractPicker.SelectedItem = p.ContractID;
        _isSettingSuggestedTotal = true;
        totalAmountEntry.Text = p.TotalAmount.ToString("F2");
        _isSettingSuggestedTotal = false;
        _isTotalAmountManuallyEdited = true;
        paidAmountEntry.Text = p.AmountPaid.ToString("F2");
        RecalculateRemainingAmount();
        balesEntry.Text = p.TotalBales.ToString();
        paymentDatePicker.Date = p.Date;
    }

    private void ClearForm()
    {
        paymentIDEntry.Text = "";
        contractPicker.SelectedIndex = -1;
        totalAmountEntry.Text = "";
        paidAmountEntry.Text = "";
        remainingAmountEntry.Text = "";
        balesEntry.Text = "";
        paymentDatePicker.Date = DateTime.Today;
        paymentPicker.SelectedItem = null;
        paymentListView.SelectedItem = null;
        _isTotalAmountManuallyEdited = false;
    }

    private void OnContractChanged(object sender, EventArgs e)
    {
        _isTotalAmountManuallyEdited = false;
        RecalculateTotalAmountForSelectedContract();
        RecalculateRemainingAmount();
    }

    private void OnTotalAmountChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isSettingSuggestedTotal)
        {
            _isTotalAmountManuallyEdited = true;
        }

        RecalculateRemainingAmount();
    }

    private void OnPaymentSelected(object sender, EventArgs e)
    {
        if (paymentPicker.SelectedItem is string id)
        {
            var selected = payments.FirstOrDefault(p => p.PaymentID == id);
            if (selected != null)
                PopulateForm(selected);
        }
    }

    private void OnPaymentListSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Payment selected)
        {
            paymentPicker.SelectedItem = selected.PaymentID;
            PopulateForm(selected);
        }
    }

    private async void OnSavePaymentClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1 || string.IsNullOrWhiteSpace(paymentIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Contract and Payment ID are required.", "OK");
            return;
        }

        var contract = contracts.First(c => c.ContractID == contractPicker.SelectedItem.ToString());

        RecalculateTotalAmountForSelectedContract();
        var totalAmount = RateCalculation.TryParseDouble(totalAmountEntry.Text, out var calcTotal) ? calcTotal : 0;

        var payment = new Payment
        {
            PaymentID = paymentIDEntry.Text,
            ContractID = contract.ContractID,
            TotalAmount = totalAmount,
            AmountPaid = RateCalculation.TryParseDouble(paidAmountEntry.Text, out var amt) ? amt : 0,
            TotalBales = int.TryParse(balesEntry.Text, out var bales) ? bales : 0,
            Date = paymentDatePicker.Date
        };

        if (payments.Any(p => p.PaymentID == payment.PaymentID))
        {
            await DisplayAlert("Duplicate", "Payment ID already exists. Use Update.", "OK");
            return;
        }

        await _db.AddPayment(payment);
        await DisplayAlert("Success", "Payment added successfully.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnUpdatePaymentClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(paymentIDEntry.Text)) return;

        var contract = contracts.First(c => c.ContractID == contractPicker.SelectedItem.ToString());

        RecalculateTotalAmountForSelectedContract();
        var totalAmount = RateCalculation.TryParseDouble(totalAmountEntry.Text, out var calcTotal) ? calcTotal : 0;

        var payment = new Payment
        {
            PaymentID = paymentIDEntry.Text,
            ContractID = contract.ContractID,
            TotalAmount = totalAmount,
            AmountPaid = RateCalculation.TryParseDouble(paidAmountEntry.Text, out var amt) ? amt : 0,
            TotalBales = int.TryParse(balesEntry.Text, out var bales) ? bales : 0,
            Date = paymentDatePicker.Date
        };

        await _db.UpdatePayment(payment);
        await DisplayAlert("Success", "Payment updated successfully.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnDeletePaymentClicked(object sender, EventArgs e)
    {
        if (paymentPicker.SelectedItem is not string id) return;

        await _db.DeletePayment(id);
        await DisplayAlert("Deleted", "Payment deleted successfully.", "OK");
        ClearForm();
        await LoadData();
    }

    private void OnPaidAmountChanged(object sender, TextChangedEventArgs e)
    {
        RecalculateRemainingAmount();
    }

    private void RecalculateTotalAmountForSelectedContract()
    {
        if (_isTotalAmountManuallyEdited)
        {
            return;
        }

        if (contractPicker.SelectedItem is not string contractId)
        {
            _isSettingSuggestedTotal = true;
            totalAmountEntry.Text = "0.00";
            _isSettingSuggestedTotal = false;
            return;
        }

        var contract = contracts.FirstOrDefault(c => c.ContractID == contractId);
        if (contract == null)
        {
            _isSettingSuggestedTotal = true;
            totalAmountEntry.Text = "0.00";
            _isSettingSuggestedTotal = false;
            return;
        }

        var contractDeliveries = deliveries.Where(d => d.ContractID == contractId).ToList();
        var suggestedTotal = contractDeliveries.Sum(d => d.Amount);

        if (suggestedTotal <= 0)
        {
            var millWeightKg = contractDeliveries.Sum(d => d.MillWeight);
            suggestedTotal = RateCalculation.AmountFromKg(millWeightKg, contract.PricePerBatch);
        }

        _isSettingSuggestedTotal = true;
        totalAmountEntry.Text = suggestedTotal.ToString("F2");
        _isSettingSuggestedTotal = false;
    }

    private void RecalculateRemainingAmount()
    {
        var total = RateCalculation.TryParseDouble(totalAmountEntry.Text, out var totalAmount) ? totalAmount : 0;
        var paid = RateCalculation.TryParseDouble(paidAmountEntry.Text, out var paidAmount) ? paidAmount : 0;
        remainingAmountEntry.Text = (total - paid).ToString("F2");
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower() ?? "";
        paymentListView.ItemsSource = new ObservableCollection<Payment>(
            payments.Where(p => p.PaymentID.ToLower().Contains(query))
        );
    }

    // Navigation
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnGinnerLedgerPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnerLedgerPage());
    private void OnLogOutButtonClicked(object sender, EventArgs e) => App.Logout();
}
