using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MadinaEnterprises
{
    public class DatabaseService
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "madina.db3");
            _connectionString = $"Data Source={_databasePath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_databasePath))
                SQLiteConnection.CreateFile(_databasePath);

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var command = new SQLiteCommand(connection);

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Contracts (
                    ContractID TEXT PRIMARY KEY,
                    GinnerID TEXT,
                    MillID TEXT,
                    TotalBales INTEGER,
                    PricePerBatch REAL,
                    TotalAmount REAL,
                    CommissionPercentage REAL,
                    DateCreated TEXT,
                    DeliveryNotes TEXT,
                    PaymentNotes TEXT
                );
                
                CREATE TABLE IF NOT EXISTS Deliveries (
                    DeliveryID TEXT PRIMARY KEY,
                    ContractID TEXT,
                    Amount REAL,
                    TotalBales INTEGER,
                    FactoryWeight REAL,
                    MillWeight REAL,
                    TruckNumber TEXT,
                    DriverContact TEXT,
                    DepartureDate TEXT,
                    DeliveryDate TEXT,
                    FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID)
                );
                
                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentID TEXT PRIMARY KEY,
                    ContractID TEXT,
                    TotalAmount REAL,
                    AmountPaid REAL,
                    TotalBales INTEGER,
                    Date TEXT,
                    FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID)
                );

                CREATE TABLE IF NOT EXISTS Ginners (
                    GinnerID TEXT PRIMARY KEY,
                    GinnerName TEXT,
                    Contact TEXT,
                    IBAN TEXT,
                    Address TEXT,
                    NTN TEXT,
                    STN TEXT,
                    BankAddress TEXT,
                    ContactPerson TEXT,
                    Station TEXT
                );
                
                CREATE TABLE IF NOT EXISTS GinnerLedger (
                    LedgerID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractID TEXT,
                    DealID TEXT,
                    AmountPaid REAL,
                    DatePaid TEXT,
                    MillsDueTo TEXT,
                    FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID)
                );

                CREATE TABLE IF NOT EXISTS Mills (
                    MillID TEXT PRIMARY KEY,
                    MillName TEXT,
                    Address TEXT,
                    OwnerName TEXT
                );
            ";
            command.ExecuteNonQuery();
        }

        // ========== CONTRACTS ==========
        public async Task<List<Contracts>> GetAllContracts()
        {
            var list = new List<Contracts>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Contracts", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Contracts
                {
                    ContractID = reader["ContractID"].ToString() ?? "",
                    GinnerID = reader["GinnerID"].ToString() ?? "",
                    MillID = reader["MillID"].ToString() ?? "",
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    PricePerBatch = Convert.ToDouble(reader["PricePerBatch"]),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    CommissionPercentage = Convert.ToDouble(reader["CommissionPercentage"]),
                    DateCreated = DateTime.Parse(reader["DateCreated"].ToString() ?? DateTime.Now.ToString()),
                    DeliveryNotes = reader["DeliveryNotes"].ToString() ?? "",
                    PaymentNotes = reader["PaymentNotes"].ToString() ?? ""
                });
            }
            return list;
        }

        public async Task<Contracts?> GetContractById(string contractId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Contracts WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Contracts
                {
                    ContractID = reader["ContractID"].ToString() ?? "",
                    GinnerID = reader["GinnerID"].ToString() ?? "",
                    MillID = reader["MillID"].ToString() ?? "",
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    PricePerBatch = Convert.ToDouble(reader["PricePerBatch"]),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    CommissionPercentage = Convert.ToDouble(reader["CommissionPercentage"]),
                    DateCreated = DateTime.Parse(reader["DateCreated"].ToString() ?? DateTime.Now.ToString()),
                    DeliveryNotes = reader["DeliveryNotes"].ToString() ?? "",
                    PaymentNotes = reader["PaymentNotes"].ToString() ?? ""
                };
            }
            return null;
        }

        public async Task AddContract(Contracts c)
        {
            // Calculate total amount if not set
            if (c.TotalAmount == 0 && c.TotalBales > 0 && c.PricePerBatch > 0)
            {
                c.TotalAmount = c.TotalBales * c.PricePerBatch;
            }

            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Contracts (ContractID, GinnerID, MillID, TotalBales, PricePerBatch, TotalAmount, CommissionPercentage, DateCreated, DeliveryNotes, PaymentNotes)
                VALUES (@ContractID, @GinnerID, @MillID, @TotalBales, @PricePerBatch, @TotalAmount, @CommissionPercentage, @DateCreated, @DeliveryNotes, @PaymentNotes)", conn);

            cmd.Parameters.AddWithValue("@ContractID", c.ContractID);
            cmd.Parameters.AddWithValue("@GinnerID", c.GinnerID);
            cmd.Parameters.AddWithValue("@MillID", c.MillID);
            cmd.Parameters.AddWithValue("@TotalBales", c.TotalBales);
            cmd.Parameters.AddWithValue("@PricePerBatch", c.PricePerBatch);
            cmd.Parameters.AddWithValue("@TotalAmount", c.TotalAmount);
            cmd.Parameters.AddWithValue("@CommissionPercentage", c.CommissionPercentage);
            cmd.Parameters.AddWithValue("@DateCreated", c.DateCreated.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryNotes", c.DeliveryNotes ?? "");
            cmd.Parameters.AddWithValue("@PaymentNotes", c.PaymentNotes ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateContract(Contracts c)
        {
            // Recalculate total amount
            if (c.TotalBales > 0 && c.PricePerBatch > 0)
            {
                c.TotalAmount = c.TotalBales * c.PricePerBatch;
            }

            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Contracts SET GinnerID=@GinnerID, MillID=@MillID, TotalBales=@TotalBales, PricePerBatch=@PricePerBatch, TotalAmount=@TotalAmount,
                CommissionPercentage=@CommissionPercentage, DateCreated=@DateCreated, DeliveryNotes=@DeliveryNotes, PaymentNotes=@PaymentNotes
                WHERE ContractID=@ContractID", conn);

            cmd.Parameters.AddWithValue("@ContractID", c.ContractID);
            cmd.Parameters.AddWithValue("@GinnerID", c.GinnerID);
            cmd.Parameters.AddWithValue("@MillID", c.MillID);
            cmd.Parameters.AddWithValue("@TotalBales", c.TotalBales);
            cmd.Parameters.AddWithValue("@PricePerBatch", c.PricePerBatch);
            cmd.Parameters.AddWithValue("@TotalAmount", c.TotalAmount);
            cmd.Parameters.AddWithValue("@CommissionPercentage", c.CommissionPercentage);
            cmd.Parameters.AddWithValue("@DateCreated", c.DateCreated.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryNotes", c.DeliveryNotes ?? "");
            cmd.Parameters.AddWithValue("@PaymentNotes", c.PaymentNotes ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteContract(string contractId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            // Delete related records first (maintain referential integrity)
            var deleteDeliveries = new SQLiteCommand("DELETE FROM Deliveries WHERE ContractID = @id", conn);
            deleteDeliveries.Parameters.AddWithValue("@id", contractId);
            await deleteDeliveries.ExecuteNonQueryAsync();

            var deletePayments = new SQLiteCommand("DELETE FROM Payments WHERE ContractID = @id", conn);
            deletePayments.Parameters.AddWithValue("@id", contractId);
            await deletePayments.ExecuteNonQueryAsync();

            var deleteLedger = new SQLiteCommand("DELETE FROM GinnerLedger WHERE ContractID = @id", conn);
            deleteLedger.Parameters.AddWithValue("@id", contractId);
            await deleteLedger.ExecuteNonQueryAsync();

            var cmd = new SQLiteCommand("DELETE FROM Contracts WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== DELIVERIES ==========
        public async Task<List<Deliveries>> GetAllDeliveries()
        {
            var list = new List<Deliveries>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Deliveries", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Deliveries
                {
                    DeliveryID = reader["DeliveryID"].ToString() ?? "",
                    ContractID = reader["ContractID"].ToString() ?? "",
                    Amount = Convert.ToDouble(reader["Amount"]),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    FactoryWeight = Convert.ToDouble(reader["FactoryWeight"]),
                    MillWeight = Convert.ToDouble(reader["MillWeight"]),
                    TruckNumber = reader["TruckNumber"].ToString() ?? "",
                    DriverContact = reader["DriverContact"].ToString() ?? "",
                    DepartureDate = DateTime.Parse(reader["DepartureDate"].ToString() ?? DateTime.Now.ToString()),
                    DeliveryDate = DateTime.Parse(reader["DeliveryDate"].ToString() ?? DateTime.Now.ToString())
                });
            }
            return list;
        }

        public async Task<List<Deliveries>> GetDeliveriesByContractId(string contractId)
        {
            var list = new List<Deliveries>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Deliveries WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Deliveries
                {
                    DeliveryID = reader["DeliveryID"].ToString() ?? "",
                    ContractID = reader["ContractID"].ToString() ?? "",
                    Amount = Convert.ToDouble(reader["Amount"]),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    FactoryWeight = Convert.ToDouble(reader["FactoryWeight"]),
                    MillWeight = Convert.ToDouble(reader["MillWeight"]),
                    TruckNumber = reader["TruckNumber"].ToString() ?? "",
                    DriverContact = reader["DriverContact"].ToString() ?? "",
                    DepartureDate = DateTime.Parse(reader["DepartureDate"].ToString() ?? DateTime.Now.ToString()),
                    DeliveryDate = DateTime.Parse(reader["DeliveryDate"].ToString() ?? DateTime.Now.ToString())
                });
            }
            return list;
        }

        public async Task AddDelivery(Deliveries d)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Deliveries (DeliveryID, ContractID, Amount, TotalBales, FactoryWeight, MillWeight, TruckNumber, DriverContact, DepartureDate, DeliveryDate)
                VALUES (@DeliveryID, @ContractID, @Amount, @TotalBales, @FactoryWeight, @MillWeight, @TruckNumber, @DriverContact, @DepartureDate, @DeliveryDate)", conn);

            cmd.Parameters.AddWithValue("@DeliveryID", d.DeliveryID);
            cmd.Parameters.AddWithValue("@ContractID", d.ContractID);
            cmd.Parameters.AddWithValue("@Amount", d.Amount);
            cmd.Parameters.AddWithValue("@TotalBales", d.TotalBales);
            cmd.Parameters.AddWithValue("@FactoryWeight", d.FactoryWeight);
            cmd.Parameters.AddWithValue("@MillWeight", d.MillWeight);
            cmd.Parameters.AddWithValue("@TruckNumber", d.TruckNumber ?? "");
            cmd.Parameters.AddWithValue("@DriverContact", d.DriverContact ?? "");
            cmd.Parameters.AddWithValue("@DepartureDate", d.DepartureDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryDate", d.DeliveryDate.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateDelivery(Deliveries d)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Deliveries SET ContractID=@ContractID, Amount=@Amount, TotalBales=@TotalBales,
                FactoryWeight=@FactoryWeight, MillWeight=@MillWeight, TruckNumber=@TruckNumber, DriverContact=@DriverContact,
                DepartureDate=@DepartureDate, DeliveryDate=@DeliveryDate
                WHERE DeliveryID=@DeliveryID", conn);

            cmd.Parameters.AddWithValue("@DeliveryID", d.DeliveryID);
            cmd.Parameters.AddWithValue("@ContractID", d.ContractID);
            cmd.Parameters.AddWithValue("@Amount", d.Amount);
            cmd.Parameters.AddWithValue("@TotalBales", d.TotalBales);
            cmd.Parameters.AddWithValue("@FactoryWeight", d.FactoryWeight);
            cmd.Parameters.AddWithValue("@MillWeight", d.MillWeight);
            cmd.Parameters.AddWithValue("@TruckNumber", d.TruckNumber ?? "");
            cmd.Parameters.AddWithValue("@DriverContact", d.DriverContact ?? "");
            cmd.Parameters.AddWithValue("@DepartureDate", d.DepartureDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryDate", d.DeliveryDate.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteDelivery(string deliveryId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM Deliveries WHERE DeliveryID = @id", conn);
            cmd.Parameters.AddWithValue("@id", deliveryId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== PAYMENTS ==========
        public async Task<List<Payment>> GetAllPayments()
        {
            var list = new List<Payment>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Payments", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Payment
                {
                    PaymentID = reader["PaymentID"].ToString() ?? "",
                    ContractID = reader["ContractID"].ToString() ?? "",
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"]),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    Date = DateTime.Parse(reader["Date"].ToString() ?? DateTime.Now.ToString())
                });
            }
            return list;
        }

        public async Task<List<Payment>> GetPaymentsByContractId(string contractId)
        {
            var list = new List<Payment>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Payments WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Payment
                {
                    PaymentID = reader["PaymentID"].ToString() ?? "",
                    ContractID = reader["ContractID"].ToString() ?? "",
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"]),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    Date = DateTime.Parse(reader["Date"].ToString() ?? DateTime.Now.ToString())
                });
            }
            return list;
        }

        public async Task AddPayment(Payment p)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Payments (PaymentID, ContractID, TotalAmount, AmountPaid, TotalBales, Date)
                VALUES (@PaymentID, @ContractID, @TotalAmount, @AmountPaid, @TotalBales, @Date)", conn);

            cmd.Parameters.AddWithValue("@PaymentID", p.PaymentID);
            cmd.Parameters.AddWithValue("@ContractID", p.ContractID);
            cmd.Parameters.AddWithValue("@TotalAmount", p.TotalAmount);
            cmd.Parameters.AddWithValue("@AmountPaid", p.AmountPaid);
            cmd.Parameters.AddWithValue("@TotalBales", p.TotalBales);
            cmd.Parameters.AddWithValue("@Date", p.Date.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePayment(Payment p)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Payments SET ContractID=@ContractID, TotalAmount=@TotalAmount, AmountPaid=@AmountPaid, TotalBales=@TotalBales, Date=@Date
                WHERE PaymentID=@PaymentID", conn);

            cmd.Parameters.AddWithValue("@PaymentID", p.PaymentID);
            cmd.Parameters.AddWithValue("@ContractID", p.ContractID);
            cmd.Parameters.AddWithValue("@TotalAmount", p.TotalAmount);
            cmd.Parameters.AddWithValue("@AmountPaid", p.AmountPaid);
            cmd.Parameters.AddWithValue("@TotalBales", p.TotalBales);
            cmd.Parameters.AddWithValue("@Date", p.Date.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeletePayment(string paymentId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM Payments WHERE PaymentID = @id", conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== GINNERS ==========
        public async Task<List<Ginners>> GetAllGinners()
        {
            var list = new List<Ginners>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Ginners", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Ginners
                {
                    GinnerID = reader["GinnerID"].ToString() ?? "",
                    GinnerName = reader["GinnerName"].ToString() ?? "",
                    Contact = reader["Contact"].ToString() ?? "",
                    IBAN = reader["IBAN"].ToString() ?? "",
                    Address = reader["Address"].ToString() ?? "",
                    NTN = reader["NTN"].ToString() ?? "",
                    STN = reader["STN"].ToString() ?? "",
                    BankAddress = reader["BankAddress"].ToString() ?? "",
                    ContactPerson = reader["ContactPerson"].ToString() ?? "",
                    Station = reader["Station"].ToString() ?? ""
                });
            }
            return list;
        }

        public async Task<Ginners?> GetGinnerById(string ginnerId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Ginners WHERE GinnerID = @id", conn);
            cmd.Parameters.AddWithValue("@id", ginnerId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Ginners
                {
                    GinnerID = reader["GinnerID"].ToString() ?? "",
                    GinnerName = reader["GinnerName"].ToString() ?? "",
                    Contact = reader["Contact"].ToString() ?? "",
                    IBAN = reader["IBAN"].ToString() ?? "",
                    Address = reader["Address"].ToString() ?? "",
                    NTN = reader["NTN"].ToString() ?? "",
                    STN = reader["STN"].ToString() ?? "",
                    BankAddress = reader["BankAddress"].ToString() ?? "",
                    ContactPerson = reader["ContactPerson"].ToString() ?? "",
                    Station = reader["Station"].ToString() ?? ""
                };
            }
            return null;
        }

        public async Task AddGinner(Ginners g)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Ginners (GinnerID, GinnerName, Contact, IBAN, Address, NTN, STN, BankAddress, ContactPerson, Station)
                VALUES (@GinnerID, @GinnerName, @Contact, @IBAN, @Address, @NTN, @STN, @BankAddress, @ContactPerson, @Station)", conn);

            cmd.Parameters.AddWithValue("@GinnerID", g.GinnerID);
            cmd.Parameters.AddWithValue("@GinnerName", g.GinnerName ?? "");
            cmd.Parameters.AddWithValue("@Contact", g.Contact ?? "");
            cmd.Parameters.AddWithValue("@IBAN", g.IBAN ?? "");
            cmd.Parameters.AddWithValue("@Address", g.Address ?? "");
            cmd.Parameters.AddWithValue("@NTN", g.NTN ?? "");
            cmd.Parameters.AddWithValue("@STN", g.STN ?? "");
            cmd.Parameters.AddWithValue("@BankAddress", g.BankAddress ?? "");
            cmd.Parameters.AddWithValue("@ContactPerson", g.ContactPerson ?? "");
            cmd.Parameters.AddWithValue("@Station", g.Station ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateGinner(Ginners g)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Ginners SET GinnerName=@GinnerName, Contact=@Contact, IBAN=@IBAN, Address=@Address, NTN=@NTN, STN=@STN, BankAddress=@BankAddress, ContactPerson=@ContactPerson, Station=@Station
                WHERE GinnerID=@GinnerID", conn);

            cmd.Parameters.AddWithValue("@GinnerID", g.GinnerID);
            cmd.Parameters.AddWithValue("@GinnerName", g.GinnerName ?? "");
            cmd.Parameters.AddWithValue("@Contact", g.Contact ?? "");
            cmd.Parameters.AddWithValue("@IBAN", g.IBAN ?? "");
            cmd.Parameters.AddWithValue("@Address", g.Address ?? "");
            cmd.Parameters.AddWithValue("@NTN", g.NTN ?? "");
            cmd.Parameters.AddWithValue("@STN", g.STN ?? "");
            cmd.Parameters.AddWithValue("@BankAddress", g.BankAddress ?? "");
            cmd.Parameters.AddWithValue("@ContactPerson", g.ContactPerson ?? "");
            cmd.Parameters.AddWithValue("@Station", g.Station ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteGinner(string ginnerId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM Ginners WHERE GinnerID = @GinnerID", conn);
            cmd.Parameters.AddWithValue("@GinnerID", ginnerId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== MILLS ==========
        public async Task<List<Mills>> GetAllMills()
        {
            var list = new List<Mills>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Mills", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Mills
                {
                    MillID = reader["MillID"].ToString() ?? "",
                    MillName = reader["MillName"].ToString() ?? "",
                    Address = reader["Address"].ToString() ?? "",
                    OwnerName = reader["OwnerName"].ToString() ?? ""
                });
            }
            return list;
        }

        public async Task<Mills?> GetMillById(string millId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Mills WHERE MillID = @id", conn);
            cmd.Parameters.AddWithValue("@id", millId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Mills
                {
                    MillID = reader["MillID"].ToString() ?? "",
                    MillName = reader["MillName"].ToString() ?? "",
                    Address = reader["Address"].ToString() ?? "",
                    OwnerName = reader["OwnerName"].ToString() ?? ""
                };
            }
            return null;
        }

        public async Task AddMill(Mills m)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("INSERT INTO Mills (MillID, MillName, Address, OwnerName) VALUES (@MillID, @MillName, @Address, @OwnerName)", conn);
            cmd.Parameters.AddWithValue("@MillID", m.MillID);
            cmd.Parameters.AddWithValue("@MillName", m.MillName ?? "");
            cmd.Parameters.AddWithValue("@Address", m.Address ?? "");
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateMill(Mills m)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("UPDATE Mills SET MillName=@MillName, Address=@Address, OwnerName=@OwnerName WHERE MillID=@MillID", conn);
            cmd.Parameters.AddWithValue("@MillID", m.MillID);
            cmd.Parameters.AddWithValue("@MillName", m.MillName ?? "");
            cmd.Parameters.AddWithValue("@Address", m.Address ?? "");
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteMill(string millId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM Mills WHERE MillID = @MillID", conn);
            cmd.Parameters.AddWithValue("@MillID", millId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== GINNER LEDGER ==========
        public async Task<List<GinnerLedger>> GetAllGinnerLedger()
        {
            var ginnerLedgerList = new List<GinnerLedger>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var command = new SQLiteCommand("SELECT * FROM GinnerLedger", conn);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ginnerLedgerList.Add(new GinnerLedger
                {
                    ContractID = reader["ContractID"].ToString() ?? "",
                    DealID = reader["DealID"].ToString() ?? "",
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"]),
                    DatePaid = DateTime.Parse(reader["DatePaid"].ToString() ?? DateTime.Today.ToString()),
                    MillsDueTo = reader["MillsDueTo"].ToString() ?? ""
                });
            }

            return ginnerLedgerList;
        }

        public async Task<List<GinnerLedger>> GetGinnerLedgerByContractId(string contractId)
        {
            var list = new List<GinnerLedger>();
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("SELECT * FROM GinnerLedger WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new GinnerLedger
                {
                    ContractID = reader["ContractID"].ToString() ?? "",
                    DealID = reader["DealID"].ToString() ?? "",
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"]),
                    DatePaid = DateTime.Parse(reader["DatePaid"].ToString() ?? DateTime.Today.ToString()),
                    MillsDueTo = reader["MillsDueTo"].ToString() ?? ""
                });
            }
            return list;
        }

        public async Task AddGinnerLedger(GinnerLedger g)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO GinnerLedger (ContractID, DealID, AmountPaid, DatePaid, MillsDueTo)
                VALUES (@ContractID, @DealID, @AmountPaid, @DatePaid, @MillsDueTo)", conn);

            cmd.Parameters.AddWithValue("@ContractID", g.ContractID);
            cmd.Parameters.AddWithValue("@DealID", g.DealID);
            cmd.Parameters.AddWithValue("@AmountPaid", g.AmountPaid);
            cmd.Parameters.AddWithValue("@DatePaid", g.DatePaid.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@MillsDueTo", g.MillsDueTo ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateGinnerLedger(GinnerLedger g)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE GinnerLedger SET AmountPaid=@AmountPaid, DatePaid=@DatePaid, MillsDueTo=@MillsDueTo
                WHERE ContractID=@ContractID AND DealID=@DealID", conn);

            cmd.Parameters.AddWithValue("@ContractID", g.ContractID);
            cmd.Parameters.AddWithValue("@DealID", g.DealID);
            cmd.Parameters.AddWithValue("@AmountPaid", g.AmountPaid);
            cmd.Parameters.AddWithValue("@DatePaid", g.DatePaid.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@MillsDueTo", g.MillsDueTo ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteGinnerLedger(string contractId, string dealId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM GinnerLedger WHERE ContractID = @ContractID AND DealID = @DealID", conn);
            cmd.Parameters.AddWithValue("@ContractID", contractId);
            cmd.Parameters.AddWithValue("@DealID", dealId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== ANALYTICS & REPORTS ==========
        public async Task<Dictionary<string, object>> GetDashboardStatistics()
        {
            var stats = new Dictionary<string, object>();

            var contracts = await GetAllContracts();
            var payments = await GetAllPayments();
            var deliveries = await GetAllDeliveries();
            var ginners = await GetAllGinners();
            var mills = await GetAllMills();

            // Calculate statistics
            double totalCommission = contracts.Sum(c => c.TotalAmount * (c.CommissionPercentage / 100));
            double totalPaid = payments.Sum(p => p.AmountPaid);
            double totalContractAmount = contracts.Sum(c => c.TotalAmount);
            double totalDue = totalContractAmount - totalPaid;
            int totalBalesSold = deliveries.Sum(d => d.TotalBales);
            int totalBalesContracted = contracts.Sum(c => c.TotalBales);
            double avgCommissionRate = contracts.Any() ? contracts.Average(c => c.CommissionPercentage) : 0;

            stats["TotalCommission"] = totalCommission;
            stats["TotalPaid"] = totalPaid;
            stats["TotalDue"] = totalDue;
            stats["TotalBalesSold"] = totalBalesSold;
            stats["TotalBalesContracted"] = totalBalesContracted;
            stats["TotalGinners"] = ginners.Count;
            stats["TotalMills"] = mills.Count;
            stats["TotalContracts"] = contracts.Count;
            stats["AvgCommissionRate"] = avgCommissionRate;
            stats["TotalDeliveries"] = deliveries.Count;
            stats["TotalPayments"] = payments.Count;

            return stats;
        }

        public async Task<ContractSummary> GetContractSummary(string contractId)
        {
            var contract = await GetContractById(contractId);
            if (contract == null) return new ContractSummary();

            var deliveries = await GetDeliveriesByContractId(contractId);
            var payments = await GetPaymentsByContractId(contractId);
            var ginner = await GetGinnerById(contract.GinnerID);
            var mill = await GetMillById(contract.MillID);

            return new ContractSummary
            {
                Contract = contract,
                Ginner = ginner,
                Mill = mill,
                Deliveries = deliveries,
                Payments = payments,
                TotalDelivered = deliveries.Sum(d => d.TotalBales),
                TotalPaid = payments.Sum(p => p.AmountPaid),
                RemainingBales = contract.TotalBales - deliveries.Sum(d => d.TotalBales),
                RemainingAmount = contract.TotalAmount - payments.Sum(p => p.AmountPaid),
                CommissionAmount = contract.TotalAmount * (contract.CommissionPercentage / 100)
            };
        }
    }

    // Helper class for contract summary
    public class ContractSummary
    {
        public Contracts? Contract { get; set; }
        public Ginners? Ginner { get; set; }
        public Mills? Mill { get; set; }
        public List<Deliveries> Deliveries { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public int TotalDelivered { get; set; }
        public double TotalPaid { get; set; }
        public int RemainingBales { get; set; }
        public double RemainingAmount { get; set; }
        public double CommissionAmount { get; set; }
    }
}