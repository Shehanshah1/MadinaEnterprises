using System;

namespace MadinaEnterprises.Modules.Models
{
    public class Payment
    {
        public string PaymentID { get; set; } = string.Empty;
        public string ContractID { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public double AmountPaid { get; set; }
        public int TotalBales { get; set; }
        public DateTime Date { get; set; }
        public string TransactionID { get; set; } = string.Empty;
    }
}
