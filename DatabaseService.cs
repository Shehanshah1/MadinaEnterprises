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
        private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "madina.db3");
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
                    DeliveryDate TEXT
                );
                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentID TEXT PRIMARY KEY,
                    ContractID TEXT,
                    TotalAmount REAL,
                    AmountPaid REAL,
                    TotalBales INTEGER,
                    Date TEXT
                );
                CREATE TABLE IF NOT EXISTS Ginners (
                    GinnerName TEXT PRIMARY KEY,
                    Address TEXT,
                    NTN TEXT,
                    STN TEXT,
                    BankAccount TEXT,
                    BankAddress TEXT,
                    ContactPerson TEXT,
                    Station TEXT
                );
                
                CREATE TABLE IF NOT EXISTS GinnerLedger (
                    ContractID TEXT,
                    DealID TEXT,
                    AmountPaid REAL,
                    DatePaid TEXT,
                    MillsDueTo TEXT,
                    PRIMARY KEY (ContractID, DealID)
                );

                CREATE TABLE IF NOT EXISTS Mills (
                    MillName TEXT PRIMARY KEY,
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
                    ContractID = reader["ContractID"].ToString(),
                    GinnerID = reader["GinnerID"].ToString(),
                    MillID = reader["MillID"].ToString(),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    PricePerBatch = Convert.ToDouble(reader["PricePerBatch"]),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    CommissionPercentage = Convert.ToDouble(reader["CommissionPercentage"]),
                    DateCreated = DateTime.Parse(reader["DateCreated"].ToString()),
                    DeliveryNotes = reader["DeliveryNotes"].ToString(),
                    PaymentNotes = reader["PaymentNotes"].ToString()
                });
            }
            return list;
        }

        public async Task AddContract(Contracts c)
        {
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
            cmd.Parameters.AddWithValue("@DeliveryNotes", c.DeliveryNotes);
            cmd.Parameters.AddWithValue("@PaymentNotes", c.PaymentNotes);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateContract(Contracts c)
        {
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
            cmd.Parameters.AddWithValue("@DeliveryNotes", c.DeliveryNotes);
            cmd.Parameters.AddWithValue("@PaymentNotes", c.PaymentNotes);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteContract(string contractId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
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
                    DeliveryID = reader["DeliveryID"].ToString(),
                    ContractID = reader["ContractID"].ToString(),
                    Amount = Convert.ToDouble(reader["Amount"]),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    FactoryWeight = Convert.ToDouble(reader["FactoryWeight"]),
                    MillWeight = Convert.ToDouble(reader["MillWeight"]),
                    TruckNumber = reader["TruckNumber"].ToString(),
                    DriverContact = reader["DriverContact"].ToString(),
                    DepartureDate = DateTime.Parse(reader["DepartureDate"].ToString()),
                    DeliveryDate = DateTime.Parse(reader["DeliveryDate"].ToString())
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
            cmd.Parameters.AddWithValue("@TruckNumber", d.TruckNumber);
            cmd.Parameters.AddWithValue("@DriverContact", d.DriverContact);
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
            cmd.Parameters.AddWithValue("@TruckNumber", d.TruckNumber);
            cmd.Parameters.AddWithValue("@DriverContact", d.DriverContact);
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
                    PaymentID = reader["PaymentID"].ToString(),
                    ContractID = reader["ContractID"].ToString(),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"]),
                    TotalBales = Convert.ToInt32(reader["TotalBales"]),
                    Date = DateTime.Parse(reader["Date"].ToString())
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
                    GinnerName = reader["GinnerName"].ToString(),
                    Address = reader["Address"].ToString(),
                    NTN = reader["NTN"].ToString(),
                    STN = reader["STN"].ToString(),
                    BankAccount = reader["BankAccount"].ToString(),
                    BankAddress = reader["BankAddress"].ToString(),
                    ContactPerson = reader["ContactPerson"].ToString(),
                    Station = reader["Station"].ToString()
                });
            }
            return list;
        }

        public async Task AddGinner(Ginners g)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Ginners (GinnerName, Address, NTN, STN, BankAccount, BankAddress, ContactPerson, Station)
                VALUES (@GinnerName, @Address, @NTN, @STN, @BankAccount, @BankAddress, @ContactPerson, @Station)", conn);

            cmd.Parameters.AddWithValue("@GinnerName", g.GinnerName);
            cmd.Parameters.AddWithValue("@Address", g.Address);
            cmd.Parameters.AddWithValue("@NTN", g.NTN);
            cmd.Parameters.AddWithValue("@STN", g.STN);
            cmd.Parameters.AddWithValue("@BankAccount", g.BankAccount);
            cmd.Parameters.AddWithValue("@BankAddress", g.BankAddress);
            cmd.Parameters.AddWithValue("@ContactPerson", g.ContactPerson);
            cmd.Parameters.AddWithValue("@Station", g.Station);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateGinner(Ginners g)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Ginners SET Address=@Address, NTN=@NTN, STN=@STN, BankAccount=@BankAccount, BankAddress=@BankAddress, ContactPerson=@ContactPerson, Station=@Station
                WHERE GinnerName=@GinnerName", conn);

            cmd.Parameters.AddWithValue("@GinnerName", g.GinnerName);
            cmd.Parameters.AddWithValue("@Address", g.Address);
            cmd.Parameters.AddWithValue("@NTN", g.NTN);
            cmd.Parameters.AddWithValue("@STN", g.STN);
            cmd.Parameters.AddWithValue("@BankAccount", g.BankAccount);
            cmd.Parameters.AddWithValue("@BankAddress", g.BankAddress);
            cmd.Parameters.AddWithValue("@ContactPerson", g.ContactPerson);
            cmd.Parameters.AddWithValue("@Station", g.Station);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteGinner(string ginnerName)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM Ginners WHERE GinnerName = @name", conn);
            cmd.Parameters.AddWithValue("@name", ginnerName);
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
                    MillName = reader["MillName"].ToString(),
                    Address = reader["Address"].ToString(),
                    OwnerName = reader["OwnerName"].ToString()
                });
            }
            return list;
        }

        public async Task AddMill(Mills m)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("INSERT INTO Mills (MillName, Address, OwnerName) VALUES (@MillName, @Address, @OwnerName)", conn);
            cmd.Parameters.AddWithValue("@MillName", m.MillName);
            cmd.Parameters.AddWithValue("@Address", m.Address);
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateMill(Mills m)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("UPDATE Mills SET Address=@Address, OwnerName=@OwnerName WHERE MillName=@MillName", conn);
            cmd.Parameters.AddWithValue("@MillName", m.MillName);
            cmd.Parameters.AddWithValue("@Address", m.Address);
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteMill(string millName)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SQLiteCommand("DELETE FROM Mills WHERE MillName = @name", conn);
            cmd.Parameters.AddWithValue("@name", millName);
            await cmd.ExecuteNonQueryAsync();
        }
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
            cmd.Parameters.AddWithValue("@MillsDueTo", g.MillsDueTo);

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
            cmd.Parameters.AddWithValue("@MillsDueTo", g.MillsDueTo);

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

    }
}
