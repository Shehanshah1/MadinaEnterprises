using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using MadinaEnterprises.Modules.Models;
using System.Reflection;
using System.Diagnostics.Contracts;

namespace MadinaEnterprises

{
    public class DatabaseService
    {
        private const string DatabaseFileName = "MadinaEnterprises.db";
        private readonly string _databasePath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
            _connectionString = $"Data Source={_databasePath};Version=3;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            // Create the database file if it doesn't exist
            if (!File.Exists(_databasePath))
            {
                SQLiteConnection.CreateFile(_databasePath);
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    // Ginners Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Ginners (
                                            GinnerID TEXT UNIQUE PRIMARY KEY,
                                            Name TEXT NOT NULL,
                                            Contact TEXT,
                                            Address TEXT,
                                            IBAN    TEXT,
                                            NTN TEXT,
                                            STN TEXT
                                        );";
                    command.ExecuteNonQuery();

                    // Mills Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Mills (
                                            MillID TEXT UNIQUE PRIMARY KEY NOT NULL,
                                            Name TEXT NOT NULL,
                                            Contact TEXT,
                                            Address TEXT,
                                            NTN TEXT,
                                            STN TEXT
                                        );";
                    command.ExecuteNonQuery();

                    // Contracts Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Contracts (
                                            ContractID TEXT UNIQUE PRIMARY KEY NOT NULL,
                                            GinnerID TEXT NOT NULL,
                                            MillID TEXT NOT NULL,
                                            TotalBales INTEGER,
                                            PricePerBatch REAL,
                                            TotalAmount REAL,
                                            CommissionPercentage REAL DEFAULT 2.0,
                                            DateCreated DATE,
                                            DeliveryNotes TEXT,
                                            PaymentNotes TEXT,
                                            FOREIGN KEY (GinnerID) REFERENCES Ginners (GinnerID),
                                            FOREIGN KEY (MillID) REFERENCES Mills (MillID)
                                        );";
                    command.ExecuteNonQuery();

                    // Deliveries Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Deliveries (
                                            DeliveryID TEXT PRIMARY KEY UNIQUE NOT NULL,
                                            ContractID TEXT NOT NULL,
                                            TruckNumber TEXT,
                                            DriverContact TEXT,
                                            NumberOfBales INTEGER,
                                            DateBooked DATE,
                                            DateDelivered DATE,
                                            FOREIGN KEY (ContractID) REFERENCES Contracts (ContractID)
                                        );";
                    command.ExecuteNonQuery();

                    // Payments Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Payments (
                                            PaymentID TEXT UNIQUE PRIMARY KEY NOT NULL,
                                            ContractID TEXT NOT NULL,
                                            AmountPaid REAL,
                                            Date DATE,
                                            TransactionID TEXT,
                                            FOREIGN KEY (ContractID) REFERENCES Contracts (ContractID)
                                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }
        public bool AddGinner(Ginners ginner)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"INSERT INTO Ginners (Name, Contact, Address, IBAN, NTN, STN)
                                    VALUES (@Name, @Contact, @Address, @IBAN, @NTN, @STN)";
                    command.Parameters.AddWithValue("@Name", ginner.Name);
                    command.Parameters.AddWithValue("@Contact", ginner.Contact);
                    command.Parameters.AddWithValue("@Address", ginner.Address);
                    command.Parameters.AddWithValue("@IBAN", ginner.IBAN);
                    command.Parameters.AddWithValue("@NTN", ginner.NTN);
                    command.Parameters.AddWithValue("@STN", ginner.STN);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<Ginners> GetAllGinners()
        {
            var ginners = new List<Ginners>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Ginners", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ginners.Add(new Ginners
                        {
                            GinnerID = reader.GetString(0),
                            Name = reader.GetString(1),
                            Contact = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Address = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IBAN = reader.IsDBNull(4) ? null : reader.GetString(4),
                            NTN = reader.IsDBNull(5) ? null : reader.GetString(5),
                            STN = reader.IsDBNull(6) ? null : reader.GetString(6)
                        });
                    }
                }
            }
            return ginners;
        }

        public bool UpdateGinner(Ginners ginner)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"UPDATE Ginners
                                    SET Name = @Name, Contact = @Contact, Address = @Address, IBAN = @IBAN, NTN = @NTN, STN = @STN
                                    WHERE GinnerID = @GinnerID";
                    command.Parameters.AddWithValue("@Name", ginner.Name);
                    command.Parameters.AddWithValue("@Contact", ginner.Contact);
                    command.Parameters.AddWithValue("@Address", ginner.Address);
                    command.Parameters.AddWithValue("@IBAN", ginner.IBAN);
                    command.Parameters.AddWithValue("@NTN", ginner.NTN);
                    command.Parameters.AddWithValue("@STN", ginner.STN);
                    command.Parameters.AddWithValue("@GinnerID", ginner.GinnerID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteGinner(string ginnerID)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM Ginners WHERE GinnerID = @GinnerID";
                    command.Parameters.AddWithValue("@GinnerID", ginnerID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
        public bool AddMill(Mills mill)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"INSERT INTO Mills (Name, Contact, Address, NTN, STN)
                                    VALUES (@Name, @Contact, @Address, @NTN, @STN)";
                    command.Parameters.AddWithValue("@Name", mill.Name);
                    command.Parameters.AddWithValue("@Contact", mill.Contact);
                    command.Parameters.AddWithValue("@Address", mill.Address);
                    command.Parameters.AddWithValue("@NTN", mill.NTN);
                    command.Parameters.AddWithValue("@STN", mill.STN);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<Mills> GetAllMills()
        {
            var mills = new List<Mills>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Mills", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        mills.Add(new Mills
                        {
                            MillID = reader.GetString(0),
                            Name = reader.GetString(1),
                            Contact = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Address = reader.IsDBNull(3) ? null : reader.GetString(3),
                            NTN = reader.IsDBNull(4) ? null : reader.GetString(4),
                            STN = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }
            }
            return mills;
        }

        public bool UpdateMill(Mills mill)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"UPDATE Mills
                                    SET Name = @Name, Contact = @Contact, Address = @Address, NTN = @NTN, STN = @STN
                                    WHERE MillID = @MillID";
                    command.Parameters.AddWithValue("@Name", mill.Name);
                    command.Parameters.AddWithValue("@Contact", mill.Contact);
                    command.Parameters.AddWithValue("@Address", mill.Address);
                    command.Parameters.AddWithValue("@NTN", mill.NTN);
                    command.Parameters.AddWithValue("@STN", mill.STN);
                    command.Parameters.AddWithValue("@MillID", mill.MillID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteMill(int millID)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM Mills WHERE MillID = @MillID";
                    command.Parameters.AddWithValue("@MillID", millID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool AddContract(Contracts contract)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"INSERT INTO Contracts (ContractID,GinnerID, MillID, TotalBales, PricePerBatch, TotalAmount,
                                                       CommissionPercentage, DateCreated, DeliveryNotes, PaymentNotes)
                                    VALUES (@ContractID, @GinnerID, @MillID, @TotalBales, @PricePerBatch, @TotalAmount,
                                            @CommissionPercentage, @DateCreated, @DeliveryNotes, @PaymentNotes)";
                    command.Parameters.AddWithValue("@ContractID", contract.ContractID);
                    command.Parameters.AddWithValue("@GinnerID", contract.GinnerID);
                    command.Parameters.AddWithValue("@MillID", contract.MillID);
                    command.Parameters.AddWithValue("@TotalBales", contract.TotalBales);
                    command.Parameters.AddWithValue("@PricePerBatch", contract.PricePerBatch);
                    command.Parameters.AddWithValue("@TotalAmount", contract.TotalAmount);
                    command.Parameters.AddWithValue("@CommissionPercentage", contract.CommissionPercentage);
                    command.Parameters.AddWithValue("@DateCreated", contract.DateCreated);
                    command.Parameters.AddWithValue("@DeliveryNotes", contract.DeliveryNotes);
                    command.Parameters.AddWithValue("@PaymentNotes", contract.PaymentNotes);
                    return command.ExecuteNonQuery() > 0;
                }
            }

        }
        public List<Contracts> GetAllContracts()
        {
            var contracts = new List<Contracts>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Contracts", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contracts.Add(new Contracts
                        {
                            ContractID = reader.GetString(0),
                            GinnerID = reader.GetString(1),
                            MillID = reader.GetString(2),
                            TotalBales = reader.GetInt32(3),
                            PricePerBatch = reader.GetDouble(4),
                            TotalAmount = reader.GetDouble(5),
                            CommissionPercentage = reader.GetDouble(6),
                            DateCreated = reader.GetDateTime(7),
                            DeliveryNotes = reader.IsDBNull(8) ? null : reader.GetString(8),
                            PaymentNotes = reader.IsDBNull(10) ? null : reader.GetString(10)
                        });
                    }
                }
            }
            return contracts;
        }

        public bool UpdateContract(Contracts contract)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"UPDATE Contracts
                                    SET GinnerID = @GinnerID, MillID = @MillID, DealID = @DealID, TotalBales = @TotalBales,
                                        PricePerBatch = @PricePerBatch, TotalAmount = @TotalAmount, CommissionPercentage = @CommissionPercentage,
                                        DateCreated = @DateCreated, DeliveryNotes = @DeliveryNotes, PaymentNotes = @PaymentNotes
                                    WHERE ContractID = @ContractID";
                    command.Parameters.AddWithValue("@ContractID", contract.ContractID);
                    command.Parameters.AddWithValue("@GinnerID", contract.GinnerID);
                    command.Parameters.AddWithValue("@MillID", contract.MillID);
                    command.Parameters.AddWithValue("@TotalBales", contract.TotalBales);
                    command.Parameters.AddWithValue("@PricePerBatch", contract.PricePerBatch);
                    command.Parameters.AddWithValue("@TotalAmount", contract.TotalAmount);
                    command.Parameters.AddWithValue("@CommissionPercentage", contract.CommissionPercentage);
                    command.Parameters.AddWithValue("@DateCreated", contract.DateCreated);
                    command.Parameters.AddWithValue("@DeliveryNotes", contract.DeliveryNotes);
                    command.Parameters.AddWithValue("@PaymentNotes", contract.PaymentNotes);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteContract(int contractID)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM Contracts WHERE ContractID = @ContractID";
                    command.Parameters.AddWithValue("@ContractID", contractID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool AddDelivery(Deliveries delivery)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"INSERT INTO Deliveries (ContractID, TruckNumber, DriverContact, NumberOfBales, DateBooked, DateDelivered)
                                    VALUES (@ContractID, @TruckNumber, @DriverContact, @NumberOfBales, @DateBooked, @DateDelivered)";
                    command.Parameters.AddWithValue("@ContractID", delivery.ContractID);
                    command.Parameters.AddWithValue("@TruckNumber", delivery.TruckNumber);
                    command.Parameters.AddWithValue("@DriverContact", delivery.DriverContact);
                    command.Parameters.AddWithValue("@NumberOfBales", delivery.NumberOfBales);
                    command.Parameters.AddWithValue("@DateBooked", delivery.DateBooked);
                    command.Parameters.AddWithValue("@DateDelivered", delivery.DateDelivered);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<Deliveries> GetAllDeliveries()
        {
            var deliveries = new List<Deliveries>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Deliveries", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        deliveries.Add(new Deliveries
                        {
                            DeliveryID = reader.GetString(0),
                            ContractID = reader.GetString(1),
                            TruckNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            DriverContact = reader.IsDBNull(3) ? null : reader.GetString(3),
                            NumberOfBales = reader.GetInt32(4),
                            DateBooked = reader.GetDateTime(5),
                            DateDelivered = reader.GetDateTime(6)
                        });
                    }
                }
            }
            return deliveries;
        }

        public bool UpdateDelivery(Deliveries delivery)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"UPDATE Deliveries
                                    SET ContractID = @ContractID, TruckNumber = @TruckNumber, DriverContact = @DriverContact,
                                        NumberOfBales = @NumberOfBales, DateBooked = @DateBooked, DateDelivered = @DateDelivered
                                    WHERE DeliveryID = @DeliveryID";
                    command.Parameters.AddWithValue("@ContractID", delivery.ContractID);
                    command.Parameters.AddWithValue("@TruckNumber", delivery.TruckNumber);
                    command.Parameters.AddWithValue("@DriverContact", delivery.DriverContact);
                    command.Parameters.AddWithValue("@NumberOfBales", delivery.NumberOfBales);
                    command.Parameters.AddWithValue("@DateBooked", delivery.DateBooked);
                    command.Parameters.AddWithValue("@DateDelivered", delivery.DateDelivered);
                    command.Parameters.AddWithValue("@DeliveryID", delivery.DeliveryID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
        public bool DeleteDelivery(int deliveryID)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM Deliveries WHERE DeliveryID = @DeliveryID";
                    command.Parameters.AddWithValue("@DeliveryID", deliveryID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool AddPayment(Payment payment)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"INSERT INTO Payments (ContractID, AmountPaid, Date, TransactionID)
                                    VALUES (@ContractID, @AmountPaid, @Date, @TransactionID)";
                    command.Parameters.AddWithValue("@ContractID", payment.ContractID);
                    command.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                    command.Parameters.AddWithValue("@Date", payment.Date);
                    command.Parameters.AddWithValue("@TransactionID", payment.TransactionID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
        public List<Payment> GetAllPayments()
        {
            var payments = new List<Payment>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Payments", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payments.Add(new Payment
                        {
                            PaymentID = reader.GetString(0),
                            ContractID = reader.GetString(1),
                            AmountPaid = reader.GetDouble(2),
                            Date = reader.GetDateTime(3),
                            TransactionID = reader.IsDBNull(4) ? null : reader.GetString(4)
                        });
                    }
                }
            }
            return payments;
        }

        public bool UpdatePayment(Payment payment)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"UPDATE Payments
                                    SET ContractID = @ContractID, AmountPaid = @AmountPaid, Date = @Date, TransactionID = @TransactionID
                                    WHERE PaymentID = @PaymentID";
                    command.Parameters.AddWithValue("@ContractID", payment.ContractID);
                    command.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                    command.Parameters.AddWithValue("@Date", payment.Date);
                    command.Parameters.AddWithValue("@TransactionID", payment.TransactionID);
                    command.Parameters.AddWithValue("@PaymentID", payment.PaymentID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeletePayment(int paymentID)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM Payments WHERE PaymentID = @PaymentID";
                    command.Parameters.AddWithValue("@PaymentID", paymentID);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}

