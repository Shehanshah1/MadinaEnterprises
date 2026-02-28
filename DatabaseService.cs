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
            IsAdmin INTEGER NOT NULL DEFAULT 0,
            IsEmailVerified INTEGER NOT NULL DEFAULT 0,
            IsApproved INTEGER NOT NULL DEFAULT 0,
            ApprovedBy TEXT,
            VerificationCode TEXT,
            VerificationExpiresAt TEXT,
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

            EnsureUserColumns(connection);
        }

        private static void EnsureUserColumns(SqliteConnection connection)
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var pragma = new SqliteCommand("PRAGMA table_info(Users);", connection);
            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                cols.Add(reader["name"]?.ToString() ?? string.Empty);
            }

            static void AddColumnIfMissing(SqliteConnection conn, HashSet<string> existing, string column, string sql)
            {
                if (existing.Contains(column)) return;
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
                existing.Add(column);
            }

            AddColumnIfMissing(connection, cols, "IsAdmin", "ALTER TABLE Users ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(connection, cols, "IsEmailVerified", "ALTER TABLE Users ADD COLUMN IsEmailVerified INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(connection, cols, "IsApproved", "ALTER TABLE Users ADD COLUMN IsApproved INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(connection, cols, "ApprovedBy", "ALTER TABLE Users ADD COLUMN ApprovedBy TEXT;");
            AddColumnIfMissing(connection, cols, "VerificationCode", "ALTER TABLE Users ADD COLUMN VerificationCode TEXT;");
            AddColumnIfMissing(connection, cols, "VerificationExpiresAt", "ALTER TABLE Users ADD COLUMN VerificationExpiresAt TEXT;");
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
        public sealed class RegistrationOutcome
        {
            public bool Success { get; set; }
            public bool IsFirstAdmin { get; set; }
            public bool NeedsAdminApproval { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public sealed class LoginValidationOutcome
        {
            public bool Success { get; set; }
            public bool IsAdmin { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public async Task<RegistrationOutcome> RegisterUser(string name, string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await UserExists(normalizedEmail))
            {
                return new RegistrationOutcome { Success = false, ErrorMessage = "This email is already in use." };
            }

            var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var hash = HashPassword(password, salt);
            var verificationCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var countCmd = new SqliteCommand("SELECT COUNT(1) FROM Users", conn);
            var userCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            var isFirstAdmin = userCount == 0;

            var cmd = new SqliteCommand(@"INSERT INTO Users (UserID, Name, Email, PasswordHash, IsAdmin, IsEmailVerified, IsApproved, ApprovedBy, VerificationCode, VerificationExpiresAt, CreatedAt)
                                         VALUES (@UserID, @Name, @Email, @PasswordHash, @IsAdmin, @IsEmailVerified, @IsApproved, @ApprovedBy, @VerificationCode, @VerificationExpiresAt, @CreatedAt)", conn);
            cmd.Parameters.AddWithValue("@UserID", Guid.NewGuid().ToString("N"));
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@Email", normalizedEmail);
            cmd.Parameters.AddWithValue("@PasswordHash", $"{salt}:{hash}");
            cmd.Parameters.AddWithValue("@IsAdmin", isFirstAdmin ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsEmailVerified", 0);
            cmd.Parameters.AddWithValue("@IsApproved", isFirstAdmin ? 1 : 0);
            cmd.Parameters.AddWithValue("@ApprovedBy", isFirstAdmin ? normalizedEmail : DBNull.Value);
            cmd.Parameters.AddWithValue("@VerificationCode", verificationCode);
            cmd.Parameters.AddWithValue("@VerificationExpiresAt", DateTime.UtcNow.AddMinutes(15).ToString("O"));
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

            await cmd.ExecuteNonQueryAsync();

            return new RegistrationOutcome
            {
                Success = true,
                IsFirstAdmin = isFirstAdmin,
                NeedsAdminApproval = !isFirstAdmin
            };
        }

        public async Task<string?> GetVerificationCodeForEmail(string email)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("SELECT VerificationCode FROM Users WHERE Email=@Email LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task<bool> VerifyEmailCode(string email, string code)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var select = new SqliteCommand("SELECT VerificationCode, VerificationExpiresAt, IsAdmin FROM Users WHERE Email = @Email LIMIT 1", conn);
            select.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());
            using var reader = await select.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return false;

            var storedCode = reader["VerificationCode"]?.ToString() ?? string.Empty;
            var expiresAtRaw = reader["VerificationExpiresAt"]?.ToString();
            var isAdmin = Convert.ToInt32(reader["IsAdmin"]) == 1;
            if (string.IsNullOrWhiteSpace(storedCode) || !string.Equals(storedCode, code.Trim(), StringComparison.Ordinal)) return false;
            if (!DateTime.TryParse(expiresAtRaw, out var expiresAt) || expiresAt < DateTime.UtcNow) return false;

            var update = new SqliteCommand("UPDATE Users SET IsEmailVerified = 1, VerificationCode = NULL, VerificationExpiresAt = NULL WHERE Email = @Email", conn);
            update.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());
            await update.ExecuteNonQueryAsync();

            if (isAdmin)
            {
                var approve = new SqliteCommand("UPDATE Users SET IsApproved = 1, ApprovedBy = Email WHERE Email = @Email", conn);
                approve.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());
                await approve.ExecuteNonQueryAsync();
            }

            return true;
        }

        public async Task<LoginValidationOutcome> ValidateUserCredentials(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("SELECT PasswordHash, IsAdmin, IsEmailVerified, IsApproved FROM Users WHERE Email = @Email LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@Email", normalizedEmail);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new LoginValidationOutcome { Success = false, ErrorMessage = "Incorrect email or password." };
            }

            var stored = reader["PasswordHash"]?.ToString() ?? string.Empty;
            var parts = stored.Split(':', 2);
            if (parts.Length != 2)
            {
                return new LoginValidationOutcome { Success = false, ErrorMessage = "Account password data is invalid." };
            }

            var computed = HashPassword(password, parts[0]);
            if (!string.Equals(computed, parts[1], StringComparison.Ordinal))
            {
                return new LoginValidationOutcome { Success = false, ErrorMessage = "Incorrect email or password." };
            }

            var isVerified = Convert.ToInt32(reader["IsEmailVerified"]) == 1;
            if (!isVerified)
            {
                return new LoginValidationOutcome { Success = false, ErrorMessage = "Please verify your email before logging in." };
            }

            var isApproved = Convert.ToInt32(reader["IsApproved"]) == 1;
            if (!isApproved)
            {
                return new LoginValidationOutcome { Success = false, ErrorMessage = "Your account is awaiting admin approval." };
            }

            var isAdmin = Convert.ToInt32(reader["IsAdmin"]) == 1;
            return new LoginValidationOutcome { Success = true, IsAdmin = isAdmin };
        }

        public async Task<bool> ApproveUser(string adminEmail, string targetEmail)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var isAdminCmd = new SqliteCommand("SELECT IsAdmin FROM Users WHERE Email=@Email LIMIT 1", conn);
            isAdminCmd.Parameters.AddWithValue("@Email", adminEmail.Trim().ToLowerInvariant());
            var adminVal = await isAdminCmd.ExecuteScalarAsync();
            if (adminVal is null || Convert.ToInt32(adminVal) != 1) return false;

            var approveCmd = new SqliteCommand("UPDATE Users SET IsApproved = 1, ApprovedBy = @AdminEmail WHERE Email = @TargetEmail", conn);
            approveCmd.Parameters.AddWithValue("@AdminEmail", adminEmail.Trim().ToLowerInvariant());
            approveCmd.Parameters.AddWithValue("@TargetEmail", targetEmail.Trim().ToLowerInvariant());
            return await approveCmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<string>> GetPendingApprovalEmails()
        {
            var list = new List<string>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("SELECT Email FROM Users WHERE IsEmailVerified = 1 AND IsApproved = 0 ORDER BY CreatedAt ASC", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader["Email"]?.ToString() ?? string.Empty);
            }
            return list;
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
