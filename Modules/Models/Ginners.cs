using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Models
{
    public class Ginners
    {
        public string GinnerID { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string IBAN { get; set; }
        public string NTN { get; set; }
        public string STN { get; set; }

        // ObservableCollection for Ginners
        public ObservableCollection<Ginners> GinnerList { get; set; } = new ObservableCollection<Ginners>();

        // Commands for Edit/Delete actions
        public Command<Ginners> EditGinnerCommand { get; set; }
        public Command<Ginners> DeleteGinnerCommand { get; set; }
    }
}
