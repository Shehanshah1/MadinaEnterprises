using System;

namespace MadinaEnterprises.Modules.Models
{
    public class Contracts
    {
        public string ContractID { get; set; } = string.Empty;
        public string GinnerID { get; set; } = string.Empty;
        public string MillID { get; set; } = string.Empty;
        public int TotalBales { get; set; }
        public double PricePerBatch { get; set; }
        public double TotalAmount { get; set; }
        public double CommissionPercentage { get; set; }
        public DateTime DateCreated { get; set; }
        public string DeliveryNotes { get; set; } = string.Empty;
        public string PaymentNotes { get; set; } = string.Empty;
        public string GinnerName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
