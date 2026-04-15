using MadinaEnterprises.Modules.Models;
using MadinaEnterprises.Modules.Services;
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
        private readonly CloudSyncService _cloud;

        public CloudSyncService Cloud => _cloud;
        public string DatabasePath => _databasePath;

        public DatabaseService() : this(new CloudSyncService()) { }

        public DatabaseService(CloudSyncService cloud)
        {
            _cloud = cloud;
            _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "madina.db3");
            _connectionString = $"Data Source={_databasePath};Foreign Keys=True";
            InitializeDatabase();
        }

        /// <summary>
        /// Fire-and-forget wrapper for pushing writes to the cloud so the UI
        /// never waits on the network. Errors surface on <see cref="CloudSyncService.LastError"/>.
        /// </summary>
        private void FireAndForget(Task task)
        {
            _ = task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
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
            FireAndForget(_cloud.UpsertAsync("contracts", CloudSyncService.Row(c), "contract_id"));
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
            FireAndForget(_cloud.UpsertAsync("contracts", CloudSyncService.Row(c), "contract_id"));
        }

        public async Task DeleteContract(string contractId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Contracts WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);
            await cmd.ExecuteNonQueryAsync();
            FireAndForget(_cloud.DeleteAsync("contracts", "contract_id", contractId));
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
            FireAndForget(_cloud.UpsertAsync("deliveries", CloudSyncService.Row(d), "delivery_id"));
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
            FireAndForget(_cloud.UpsertAsync("deliveries", CloudSyncService.Row(d), "delivery_id"));
        }

        public async Task DeleteDelivery(string deliveryId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Deliveries WHERE DeliveryID = @id", conn);
            cmd.Parameters.AddWithValue("@id", deliveryId);
            await cmd.ExecuteNonQueryAsync();
            FireAndForget(_cloud.DeleteAsync("deliveries", "delivery_id", deliveryId));
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
            FireAndForget(_cloud.UpsertAsync("payments", CloudSyncService.Row(p), "payment_id"));
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
            FireAndForget(_cloud.UpsertAsync("payments", CloudSyncService.Row(p), "payment_id"));
        }

        public async Task DeletePayment(string paymentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Payments WHERE PaymentID = @id", conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            await cmd.ExecuteNonQueryAsync();
            FireAndForget(_cloud.DeleteAsync("payments", "payment_id", paymentId));
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
            FireAndForget(_cloud.UpsertAsync("ginners", CloudSyncService.Row(g), "ginner_id"));
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
            FireAndForget(_cloud.UpsertAsync("ginners", CloudSyncService.Row(g), "ginner_id"));
        }

        public async Task DeleteGinner(string GinnerID)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Ginners WHERE GinnerID = @GinnerID", conn);
            cmd.Parameters.AddWithValue("@GinnerID", GinnerID);
            await cmd.ExecuteNonQueryAsync();
            FireAndForget(_cloud.DeleteAsync("ginners", "ginner_id", GinnerID));
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
            FireAndForget(_cloud.UpsertAsync("mills", CloudSyncService.Row(m), "mill_id"));
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
            FireAndForget(_cloud.UpsertAsync("mills", CloudSyncService.Row(m), "mill_id"));
        }

        public async Task DeleteMill(string millId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqliteCommand("DELETE FROM Mills WHERE MillID = @MillID", conn);
            cmd.Parameters.AddWithValue("@MillID", millId);
            await cmd.ExecuteNonQueryAsync();
            FireAndForget(_cloud.DeleteAsync("mills", "mill_id", millId));
        }

        // ================================================================
        //                  CLOUD SYNC — pull + push-all
        // ================================================================

        /// <summary>
        /// Pulls every row from Supabase and merges it into the local SQLite database.
        /// Uses INSERT ... ON CONFLICT DO UPDATE so each row is created on first sync
        /// and updated on subsequent syncs. Existing local-only rows are preserved and
        /// pushed back to the cloud at the end.
        /// No-op (and returns false) if cloud sync is not configured.
        /// </summary>
        public async Task<bool> SyncFromCloudAsync()
        {
            if (!_cloud.IsEnabled) return false;

            // Respect FK order: parents first.
            var ginners    = await _cloud.FetchAllAsync("ginners");
            var mills      = await _cloud.FetchAllAsync("mills");
            var contracts  = await _cloud.FetchAllAsync("contracts");
            var deliveries = await _cloud.FetchAllAsync("deliveries");
            var payments   = await _cloud.FetchAllAsync("payments");

            if (ginners == null && mills == null && contracts == null &&
                deliveries == null && payments == null)
            {
                // Network or auth problem — nothing fetched.
                return false;
            }

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            if (ginners != null)
            {
                foreach (var el in ginners)
                {
                    var cmd = new SqliteCommand(@"
INSERT INTO Ginners (GinnerID, GinnerName, Contact, IBAN, Address, NTN, STN, BankAddress, ContactPerson, Station)
VALUES (@GinnerID, @GinnerName, @Contact, @IBAN, @Address, @NTN, @STN, @BankAddress, @ContactPerson, @Station)
ON CONFLICT(GinnerID) DO UPDATE SET
    GinnerName=excluded.GinnerName, Contact=excluded.Contact, IBAN=excluded.IBAN,
    Address=excluded.Address, NTN=excluded.NTN, STN=excluded.STN,
    BankAddress=excluded.BankAddress, ContactPerson=excluded.ContactPerson,
    Station=excluded.Station;", conn, tx);
                    cmd.Parameters.AddWithValue("@GinnerID", CloudSyncService.S(el, "ginner_id"));
                    cmd.Parameters.AddWithValue("@GinnerName", CloudSyncService.S(el, "ginner_name"));
                    cmd.Parameters.AddWithValue("@Contact", CloudSyncService.S(el, "contact"));
                    cmd.Parameters.AddWithValue("@IBAN", CloudSyncService.S(el, "iban"));
                    cmd.Parameters.AddWithValue("@Address", CloudSyncService.S(el, "address"));
                    cmd.Parameters.AddWithValue("@NTN", CloudSyncService.S(el, "ntn"));
                    cmd.Parameters.AddWithValue("@STN", CloudSyncService.S(el, "stn"));
                    cmd.Parameters.AddWithValue("@BankAddress", CloudSyncService.S(el, "bank_address"));
                    cmd.Parameters.AddWithValue("@ContactPerson", CloudSyncService.S(el, "contact_person"));
                    cmd.Parameters.AddWithValue("@Station", CloudSyncService.S(el, "station"));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (mills != null)
            {
                foreach (var el in mills)
                {
                    var cmd = new SqliteCommand(@"
INSERT INTO Mills (MillID, MillName, Address, OwnerName)
VALUES (@MillID, @MillName, @Address, @OwnerName)
ON CONFLICT(MillID) DO UPDATE SET
    MillName=excluded.MillName, Address=excluded.Address, OwnerName=excluded.OwnerName;", conn, tx);
                    cmd.Parameters.AddWithValue("@MillID", CloudSyncService.S(el, "mill_id"));
                    cmd.Parameters.AddWithValue("@MillName", CloudSyncService.S(el, "mill_name"));
                    cmd.Parameters.AddWithValue("@Address", CloudSyncService.S(el, "address"));
                    cmd.Parameters.AddWithValue("@OwnerName", CloudSyncService.S(el, "owner_name"));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (contracts != null)
            {
                foreach (var el in contracts)
                {
                    var cmd = new SqliteCommand(@"
INSERT INTO Contracts (ContractID, GinnerID, MillID, TotalBales, PricePerBatch, TotalAmount,
    CommissionPercentage, DateCreated, DeliveryNotes, PaymentNotes, Description)
VALUES (@ContractID, @GinnerID, @MillID, @TotalBales, @PricePerBatch, @TotalAmount,
    @CommissionPercentage, @DateCreated, @DeliveryNotes, @PaymentNotes, @Description)
ON CONFLICT(ContractID) DO UPDATE SET
    GinnerID=excluded.GinnerID, MillID=excluded.MillID, TotalBales=excluded.TotalBales,
    PricePerBatch=excluded.PricePerBatch, TotalAmount=excluded.TotalAmount,
    CommissionPercentage=excluded.CommissionPercentage, DateCreated=excluded.DateCreated,
    DeliveryNotes=excluded.DeliveryNotes, PaymentNotes=excluded.PaymentNotes,
    Description=excluded.Description;", conn, tx);
                    cmd.Parameters.AddWithValue("@ContractID", CloudSyncService.S(el, "contract_id"));
                    cmd.Parameters.AddWithValue("@GinnerID", CloudSyncService.S(el, "ginner_id"));
                    cmd.Parameters.AddWithValue("@MillID", CloudSyncService.S(el, "mill_id"));
                    cmd.Parameters.AddWithValue("@TotalBales", CloudSyncService.I(el, "total_bales"));
                    cmd.Parameters.AddWithValue("@PricePerBatch", CloudSyncService.D(el, "price_per_batch"));
                    cmd.Parameters.AddWithValue("@TotalAmount", CloudSyncService.D(el, "total_amount"));
                    cmd.Parameters.AddWithValue("@CommissionPercentage", CloudSyncService.D(el, "commission_percentage"));
                    cmd.Parameters.AddWithValue("@DateCreated", CloudSyncService.S(el, "date_created"));
                    cmd.Parameters.AddWithValue("@DeliveryNotes", (object?)CloudSyncService.SNullable(el, "delivery_notes") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaymentNotes", (object?)CloudSyncService.SNullable(el, "payment_notes") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object?)CloudSyncService.SNullable(el, "description") ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (deliveries != null)
            {
                foreach (var el in deliveries)
                {
                    var cmd = new SqliteCommand(@"
INSERT INTO Deliveries (DeliveryID, ContractID, Amount, TotalBales, FactoryWeight, MillWeight,
    TruckNumber, DriverContact, DepartureDate, DeliveryDate)
VALUES (@DeliveryID, @ContractID, @Amount, @TotalBales, @FactoryWeight, @MillWeight,
    @TruckNumber, @DriverContact, @DepartureDate, @DeliveryDate)
ON CONFLICT(DeliveryID) DO UPDATE SET
    ContractID=excluded.ContractID, Amount=excluded.Amount, TotalBales=excluded.TotalBales,
    FactoryWeight=excluded.FactoryWeight, MillWeight=excluded.MillWeight,
    TruckNumber=excluded.TruckNumber, DriverContact=excluded.DriverContact,
    DepartureDate=excluded.DepartureDate, DeliveryDate=excluded.DeliveryDate;", conn, tx);
                    cmd.Parameters.AddWithValue("@DeliveryID", CloudSyncService.S(el, "delivery_id"));
                    cmd.Parameters.AddWithValue("@ContractID", CloudSyncService.S(el, "contract_id"));
                    cmd.Parameters.AddWithValue("@Amount", CloudSyncService.D(el, "amount"));
                    cmd.Parameters.AddWithValue("@TotalBales", CloudSyncService.I(el, "total_bales"));
                    cmd.Parameters.AddWithValue("@FactoryWeight", CloudSyncService.D(el, "factory_weight"));
                    cmd.Parameters.AddWithValue("@MillWeight", CloudSyncService.D(el, "mill_weight"));
                    cmd.Parameters.AddWithValue("@TruckNumber", CloudSyncService.S(el, "truck_number"));
                    cmd.Parameters.AddWithValue("@DriverContact", CloudSyncService.S(el, "driver_contact"));
                    cmd.Parameters.AddWithValue("@DepartureDate", CloudSyncService.S(el, "departure_date"));
                    cmd.Parameters.AddWithValue("@DeliveryDate", CloudSyncService.S(el, "delivery_date"));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (payments != null)
            {
                foreach (var el in payments)
                {
                    var cmd = new SqliteCommand(@"
INSERT INTO Payments (PaymentID, ContractID, TotalAmount, AmountPaid, TotalBales, Date, TransactionID)
VALUES (@PaymentID, @ContractID, @TotalAmount, @AmountPaid, @TotalBales, @Date, @TransactionID)
ON CONFLICT(PaymentID) DO UPDATE SET
    ContractID=excluded.ContractID, TotalAmount=excluded.TotalAmount, AmountPaid=excluded.AmountPaid,
    TotalBales=excluded.TotalBales, Date=excluded.Date, TransactionID=excluded.TransactionID;", conn, tx);
                    cmd.Parameters.AddWithValue("@PaymentID", CloudSyncService.S(el, "payment_id"));
                    cmd.Parameters.AddWithValue("@ContractID", CloudSyncService.S(el, "contract_id"));
                    cmd.Parameters.AddWithValue("@TotalAmount", CloudSyncService.D(el, "total_amount"));
                    cmd.Parameters.AddWithValue("@AmountPaid", CloudSyncService.D(el, "amount_paid"));
                    cmd.Parameters.AddWithValue("@TotalBales", CloudSyncService.I(el, "total_bales"));
                    cmd.Parameters.AddWithValue("@Date", CloudSyncService.S(el, "date"));
                    cmd.Parameters.AddWithValue("@TransactionID", (object?)CloudSyncService.SNullable(el, "transaction_id") ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            tx.Commit();

            // After pulling, push any local rows the cloud didn't have yet so both sides converge.
            await PushAllToCloudAsync();

            _cloud.MarkSynced();
            return true;
        }

        /// <summary>
        /// Uploads every locally-stored row to Supabase via bulk upserts. Safe to call
        /// repeatedly — merge-duplicates means existing rows are just refreshed.
        /// </summary>
        public async Task<bool> PushAllToCloudAsync()
        {
            if (!_cloud.IsEnabled) return false;

            var ginners    = await GetAllGinners();
            var mills      = await GetAllMills();
            var contracts  = await GetAllContracts();
            var deliveries = await GetAllDeliveries();
            var payments   = await GetAllPayments();

            await _cloud.UpsertManyAsync("ginners",    ginners.ConvertAll(CloudSyncService.Row),    "ginner_id");
            await _cloud.UpsertManyAsync("mills",      mills.ConvertAll(CloudSyncService.Row),      "mill_id");
            await _cloud.UpsertManyAsync("contracts",  contracts.ConvertAll(CloudSyncService.Row),  "contract_id");
            await _cloud.UpsertManyAsync("deliveries", deliveries.ConvertAll(CloudSyncService.Row), "delivery_id");
            await _cloud.UpsertManyAsync("payments",   payments.ConvertAll(CloudSyncService.Row),   "payment_id");

            _cloud.MarkSynced();
            return true;
        }
    }
}
