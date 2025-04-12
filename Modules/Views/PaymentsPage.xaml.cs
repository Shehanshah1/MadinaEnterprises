using MadinaEnterprises.Modules.Models;
using Microsoft.Maui.Controls;
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
    private ObservableCollection<Payment> filteredPayments = new();

    public PaymentsPage()
    {
        InitializeComponent();
        _ = LoadData();
    }

    private async Task LoadData()
    {
        contracts = await _db.GetAllContracts();
        payments = await _db.GetAllPayments();

        contractPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();
        paymentPicker.ItemsSource = payments.Select(p => p.PaymentID).ToList();

        filteredPayments = new ObservableCollection<Payment>(payments);
        paymentListView.ItemsSource = filteredPayments;
    }

    private void PopulateForm(Payment p)
    {
        paymentIDEntry.Text = p.PaymentID;
        contractPicker.SelectedItem = p.ContractID;
        totalAmountEntry.Text = p.TotalAmount.ToString("F2");
        paidAmountEntry.Text = p.AmountPaid.ToString("F2");
        remainingAmountEntry.Text = (p.TotalAmount - p.AmountPaid).ToString("F2");
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

        var payment = new Payment
        {
            PaymentID = paymentIDEntry.Text,
            ContractID = contract.ContractID,
            TotalAmount = contract.TotalAmount,
            AmountPaid = double.TryParse(paidAmountEntry.Text, out var amt) ? amt : 0,
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

        var payment = new Payment
        {
            PaymentID = paymentIDEntry.Text,
            ContractID = contract.ContractID,
            TotalAmount = contract.TotalAmount,
            AmountPaid = double.TryParse(paidAmountEntry.Text, out var amt) ? amt : 0,
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
        if (double.TryParse(totalAmountEntry.Text, out var total) &&
            double.TryParse(paidAmountEntry.Text, out var paid))
        {
            remainingAmountEntry.Text = (total - paid).ToString("F2");
        }
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
    private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
}
