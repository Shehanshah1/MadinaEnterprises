using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
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
            _connectionString = $"Data Source={_databasePath};Foreign Keys=True";
            InitializeDatabase();
        }

        private static DateTime ParseDateOrDefault(object? value)
        {
            var text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                return parsed;
            }

            return DateTime.TryParse(text, out parsed) ? parsed : DateTime.MinValue;
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();

            command.CommandText = @"
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

        CREATE TABLE IF NOT EXISTS Mills (
            MillName TEXT,
            MillID TEXT PRIMARY KEY,
            Address TEXT,
            OwnerName TEXT
        );

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
            PaymentNotes TEXT,
            Description TEXT,
            FOREIGN KEY (GinnerID) REFERENCES Ginners(GinnerID) ON UPDATE CASCADE ON DELETE RESTRICT,
            FOREIGN KEY (MillID)   REFERENCES Mills(MillID)     ON UPDATE CASCADE ON DELETE RESTRICT
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
            FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID) ON UPDATE CASCADE ON DELETE RESTRICT
        );

        CREATE TABLE IF NOT EXISTS Payments (
            PaymentID TEXT PRIMARY KEY,
            ContractID TEXT,
            TotalAmount REAL,
            AmountPaid REAL,
            TotalBales INTEGER,
            Date TEXT,
            TransactionID TEXT,
            FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID) ON UPDATE CASCADE ON DELETE RESTRICT
        );
    ";
            command.ExecuteNonQuery();

            // ---- Migration for existing databases: add Description if missing
            using (var pragma = new SqliteCommand("PRAGMA table_info(Contracts);", connection))
            using (var reader = pragma.ExecuteReader())
            {
                bool hasDescription = false;
                while (reader.Read())
                {
                    var colName = reader["name"]?.ToString();
                    if (string.Equals(colName, "Description", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDescription = true;
                        break;
                    }
                }

                if (!hasDescription)
                {
                    using var alter = new SqliteCommand("ALTER TABLE Contracts ADD COLUMN Description TEXT;", connection);
                    alter.ExecuteNonQuery();
                }
            }

            // ---- Migration: add TransactionID to Payments if missing
            using (var pragma2 = new SqliteCommand("PRAGMA table_info(Payments);", connection))
            using (var reader2 = pragma2.ExecuteReader())
            {
                bool hasTransactionID = false;
                while (reader2.Read())
                {
                    var colName = reader2["name"]?.ToString();
                    if (string.Equals(colName, "TransactionID", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTransactionID = true;
                        break;
                    }
                }

                if (!hasTransactionID)
                {
                    using var alter = new SqliteCommand("ALTER TABLE Payments ADD COLUMN TransactionID TEXT;", connection);
                    alter.ExecuteNonQuery();
                }
            }
        }

        // ========== CONTRACTS ==========
        public async Task<List<Contracts>> GetAllContracts()
        {
            var list = new List<Contracts>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqliteCommand("SELECT * FROM Contracts", conn);
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
                    DateCreated = ParseDateOrDefault(reader["DateCreated"]),
                    DeliveryNotes = reader["DeliveryNotes"].ToString(),
                    PaymentNotes = reader["PaymentNotes"].ToString(),
                    Description = reader["Description"] is DBNull ? null : reader["Description"]?.ToString()
                });

            }
            return list;
        }

        public async Task AddContract(Contracts c)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
    INSERT INTO Contracts (
        ContractID, GinnerID, MillID, TotalBales, PricePerBatch, TotalAmount,
        CommissionPercentage, DateCreated, DeliveryNotes, PaymentNotes, Description
    )
    VALUES (
        @ContractID, @GinnerID, @MillID, @TotalBales, @PricePerBatch, @TotalAmount,
        @CommissionPercentage, @DateCreated, @DeliveryNotes, @PaymentNotes, @Description
    )", conn);

            cmd.Parameters.AddWithValue("@ContractID", c.ContractID);
            cmd.Parameters.AddWithValue("@GinnerID", c.GinnerID);
            cmd.Parameters.AddWithValue("@MillID", c.MillID);
            cmd.Parameters.AddWithValue("@TotalBales", c.TotalBales);
            cmd.Parameters.AddWithValue("@PricePerBatch", c.PricePerBatch);
            cmd.Parameters.AddWithValue("@TotalAmount", c.TotalAmount);
            cmd.Parameters.AddWithValue("@CommissionPercentage", c.CommissionPercentage);
            cmd.Parameters.AddWithValue("@DateCreated", c.DateCreated.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryNotes", (object?)c.DeliveryNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PaymentNotes", (object?)c.PaymentNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)c.Description ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateContract(Contracts c)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
    UPDATE Contracts SET
        GinnerID=@GinnerID,
        MillID=@MillID,
        TotalBales=@TotalBales,
        PricePerBatch=@PricePerBatch,
        TotalAmount=@TotalAmount,
        CommissionPercentage=@CommissionPercentage,
        DateCreated=@DateCreated,
        DeliveryNotes=@DeliveryNotes,
        PaymentNotes=@PaymentNotes,
        Description=@Description
    WHERE ContractID=@ContractID", conn);

            cmd.Parameters.AddWithValue("@ContractID", c.ContractID);
            cmd.Parameters.AddWithValue("@GinnerID", c.GinnerID);
            cmd.Parameters.AddWithValue("@MillID", c.MillID);
            cmd.Parameters.AddWithValue("@TotalBales", c.TotalBales);
            cmd.Parameters.AddWithValue("@PricePerBatch", c.PricePerBatch);
            cmd.Parameters.AddWithValue("@TotalAmount", c.TotalAmount);
            cmd.Parameters.AddWithValue("@CommissionPercentage", c.CommissionPercentage);
            cmd.Parameters.AddWithValue("@DateCreated", c.DateCreated.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryNotes", (object?)c.DeliveryNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PaymentNotes", (object?)c.PaymentNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)c.Description ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteContract(string contractId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Contracts WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== DELIVERIES ==========
        public async Task<List<Deliveries>> GetAllDeliveries()
        {
            var list = new List<Deliveries>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqliteCommand("SELECT * FROM Deliveries", conn);
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
                    DepartureDate = ParseDateOrDefault(reader["DepartureDate"]),
                    DeliveryDate = ParseDateOrDefault(reader["DeliveryDate"])
                });
            }
            return list;
        }

        public async Task AddDelivery(Deliveries d)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Deliveries WHERE DeliveryID = @id", conn);
            cmd.Parameters.AddWithValue("@id", deliveryId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== PAYMENTS ==========
        public async Task<List<Payment>> GetAllPayments()
        {
            var list = new List<Payment>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqliteCommand("SELECT * FROM Payments", conn);
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
                    Date = ParseDateOrDefault(reader["Date"]),
                    TransactionID = reader["TransactionID"] is DBNull ? "" : reader["TransactionID"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public async Task AddPayment(Payment p)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
                INSERT INTO Payments (PaymentID, ContractID, TotalAmount, AmountPaid, TotalBales, Date, TransactionID)
                VALUES (@PaymentID, @ContractID, @TotalAmount, @AmountPaid, @TotalBales, @Date, @TransactionID)", conn);

            cmd.Parameters.AddWithValue("@PaymentID", p.PaymentID);
            cmd.Parameters.AddWithValue("@ContractID", p.ContractID);
            cmd.Parameters.AddWithValue("@TotalAmount", p.TotalAmount);
            cmd.Parameters.AddWithValue("@AmountPaid", p.AmountPaid);
            cmd.Parameters.AddWithValue("@TotalBales", p.TotalBales);
            cmd.Parameters.AddWithValue("@Date", p.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@TransactionID", (object?)p.TransactionID ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePayment(Payment p)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
                UPDATE Payments SET ContractID=@ContractID, TotalAmount=@TotalAmount, AmountPaid=@AmountPaid, TotalBales=@TotalBales, Date=@Date, TransactionID=@TransactionID
                WHERE PaymentID=@PaymentID", conn);

            cmd.Parameters.AddWithValue("@PaymentID", p.PaymentID);
            cmd.Parameters.AddWithValue("@ContractID", p.ContractID);
            cmd.Parameters.AddWithValue("@TotalAmount", p.TotalAmount);
            cmd.Parameters.AddWithValue("@AmountPaid", p.AmountPaid);
            cmd.Parameters.AddWithValue("@TotalBales", p.TotalBales);
            cmd.Parameters.AddWithValue("@Date", p.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@TransactionID", (object?)p.TransactionID ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeletePayment(string paymentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Payments WHERE PaymentID = @id", conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== GINNERS ==========
        public async Task<List<Ginners>> GetAllGinners()
        {
            var list = new List<Ginners>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqliteCommand("SELECT * FROM Ginners", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Ginners
                {
                    GinnerID = reader["GinnerID"].ToString(),
                    GinnerName = reader["GinnerName"].ToString(),
                    Contact = reader["Contact"].ToString(),
                    IBAN = reader["IBAN"].ToString(),
                    Address = reader["Address"].ToString(),
                    NTN = reader["NTN"].ToString(),
                    STN = reader["STN"].ToString(),
                    BankAddress = reader["BankAddress"].ToString(),
                    ContactPerson = reader["ContactPerson"].ToString(),
                    Station = reader["Station"].ToString()
                });
            }
            return list;
        }

        public async Task AddGinner(Ginners g)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
                INSERT INTO Ginners (GinnerID, GinnerName, Contact, IBAN, Address, NTN, STN, BankAddress, ContactPerson, Station)
                VALUES (@GinnerID, @GinnerName, @Contact, @IBAN, @Address, @NTN, @STN, @BankAddress, @ContactPerson, @Station)", conn);

            cmd.Parameters.AddWithValue("@GinnerID", g.GinnerID);
            cmd.Parameters.AddWithValue("@GinnerName", g.GinnerName);
            cmd.Parameters.AddWithValue("@Contact", g.Contact);
            cmd.Parameters.AddWithValue("@IBAN", g.IBAN);
            cmd.Parameters.AddWithValue("@Address", g.Address);
            cmd.Parameters.AddWithValue("@NTN", g.NTN);
            cmd.Parameters.AddWithValue("@STN", g.STN);
            cmd.Parameters.AddWithValue("@BankAddress", g.BankAddress);
            cmd.Parameters.AddWithValue("@ContactPerson", g.ContactPerson);
            cmd.Parameters.AddWithValue("@Station", g.Station);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateGinner(Ginners g)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
                UPDATE Ginners SET GinnerName=@GinnerName, Contact=@Contact, IBAN=@IBAN, Address=@Address, NTN=@NTN, STN=@STN, BankAddress=@BankAddress, ContactPerson=@ContactPerson, Station=@Station
                WHERE GinnerID=@GinnerID", conn);

            cmd.Parameters.AddWithValue("@GinnerID", g.GinnerID);
            cmd.Parameters.AddWithValue("@GinnerName", g.GinnerName);
            cmd.Parameters.AddWithValue("@Contact", g.Contact);
            cmd.Parameters.AddWithValue("@IBAN", g.IBAN);
            cmd.Parameters.AddWithValue("@Address", g.Address);
            cmd.Parameters.AddWithValue("@NTN", g.NTN);
            cmd.Parameters.AddWithValue("@STN", g.STN);
            cmd.Parameters.AddWithValue("@BankAddress", g.BankAddress);
            cmd.Parameters.AddWithValue("@ContactPerson", g.ContactPerson);
            cmd.Parameters.AddWithValue("@Station", g.Station);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteGinner(string GinnerID)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Ginners WHERE GinnerID = @GinnerID", conn);
            cmd.Parameters.AddWithValue("@GinnerID", GinnerID);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== MILLS ==========
        public async Task<List<Mills>> GetAllMills()
        {
            var list = new List<Mills>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqliteCommand("SELECT * FROM Mills", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Mills
                {
                    MillName = reader["MillName"].ToString(),
                    MillID = reader["MillID"].ToString(),
                    Address = reader["Address"].ToString(),
                    OwnerName = reader["OwnerName"].ToString()
                });
            }
            return list;
        }

        public async Task AddMill(Mills m)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("INSERT INTO Mills (MillName, MillID, Address, OwnerName) VALUES (@MillName, @MillID, @Address, @OwnerName)", conn);
            cmd.Parameters.AddWithValue("@MillName", m.MillName);
            cmd.Parameters.AddWithValue("@MillID", m.MillID);
            cmd.Parameters.AddWithValue("@Address", m.Address);
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateMill(Mills m)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("UPDATE Mills SET MillName = @MillName, Address=@Address, OwnerName=@OwnerName WHERE MillID=@MillID", conn);
            cmd.Parameters.AddWithValue("@MillID", m.MillID);
            cmd.Parameters.AddWithValue("@MillName", m.MillName);
            cmd.Parameters.AddWithValue("@Address", m.Address);
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteMill(string millId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Mills WHERE MillID = @MillID", conn);
            cmd.Parameters.AddWithValue("@MillID", millId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
