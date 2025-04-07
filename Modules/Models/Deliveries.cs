using System;

namespace MadinaEnterprises.Modules.Models
{
    public class Deliveries
    {
        public string DeliveryID { get; set; } = string.Empty;
        public string ContractID { get; set; } = string.Empty;
        public double Amount { get; set; }
        public int TotalBales { get; set; }
        public double FactoryWeight { get; set; }
        public double MillWeight { get; set; }
        public string TruckNumber { get; set; } = string.Empty;
        public string DriverContact { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public DateTime DeliveryDate { get; set; }
    }
}
