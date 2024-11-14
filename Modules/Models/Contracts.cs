using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Models
{
    public class Contracts
    {
        public string ContractID { get; set; }
        public string GinnerID { get; set; }
        public string MillID { get; set; }
        public int TotalBales { get; set; }
        public double PricePerBatch { get; set; }
        public double TotalAmount { get; set; }
        public double CommissionPercentage { get; set; }
        public string DateCreated { get; set; }
        public string DeliveryNotes { get; set; }
        public string PaymentNotes { get; set; }
    }
}
