using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Models
{
    public class Deliveries
    {
        public string DeliveryID { get; set; }
        public string ContractID { get; set; }
        public string TruckNumber { get; set; }
        public string DriverContact { get; set; }
        public int NumberOfBales { get; set; }
        public DateTime DateBooked { get; set; }
        public DateTime DateDelivered { get; set; }
    }
}
