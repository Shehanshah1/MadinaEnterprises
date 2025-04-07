using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views
{
    public partial class PaymentsPage : ContentPage
    {
        private readonly DatabaseService _db = App.DatabaseService!;
        private List<Payment> _payments = new();
        private List<Contracts> _contracts = new();

        public PaymentsPage()
        {
            InitializeComponent();
            _ = LoadContractsAndPayments();
        }

        private async Task LoadContractsAndPayments()
        {
            _contracts = await _db.GetAllContracts();
            _payments = await _db.GetAllPayments();

            contractPicker.ItemsSource = _contracts.Select(c => c.ContractID).ToList();
            paymentPicker.ItemsSource = _payments.Select(p => p.PaymentID).ToList();
        }

        private void OnPaymentSelected(object sender, EventArgs e)
        {
            if (paymentPicker.SelectedItem is not string selectedID) return;

            var payment = _payments.FirstOrDefault(p => p.PaymentID == selectedID);
            if (payment == null) return;

            paymentIDEntry.Text = payment.PaymentID;
            contractPicker.SelectedItem = payment.ContractID;
            totalAmountEntry.Text = payment.TotalAmount.ToString("F2");
            paidAmountEntry.Text = payment.AmountPaid.ToString("F2");
            balesEntry.Text = payment.TotalBales.ToString();
            paymentDatePicker.Date = payment.Date;
            remainingAmountEntry.Text = (payment.TotalAmount - payment.AmountPaid).ToString("F2");
        }

        private async void OnSavePaymentClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(paymentIDEntry.Text) || contractPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Please provide a valid Payment ID and Contract.", "OK");
                return;
            }

            var contract = _contracts.FirstOrDefault(c => c.ContractID == (string)contractPicker.SelectedItem);
            if (contract == null) return;

            var payment = new Payment
            {
                PaymentID = paymentIDEntry.Text,
                ContractID = contract.ContractID,
                TotalAmount = contract.TotalAmount,
                AmountPaid = double.TryParse(paidAmountEntry.Text, out var amt) ? amt : 0,
                TotalBales = int.TryParse(balesEntry.Text, out var bales) ? bales : 0,
                Date = paymentDatePicker.Date
            };

            var existing = _payments.FirstOrDefault(p => p.PaymentID == payment.PaymentID);
            if (existing == null)
                await _db.AddPayment(payment);
            else
                await _db.UpdatePayment(payment);

            await DisplayAlert("Success", "Payment saved successfully!", "OK");
            ClearForm();
            await LoadContractsAndPayments();
        }

        private async void OnDeletePaymentClicked(object sender, EventArgs e)
        {
            if (paymentPicker.SelectedItem is not string selectedID) return;

            var confirm = await DisplayAlert("Confirm", $"Delete payment {selectedID}?", "Yes", "No");
            if (!confirm) return;

            await _db.DeletePayment(selectedID);
            await DisplayAlert("Deleted", "Payment deleted successfully!", "OK");
            ClearForm();
            await LoadContractsAndPayments();
        }

        private void OnPaidAmountChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(totalAmountEntry.Text, out var total) &&
                double.TryParse(paidAmountEntry.Text, out var paid))
            {
                remainingAmountEntry.Text = (total - paid).ToString("F2");
            }
        }

        private void ClearForm()
        {
            paymentIDEntry.Text = string.Empty;
            paidAmountEntry.Text = string.Empty;
            balesEntry.Text = string.Empty;
            totalAmountEntry.Text = string.Empty;
            remainingAmountEntry.Text = string.Empty;
            contractPicker.SelectedItem = null;
            paymentPicker.SelectedItem = null;
            paymentDatePicker.Date = DateTime.Today;
        }

        // Navigation buttons
        private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
        private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
        private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
        private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
        private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
        private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
    }
}
