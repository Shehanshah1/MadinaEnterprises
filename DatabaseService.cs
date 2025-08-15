using DocumentFormat.OpenXml.Office.Word;
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
        private static readonly object _lock = new object();

        public DatabaseService()
        {
            _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "madina.db3");
            // Add connection pooling and journal mode to prevent locking
            _connectionString = $"Data Source={_databasePath};Version=3;Pooling=True;Max Pool Size=100;Journal Mode=WAL;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_databasePath))
                        SQLiteConnection.CreateFile(_databasePath);

                    using var connection = new SQLiteConnection(_connectionString);
                    connection.Open();

                    // Enable Write-Ahead Logging to prevent locking
                    using (var pragmaCmd = new SQLiteCommand("PRAGMA journal_mode=WAL;", connection))
                    {
                        pragmaCmd.ExecuteNonQuery();
                    }

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
                        );";

                    command.ExecuteNonQuery();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                    throw;
                }
            }
        }

        private async Task<SQLiteConnection> GetConnectionAsync()
        {
            var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        // ========== CONTRACTS ==========
        public async Task<List<Contracts>> GetAllContracts()
        {
            var list = new List<Contracts>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Contracts", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Contracts
                {
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    GinnerID = reader["GinnerID"]?.ToString() ?? "",
                    MillID = reader["MillID"]?.ToString() ?? "",
                    TotalBales = Convert.ToInt32(reader["TotalBales"] ?? 0),
                    PricePerBatch = Convert.ToDouble(reader["PricePerBatch"] ?? 0),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"] ?? 0),
                    CommissionPercentage = Convert.ToDouble(reader["CommissionPercentage"] ?? 0),
                    DateCreated = DateTime.TryParse(reader["DateCreated"]?.ToString(), out var date) ? date : DateTime.Now,
                    DeliveryNotes = reader["DeliveryNotes"]?.ToString() ?? "",
                    PaymentNotes = reader["PaymentNotes"]?.ToString() ?? ""
                });
            }
            conn.Close();
            return list;
        }

        public async Task<Contracts?> GetContractById(string contractId)
        {
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Contracts WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var contract = new Contracts
                {
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    GinnerID = reader["GinnerID"]?.ToString() ?? "",
                    MillID = reader["MillID"]?.ToString() ?? "",
                    TotalBales = Convert.ToInt32(reader["TotalBales"] ?? 0),
                    PricePerBatch = Convert.ToDouble(reader["PricePerBatch"] ?? 0),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"] ?? 0),
                    CommissionPercentage = Convert.ToDouble(reader["CommissionPercentage"] ?? 0),
                    DateCreated = DateTime.TryParse(reader["DateCreated"]?.ToString(), out var date) ? date : DateTime.Now,
                    DeliveryNotes = reader["DeliveryNotes"]?.ToString() ?? "",
                    PaymentNotes = reader["PaymentNotes"]?.ToString() ?? ""
                };
                conn.Close();
                return contract;
            }
            conn.Close();
            return null;
        }

        public async Task AddContract(Contracts c)
        {
            // Calculate total amount if not set
            if (c.TotalAmount == 0 && c.TotalBales > 0 && c.PricePerBatch > 0)
            {
                c.TotalAmount = c.TotalBales * c.PricePerBatch;
            }

            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Contracts (ContractID, GinnerID, MillID, TotalBales, PricePerBatch, TotalAmount, CommissionPercentage, DateCreated, DeliveryNotes, PaymentNotes)
                VALUES (@ContractID, @GinnerID, @MillID, @TotalBales, @PricePerBatch, @TotalAmount, @CommissionPercentage, @DateCreated, @DeliveryNotes, @PaymentNotes)", conn);

            cmd.Parameters.AddWithValue("@ContractID", c.ContractID ?? "");
            cmd.Parameters.AddWithValue("@GinnerID", c.GinnerID ?? "");
            cmd.Parameters.AddWithValue("@MillID", c.MillID ?? "");
            cmd.Parameters.AddWithValue("@TotalBales", c.TotalBales);
            cmd.Parameters.AddWithValue("@PricePerBatch", c.PricePerBatch);
            cmd.Parameters.AddWithValue("@TotalAmount", c.TotalAmount);
            cmd.Parameters.AddWithValue("@CommissionPercentage", c.CommissionPercentage);
            cmd.Parameters.AddWithValue("@DateCreated", c.DateCreated.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryNotes", c.DeliveryNotes ?? "");
            cmd.Parameters.AddWithValue("@PaymentNotes", c.PaymentNotes ?? "");

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task UpdateContract(Contracts c)
        {
            // Recalculate total amount
            if (c.TotalBales > 0 && c.PricePerBatch > 0)
            {
                c.TotalAmount = c.TotalBales * c.PricePerBatch;
            }

            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Contracts SET GinnerID=@GinnerID, MillID=@MillID, TotalBales=@TotalBales, PricePerBatch=@PricePerBatch, TotalAmount=@TotalAmount,
                CommissionPercentage=@CommissionPercentage, DateCreated=@DateCreated, DeliveryNotes=@DeliveryNotes, PaymentNotes=@PaymentNotes
                WHERE ContractID=@ContractID", conn);

            cmd.Parameters.AddWithValue("@ContractID", c.ContractID ?? "");
            cmd.Parameters.AddWithValue("@GinnerID", c.GinnerID ?? "");
            cmd.Parameters.AddWithValue("@MillID", c.MillID ?? "");
            cmd.Parameters.AddWithValue("@TotalBales", c.TotalBales);
            cmd.Parameters.AddWithValue("@PricePerBatch", c.PricePerBatch);
            cmd.Parameters.AddWithValue("@TotalAmount", c.TotalAmount);
            cmd.Parameters.AddWithValue("@CommissionPercentage", c.CommissionPercentage);
            cmd.Parameters.AddWithValue("@DateCreated", c.DateCreated.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryNotes", c.DeliveryNotes ?? "");
            cmd.Parameters.AddWithValue("@PaymentNotes", c.PaymentNotes ?? "");

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task DeleteContract(string contractId)
        {
            using var conn = await GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Delete related records first (maintain referential integrity)
                var deleteDeliveries = new SQLiteCommand("DELETE FROM Deliveries WHERE ContractID = @id", conn, transaction);
                deleteDeliveries.Parameters.AddWithValue("@id", contractId);
                await deleteDeliveries.ExecuteNonQueryAsync();

                var deletePayments = new SQLiteCommand("DELETE FROM Payments WHERE ContractID = @id", conn, transaction);
                deletePayments.Parameters.AddWithValue("@id", contractId);
                await deletePayments.ExecuteNonQueryAsync();

                var deleteLedger = new SQLiteCommand("DELETE FROM GinnerLedger WHERE ContractID = @id", conn, transaction);
                deleteLedger.Parameters.AddWithValue("@id", contractId);
                await deleteLedger.ExecuteNonQueryAsync();

                var cmd = new SQLiteCommand("DELETE FROM Contracts WHERE ContractID = @id", conn, transaction);
                cmd.Parameters.AddWithValue("@id", contractId);
                await cmd.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                conn.Close();
            }
        }

        // ========== DELIVERIES ==========
        public async Task<List<Deliveries>> GetAllDeliveries()
        {
            var list = new List<Deliveries>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Deliveries", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Deliveries
                {
                    DeliveryID = reader["DeliveryID"]?.ToString() ?? "",
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    Amount = Convert.ToDouble(reader["Amount"] ?? 0),
                    TotalBales = Convert.ToInt32(reader["TotalBales"] ?? 0),
                    FactoryWeight = Convert.ToDouble(reader["FactoryWeight"] ?? 0),
                    MillWeight = Convert.ToDouble(reader["MillWeight"] ?? 0),
                    TruckNumber = reader["TruckNumber"]?.ToString() ?? "",
                    DriverContact = reader["DriverContact"]?.ToString() ?? "",
                    DepartureDate = DateTime.TryParse(reader["DepartureDate"]?.ToString(), out var depDate) ? depDate : DateTime.Now,
                    DeliveryDate = DateTime.TryParse(reader["DeliveryDate"]?.ToString(), out var delDate) ? delDate : DateTime.Now
                });
            }
            conn.Close();
            return list;
        }

        public async Task<List<Deliveries>> GetDeliveriesByContractId(string contractId)
        {
            var list = new List<Deliveries>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Deliveries WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Deliveries
                {
                    DeliveryID = reader["DeliveryID"]?.ToString() ?? "",
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    Amount = Convert.ToDouble(reader["Amount"] ?? 0),
                    TotalBales = Convert.ToInt32(reader["TotalBales"] ?? 0),
                    FactoryWeight = Convert.ToDouble(reader["FactoryWeight"] ?? 0),
                    MillWeight = Convert.ToDouble(reader["MillWeight"] ?? 0),
                    TruckNumber = reader["TruckNumber"]?.ToString() ?? "",
                    DriverContact = reader["DriverContact"]?.ToString() ?? "",
                    DepartureDate = DateTime.TryParse(reader["DepartureDate"]?.ToString(), out var depDate) ? depDate : DateTime.Now,
                    DeliveryDate = DateTime.TryParse(reader["DeliveryDate"]?.ToString(), out var delDate) ? delDate : DateTime.Now
                });
            }
            conn.Close();
            return list;
        }

        public async Task AddDelivery(Deliveries d)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Deliveries (DeliveryID, ContractID, Amount, TotalBales, FactoryWeight, MillWeight, TruckNumber, DriverContact, DepartureDate, DeliveryDate)
                VALUES (@DeliveryID, @ContractID, @Amount, @TotalBales, @FactoryWeight, @MillWeight, @TruckNumber, @DriverContact, @DepartureDate, @DeliveryDate)", conn);

            cmd.Parameters.AddWithValue("@DeliveryID", d.DeliveryID ?? "");
            cmd.Parameters.AddWithValue("@ContractID", d.ContractID ?? "");
            cmd.Parameters.AddWithValue("@Amount", d.Amount);
            cmd.Parameters.AddWithValue("@TotalBales", d.TotalBales);
            cmd.Parameters.AddWithValue("@FactoryWeight", d.FactoryWeight);
            cmd.Parameters.AddWithValue("@MillWeight", d.MillWeight);
            cmd.Parameters.AddWithValue("@TruckNumber", d.TruckNumber ?? "");
            cmd.Parameters.AddWithValue("@DriverContact", d.DriverContact ?? "");
            cmd.Parameters.AddWithValue("@DepartureDate", d.DepartureDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryDate", d.DeliveryDate.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task UpdateDelivery(Deliveries d)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Deliveries SET ContractID=@ContractID, Amount=@Amount, TotalBales=@TotalBales,
                FactoryWeight=@FactoryWeight, MillWeight=@MillWeight, TruckNumber=@TruckNumber, DriverContact=@DriverContact,
                DepartureDate=@DepartureDate, DeliveryDate=@DeliveryDate
                WHERE DeliveryID=@DeliveryID", conn);

            cmd.Parameters.AddWithValue("@DeliveryID", d.DeliveryID ?? "");
            cmd.Parameters.AddWithValue("@ContractID", d.ContractID ?? "");
            cmd.Parameters.AddWithValue("@Amount", d.Amount);
            cmd.Parameters.AddWithValue("@TotalBales", d.TotalBales);
            cmd.Parameters.AddWithValue("@FactoryWeight", d.FactoryWeight);
            cmd.Parameters.AddWithValue("@MillWeight", d.MillWeight);
            cmd.Parameters.AddWithValue("@TruckNumber", d.TruckNumber ?? "");
            cmd.Parameters.AddWithValue("@DriverContact", d.DriverContact ?? "");
            cmd.Parameters.AddWithValue("@DepartureDate", d.DepartureDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@DeliveryDate", d.DeliveryDate.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task DeleteDelivery(string deliveryId)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand("DELETE FROM Deliveries WHERE DeliveryID = @id", conn);
            cmd.Parameters.AddWithValue("@id", deliveryId);
            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        // ========== PAYMENTS ==========
        public async Task<List<Payment>> GetAllPayments()
        {
            var list = new List<Payment>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Payments", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Payment
                {
                    PaymentID = reader["PaymentID"]?.ToString() ?? "",
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"] ?? 0),
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"] ?? 0),
                    TotalBales = Convert.ToInt32(reader["TotalBales"] ?? 0),
                    Date = DateTime.TryParse(reader["Date"]?.ToString(), out var date) ? date : DateTime.Now
                });
            }
            conn.Close();
            return list;
        }

        public async Task<List<Payment>> GetPaymentsByContractId(string contractId)
        {
            var list = new List<Payment>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Payments WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Payment
                {
                    PaymentID = reader["PaymentID"]?.ToString() ?? "",
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"] ?? 0),
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"] ?? 0),
                    TotalBales = Convert.ToInt32(reader["TotalBales"] ?? 0),
                    Date = DateTime.TryParse(reader["Date"]?.ToString(), out var date) ? date : DateTime.Now
                });
            }
            conn.Close();
            return list;
        }

        public async Task AddPayment(Payment p)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Payments (PaymentID, ContractID, TotalAmount, AmountPaid, TotalBales, Date)
                VALUES (@PaymentID, @ContractID, @TotalAmount, @AmountPaid, @TotalBales, @Date)", conn);

            cmd.Parameters.AddWithValue("@PaymentID", p.PaymentID ?? "");
            cmd.Parameters.AddWithValue("@ContractID", p.ContractID ?? "");
            cmd.Parameters.AddWithValue("@TotalAmount", p.TotalAmount);
            cmd.Parameters.AddWithValue("@AmountPaid", p.AmountPaid);
            cmd.Parameters.AddWithValue("@TotalBales", p.TotalBales);
            cmd.Parameters.AddWithValue("@Date", p.Date.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task UpdatePayment(Payment p)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Payments SET ContractID=@ContractID, TotalAmount=@TotalAmount, AmountPaid=@AmountPaid, TotalBales=@TotalBales, Date=@Date
                WHERE PaymentID=@PaymentID", conn);

            cmd.Parameters.AddWithValue("@PaymentID", p.PaymentID ?? "");
            cmd.Parameters.AddWithValue("@ContractID", p.ContractID ?? "");
            cmd.Parameters.AddWithValue("@TotalAmount", p.TotalAmount);
            cmd.Parameters.AddWithValue("@AmountPaid", p.AmountPaid);
            cmd.Parameters.AddWithValue("@TotalBales", p.TotalBales);
            cmd.Parameters.AddWithValue("@Date", p.Date.ToString("yyyy-MM-dd"));

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task DeletePayment(string paymentId)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand("DELETE FROM Payments WHERE PaymentID = @id", conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        // ========== GINNERS ==========
        public async Task<List<Ginners>> GetAllGinners()
        {
            var list = new List<Ginners>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Ginners", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Ginners
                {
                    GinnerID = reader["GinnerID"]?.ToString() ?? "",
                    GinnerName = reader["GinnerName"]?.ToString() ?? "",
                    Contact = reader["Contact"]?.ToString() ?? "",
                    IBAN = reader["IBAN"]?.ToString() ?? "",
                    Address = reader["Address"]?.ToString() ?? "",
                    NTN = reader["NTN"]?.ToString() ?? "",
                    STN = reader["STN"]?.ToString() ?? "",
                    BankAddress = reader["BankAddress"]?.ToString() ?? "",
                    ContactPerson = reader["ContactPerson"]?.ToString() ?? "",
                    Station = reader["Station"]?.ToString() ?? ""
                });
            }
            conn.Close();
            return list;
        }

        public async Task<Ginners?> GetGinnerById(string ginnerId)
        {
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Ginners WHERE GinnerID = @id", conn);
            cmd.Parameters.AddWithValue("@id", ginnerId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var ginner = new Ginners
                {
                    GinnerID = reader["GinnerID"]?.ToString() ?? "",
                    GinnerName = reader["GinnerName"]?.ToString() ?? "",
                    Contact = reader["Contact"]?.ToString() ?? "",
                    IBAN = reader["IBAN"]?.ToString() ?? "",
                    Address = reader["Address"]?.ToString() ?? "",
                    NTN = reader["NTN"]?.ToString() ?? "",
                    STN = reader["STN"]?.ToString() ?? "",
                    BankAddress = reader["BankAddress"]?.ToString() ?? "",
                    ContactPerson = reader["ContactPerson"]?.ToString() ?? "",
                    Station = reader["Station"]?.ToString() ?? ""
                };
                conn.Close();
                return ginner;
            }
            conn.Close();
            return null;
        }

        public async Task AddGinner(Ginners g)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Ginners (GinnerID, GinnerName, Contact, IBAN, Address, NTN, STN, BankAddress, ContactPerson, Station)
                VALUES (@GinnerID, @GinnerName, @Contact, @IBAN, @Address, @NTN, @STN, @BankAddress, @ContactPerson, @Station)", conn);

            cmd.Parameters.AddWithValue("@GinnerID", g.GinnerID ?? "");
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
            conn.Close();
        }

        public async Task UpdateGinner(Ginners g)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Ginners SET GinnerName=@GinnerName, Contact=@Contact, IBAN=@IBAN, Address=@Address, NTN=@NTN, STN=@STN, BankAddress=@BankAddress, ContactPerson=@ContactPerson, Station=@Station
                WHERE GinnerID=@GinnerID", conn);

            cmd.Parameters.AddWithValue("@GinnerID", g.GinnerID ?? "");
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
            conn.Close();
        }

        public async Task DeleteGinner(string ginnerId)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand("DELETE FROM Ginners WHERE GinnerID = @GinnerID", conn);
            cmd.Parameters.AddWithValue("@GinnerID", ginnerId);
            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        // ========== MILLS ==========
        public async Task<List<Mills>> GetAllMills()
        {
            var list = new List<Mills>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Mills", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Mills
                {
                    MillID = reader["MillID"]?.ToString() ?? "",
                    MillName = reader["MillName"]?.ToString() ?? "",
                    Address = reader["Address"]?.ToString() ?? "",
                    OwnerName = reader["OwnerName"]?.ToString() ?? ""
                });
            }
            conn.Close();
            return list;
        }

        public async Task<Mills?> GetMillById(string millId)
        {
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM Mills WHERE MillID = @id", conn);
            cmd.Parameters.AddWithValue("@id", millId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var mill = new Mills
                {
                    MillID = reader["MillID"]?.ToString() ?? "",
                    MillName = reader["MillName"]?.ToString() ?? "",
                    Address = reader["Address"]?.ToString() ?? "",
                    OwnerName = reader["OwnerName"]?.ToString() ?? ""
                };
                conn.Close();
                return mill;
            }
            conn.Close();
            return null;
        }

        public async Task AddMill(Mills m)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand("INSERT INTO Mills (MillID, MillName, Address, OwnerName) VALUES (@MillID, @MillName, @Address, @OwnerName)", conn);
            cmd.Parameters.AddWithValue("@MillID", m.MillID ?? "");
            cmd.Parameters.AddWithValue("@MillName", m.MillName ?? "");
            cmd.Parameters.AddWithValue("@Address", m.Address ?? "");
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName ?? "");
            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task UpdateMill(Mills m)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE Mills SET MillName=@MillName, Address=@Address, OwnerName=@OwnerName
                WHERE MillID=@MillID", conn);

            cmd.Parameters.AddWithValue("@MillID", m.MillID ?? "");
            cmd.Parameters.AddWithValue("@MillName", m.MillName ?? "");
            cmd.Parameters.AddWithValue("@Address", m.Address ?? "");
            cmd.Parameters.AddWithValue("@OwnerName", m.OwnerName ?? "");

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task DeleteMill(string millId)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand("DELETE FROM Mills WHERE MillID = @MillID", conn);
            cmd.Parameters.AddWithValue("@MillID", millId);
            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        // ========== GINNER LEDGER ==========
        public async Task<List<GinnerLedger>> GetAllGinnerLedger()
        {
            var ginnerLedgerList = new List<GinnerLedger>();
            using var conn = await GetConnectionAsync();

            var command = new SQLiteCommand("SELECT * FROM GinnerLedger", conn);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ginnerLedgerList.Add(new GinnerLedger
                {
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    DealID = reader["DealID"]?.ToString() ?? "",
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"] ?? 0),
                    DatePaid = DateTime.TryParse(reader["DatePaid"]?.ToString(), out var date) ? date : DateTime.Today,
                    MillsDueTo = reader["MillsDueTo"]?.ToString() ?? ""
                });
            }
            conn.Close();
            return ginnerLedgerList;
        }

        public async Task<List<GinnerLedger>> GetGinnerLedgerByContractId(string contractId)
        {
            var list = new List<GinnerLedger>();
            using var conn = await GetConnectionAsync();

            var cmd = new SQLiteCommand("SELECT * FROM GinnerLedger WHERE ContractID = @id", conn);
            cmd.Parameters.AddWithValue("@id", contractId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new GinnerLedger
                {
                    ContractID = reader["ContractID"]?.ToString() ?? "",
                    DealID = reader["DealID"]?.ToString() ?? "",
                    AmountPaid = Convert.ToDouble(reader["AmountPaid"] ?? 0),
                    DatePaid = DateTime.TryParse(reader["DatePaid"]?.ToString(), out var date) ? date : DateTime.Today,
                    MillsDueTo = reader["MillsDueTo"]?.ToString() ?? ""
                });
            }
            conn.Close();
            return list;
        }

        public async Task AddGinnerLedger(GinnerLedger g)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                INSERT INTO GinnerLedger (ContractID, DealID, AmountPaid, DatePaid, MillsDueTo)
                VALUES (@ContractID, @DealID, @AmountPaid, @DatePaid, @MillsDueTo)", conn);

            cmd.Parameters.AddWithValue("@ContractID", g.ContractID ?? "");
            cmd.Parameters.AddWithValue("@DealID", g.DealID ?? "");
            cmd.Parameters.AddWithValue("@AmountPaid", g.AmountPaid);
            cmd.Parameters.AddWithValue("@DatePaid", g.DatePaid.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@MillsDueTo", g.MillsDueTo ?? "");

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task UpdateGinnerLedger(GinnerLedger g)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand(@"
                UPDATE GinnerLedger SET AmountPaid=@AmountPaid, DatePaid=@DatePaid, MillsDueTo=@MillsDueTo
                WHERE ContractID=@ContractID AND DealID=@DealID", conn);

            cmd.Parameters.AddWithValue("@ContractID", g.ContractID ?? "");
            cmd.Parameters.AddWithValue("@DealID", g.DealID ?? "");
            cmd.Parameters.AddWithValue("@AmountPaid", g.AmountPaid);
            cmd.Parameters.AddWithValue("@DatePaid", g.DatePaid.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@MillsDueTo", g.MillsDueTo ?? "");

            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        public async Task DeleteGinnerLedger(string contractId, string dealId)
        {
            using var conn = await GetConnectionAsync();
            var cmd = new SQLiteCommand("DELETE FROM GinnerLedger WHERE ContractID = @ContractID AND DealID = @DealID", conn);
            cmd.Parameters.AddWithValue("@ContractID", contractId);
            cmd.Parameters.AddWithValue("@DealID", dealId);
            await cmd.ExecuteNonQueryAsync();
            conn.Close();
        }

        // ========== ANALYTICS & REPORTS ==========
        public async Task<Dictionary<string, object>> GetDashboardStatistics()
        {
            var stats = new Dictionary<string, object>();

            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting dashboard statistics: {ex.Message}");
                // Return default values if error occurs
                stats["TotalCommission"] = 0.0;
                stats["TotalPaid"] = 0.0;
                stats["TotalDue"] = 0.0;
                stats["TotalBalesSold"] = 0;
                stats["TotalBalesContracted"] = 0;
                stats["TotalGinners"] = 0;
                stats["TotalMills"] = 0;
                stats["TotalContracts"] = 0;
                stats["AvgCommissionRate"] = 0.0;
                stats["TotalDeliveries"] = 0;
                stats["TotalPayments"] = 0;
            }

            return stats;
        }

        public async Task<ContractSummary> GetContractSummary(string contractId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting contract summary: {ex.Message}");
                return new ContractSummary();
            }
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