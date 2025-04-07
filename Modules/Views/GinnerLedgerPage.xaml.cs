using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views
{
    public partial class GinnerLedgerPage : ContentPage
    {
        private readonly DatabaseService _database;
        private GinnerLedger _selectedEntry;

        public GinnerLedgerPage()
        {
            InitializeComponent();
            _database = App.DatabaseService;
            LoadLedgerEntries();
        }

        private async void LoadLedgerEntries()
        {
            var entries = await _database.GetAllGinnerLedger();
            ledgerCollectionView.ItemsSource = entries;
        }

        private async void OnAddLedgerEntryClicked(object sender, EventArgs e)
        {
            var entry = new GinnerLedger
            {
                ContractID = contractIdEntry.Text,
                DealID = dealIdEntry.Text,
                AmountPaid = double.Parse(amountPaidEntry.Text),
                DatePaid = DateTime.Parse(datePaidEntry.Text),
                MillsDueTo = millsDueToEntry.Text
            };

            await _database.AddGinnerLedger(entry);
            ClearForm();
            LoadLedgerEntries();
        }

        private async void OnUpdateLedgerEntryClicked(object sender, EventArgs e)
        {
            if (_selectedEntry == null) return;

            _selectedEntry.ContractID = contractIdEntry.Text;
            _selectedEntry.DealID = dealIdEntry.Text;
            _selectedEntry.AmountPaid = double.Parse(amountPaidEntry.Text);
            _selectedEntry.DatePaid = DateTime.Parse(datePaidEntry.Text);
            _selectedEntry.MillsDueTo = millsDueToEntry.Text;

            await _database.UpdateGinnerLedger(_selectedEntry);
            ClearForm();
            LoadLedgerEntries();
        }

        private async void OnDeleteLedgerEntryClicked(object sender, EventArgs e)
        {
            if (_selectedEntry == null) return;

            await _database.DeleteGinnerLedger(_selectedEntry.ContractID, _selectedEntry.DealID);
            ClearForm();
            LoadLedgerEntries();
        }

        private void ClearForm()
        {
            contractIdEntry.Text = "";
            dealIdEntry.Text = "";
            amountPaidEntry.Text = "";
            datePaidEntry.Text = "";
            millsDueToEntry.Text = "";
            _selectedEntry = null;
        }

        private void OnLogoutButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new LoginPage();
        private void OnDashboardPageButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new DashboardPage();
        private void OnGinnersPageButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new GinnersPage();
        private void OnMillsPageButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new MillsPage();
        private void OnContractsPageButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new ContractsPage();
        private void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new DeliveriesPage();
        private void OnPaymentsPageButtonClicked(object sender, EventArgs e) => Application.Current.MainPage = new PaymentsPage();

        private void ledgerCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEntry = e.CurrentSelection.FirstOrDefault() as GinnerLedger;
            if (_selectedEntry != null)
            {
                contractIdEntry.Text = _selectedEntry.ContractID;
                dealIdEntry.Text = _selectedEntry.DealID;
                amountPaidEntry.Text = _selectedEntry.AmountPaid.ToString();
                datePaidEntry.Text = _selectedEntry.DatePaid.ToString("yyyy-MM-dd");
                millsDueToEntry.Text = _selectedEntry.MillsDueTo;
            }
        }
    }
}
