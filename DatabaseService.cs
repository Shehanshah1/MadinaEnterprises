using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            _connectionString = $"Data Source={_databasePath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();

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
            PaymentNotes TEXT,
            Description TEXT   -- NEW (nullable)
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
            ContractID TEXT,
            DealID TEXT,
            AmountPaid REAL,
            DatePaid TEXT,
            MillsDueTo TEXT,
            PRIMARY KEY (ContractID, DealID)
        );

        CREATE TABLE IF NOT EXISTS Mills (
            MillName TEXT,
            MillID TEXT PRIMARY KEY,
            Address TEXT,
            OwnerName TEXT
        );

        CREATE TABLE IF NOT EXISTS Users (
            UserID TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Email TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
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
                    DateCreated = DateTime.Parse(reader["DateCreated"].ToString()),
                    DeliveryNotes = reader["DeliveryNotes"].ToString(),
                    PaymentNotes = reader["PaymentNotes"].ToString(),
                    Description = reader["Description"] is DBNull ? null : reader["Description"]?.ToString()   // NEW
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
            cmd.Parameters.AddWithValue("@Description", (object?)c.Description ?? DBNull.Value);   // NEW

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
            cmd.Parameters.AddWithValue("@Description", (object?)c.Description ?? DBNull.Value);   // NEW

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
                    DepartureDate = DateTime.Parse(reader["DepartureDate"].ToString()),
                    DeliveryDate = DateTime.Parse(reader["DeliveryDate"].ToString())
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
                    Date = DateTime.Parse(reader["Date"].ToString())
                });
            }
            return list;
        }

        public async Task AddPayment(Payment p)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
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
        public async Task<List<GinnerLedger>> GetAllGinnerLedger()
        {
            var ginnerLedgerList = new List<GinnerLedger>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var command = new SqliteCommand("SELECT * FROM GinnerLedger", conn);
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM GinnerLedger WHERE ContractID = @ContractID AND DealID = @DealID", conn);
            cmd.Parameters.AddWithValue("@ContractID", contractId);
            cmd.Parameters.AddWithValue("@DealID", dealId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== USERS / AUTH ==========
        public async Task<bool> RegisterUser(string name, string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await UserExists(normalizedEmail))
            {
                return false;
            }

            var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var hash = HashPassword(password, salt);

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand(@"INSERT INTO Users (UserID, Name, Email, PasswordHash, CreatedAt)
                                         VALUES (@UserID, @Name, @Email, @PasswordHash, @CreatedAt)", conn);
            cmd.Parameters.AddWithValue("@UserID", Guid.NewGuid().ToString("N"));
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@Email", normalizedEmail);
            cmd.Parameters.AddWithValue("@PasswordHash", $"{salt}:{hash}");
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> ValidateUserCredentials(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("SELECT PasswordHash FROM Users WHERE Email = @Email LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@Email", normalizedEmail);

            var result = await cmd.ExecuteScalarAsync();
            if (result is null || result == DBNull.Value)
            {
                return false;
            }

            var stored = result.ToString() ?? string.Empty;
            var parts = stored.Split(':', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            var computed = HashPassword(password, parts[0]);
            return string.Equals(computed, parts[1], StringComparison.Ordinal);
        }

        public async Task<bool> UserExists(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("SELECT COUNT(1) FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", normalizedEmail);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        private static string HashPassword(string password, string salt)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
            return Convert.ToHexString(bytes);
        }


    }
}
