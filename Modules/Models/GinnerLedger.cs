using System;

namespace MadinaEnterprises.Modules.Models
{
    public class GinnerLedger
    {
        public string ContractID { get; set; } = string.Empty;
        public string DealID { get; set; } = string.Empty;
        public double AmountPaid { get; set; }
        public DateTime DatePaid { get; set; }
        public string MillsDueTo { get; set; } = string.Empty;
    }
}
