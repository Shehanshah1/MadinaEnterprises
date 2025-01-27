using MadinaEnterprises.Modules.Models; 
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace MadinaEnterprises.Modules.Views
{
    public partial class GinnerLedgerPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public GinnerLedgerPage()
        {
            InitializeComponent();
            _databaseService = App.DatabaseService; // Reference to the shared database service
            LoadGinners();
        }

        private void LoadGinners()
        {
            var ginners = _databaseService.GetAllGinners(); // Fetch all ginners from the database
            GinnerPicker.ItemsSource = ginners;
            GinnerPicker.ItemDisplayBinding = new Binding("Name"); // Display the Ginner name in the dropdown
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Navigate back to Ginner Page page
            await App.GoBack();
        }
    }
}
