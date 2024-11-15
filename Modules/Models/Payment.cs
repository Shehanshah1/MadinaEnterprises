using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Models
{
    public class Payment
    {
        public string PaymentID { get; set; }
        public string ContractID { get; set; }
        public double AmountPaid { get; set; }
        public DateTime Date { get; set; }
        public string TransactionID { get; set; }
    }
}
