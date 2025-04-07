using System;

namespace MadinaEnterprises.Modules.Models
{


    public class GinnerLedger
    {
        private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "madina.db3");
        // Properties
        public string ContractID { get; set; }
        public string DealID { get; set; }
        public int TotalBales { get; set; }
        public int BalesInOneOrder { get; set; }
        public double ContractAmount { get; set; }
        public double TotalAmountPaid { get; set; }
        public double ContractAmountPaid { get; set; }
        public double TotalAmountLeft { get; set; }
        public DateTime DatePaid { get; set; }
        public int BalesSold { get; set; }
        public string MillsDueTo { get; set; }
        public double AmountPaid { get; set; } // Or decimal, depends on your design


        // Constructor
        public GinnerLedger()
        {
        }

        public GinnerLedger(
            string contractID,
            string dealID,
            int totalBales,
            int balesInOneOrder,
            double contractAmount,
            double totalAmountPaid,
            double contractAmountPaid,
            double totalAmountLeft,
            DateTime datePaid,
            int balesSold,
            string millsDueTo)
        {
            ContractID = contractID;
            DealID = dealID;
            TotalBales = totalBales;
            BalesInOneOrder = balesInOneOrder;
            ContractAmount = contractAmount;
            TotalAmountPaid = totalAmountPaid;
            ContractAmountPaid = contractAmountPaid;
            TotalAmountLeft = totalAmountLeft;
            DatePaid = datePaid;
            BalesSold = balesSold;
            MillsDueTo = millsDueTo;
        }

        // Methods for calculation or validation if needed
        public bool IsPaymentComplete()
        {
            return TotalAmountLeft <= 0;
        }

        public bool IsFullyDelivered()
        {
            return BalesSold >= TotalBales;
        }

        // ToString override for debugging or displaying the object
        public override string ToString()
        {
            return $"ContractID: {ContractID}, DealID: {DealID}, TotalBales: {TotalBales}, " +
                   $"BalesInOneOrder: {BalesInOneOrder}, ContractAmount: {ContractAmount:C}, " +
                   $"TotalAmountPaid: {TotalAmountPaid:C}, ContractAmountPaid: {ContractAmountPaid:C}, " +
                   $"TotalAmountLeft: {TotalAmountLeft:C}, DatePaid: {DatePaid}, " +
                   $"BalesSold: {BalesSold}, MillsDueTo: {MillsDueTo}";
        }
    }
}
