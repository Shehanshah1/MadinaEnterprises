using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace MadinaEnterprises
{
    public class DatabaseService
    {
        private const string DatabaseFileName = "MadinaEnterprises.db"; // SQLite database file name
        private readonly string _databasePath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Set the database path and connection string
            _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
            _connectionString = $"Data Source={_databasePath};Version=3;";
            InitializeDatabase(); // Initialize the database on service creation
        }

        // Method to initialize the database (create file and table if not exists)
        private void InitializeDatabase()
        {
            if (!File.Exists(_databasePath))
            {
                SQLiteConnection.CreateFile(_databasePath);
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    // Create Ginners table if it doesn't exist
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Ginners (
                            GinnerID TEXT UNIQUE PRIMARY KEY,
                            Name TEXT NOT NULL,
                            Contact TEXT,
                            Address TEXT,
                            IBAN TEXT,
                            NTN TEXT,
                            STN TEXT
                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        // Method to add a new Ginner to the database
        public bool AddGinner(Ginners ginner)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                            INSERT INTO Ginners (GinnerID, Name, Contact, Address, IBAN, NTN, STN)
                            VALUES (@ID, @Name, @Contact, @Address, @IBAN, @NTN, @STN)";

                        command.Parameters.AddWithValue("@ID", ginner.GinnerID);
                        command.Parameters.AddWithValue("@Name", ginner.Name);
                        command.Parameters.AddWithValue("@Contact", ginner.Contact);
                        command.Parameters.AddWithValue("@Address", ginner.Address);
                        command.Parameters.AddWithValue("@IBAN", ginner.IBAN);
                        command.Parameters.AddWithValue("@NTN", ginner.NTN);
                        command.Parameters.AddWithValue("@STN", ginner.STN);

                        return command.ExecuteNonQuery() > 0; // If rows are affected, return true
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding Ginner: {ex.Message}");
                return false;
            }
        }

        // Method to get all Ginners from the database
        public List<Ginners> GetAllGinners()
        {
            var ginnersList = new List<Ginners>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM Ginners", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var ginner = new Ginners
                                {
                                    GinnerID = reader["GinnerID"].ToString(),
                                    Name = reader["Name"].ToString(),
                                    Contact = reader["Contact"].ToString(),
                                    Address = reader["Address"].ToString(),
                                    IBAN = reader["IBAN"].ToString(),
                                    NTN = reader["NTN"].ToString(),
                                    STN = reader["STN"].ToString()
                                };
                                ginnersList.Add(ginner);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Ginners: {ex.Message}");
            }

            return ginnersList;
        }

        // Method to update an existing Ginner's details
        public bool UpdateGinner(Ginners ginner)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                            UPDATE Ginners
                            SET Name = @Name, Contact = @Contact, Address = @Address, IBAN = @IBAN, NTN = @NTN, STN = @STN
                            WHERE GinnerID = @ID";

                        command.Parameters.AddWithValue("@ID", ginner.GinnerID);
                        command.Parameters.AddWithValue("@Name", ginner.Name);
                        command.Parameters.AddWithValue("@Contact", ginner.Contact);
                        command.Parameters.AddWithValue("@Address", ginner.Address);
                        command.Parameters.AddWithValue("@IBAN", ginner.IBAN);
                        command.Parameters.AddWithValue("@NTN", ginner.NTN);
                        command.Parameters.AddWithValue("@STN", ginner.STN);

                        return command.ExecuteNonQuery() > 0; // If rows are affected, return true
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Ginner: {ex.Message}");
                return false;
            }
        }

        // Method to delete a Ginner by ID
        public bool DeleteGinner(string ginnerId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = "DELETE FROM Ginners WHERE GinnerID = @ID";
                        command.Parameters.AddWithValue("@ID", ginnerId);

                        return command.ExecuteNonQuery() > 0; // If rows are affected, return true
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting Ginner: {ex.Message}");
                return false;
            }
        }
    

// MILL METHODS
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
        //CONTRACT METHODS
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
        // DELIVERY MEHTODS
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
        // PAYMENT METHODS
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
        public List<Contracts> GetContractsByGinner(string ginnerID)
        {
            var contracts = new List<Contracts>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Contracts WHERE GinnerID = @GinnerID", connection))
                {
                    command.Parameters.AddWithValue("@GinnerID", ginnerID);

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
                                PaymentNotes = reader.IsDBNull(9) ? null : reader.GetString(9)
                            });
                        }
                    }
                }
            }

            return contracts;
        }
        public List<Payment> GetPaymentsByContract(string contractID)
        {
            var payments = new List<Payment>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Payments WHERE ContractID = @ContractID", connection))
                {
                    command.Parameters.AddWithValue("@ContractID", contractID);

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
            }

            return payments;
        }
        public List<Deliveries> GetDeliveriesByContract(string contractID)
        {
            var deliveries = new List<Deliveries>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Deliveries WHERE ContractID = @ContractID", connection))
                {
                    command.Parameters.AddWithValue("@ContractID", contractID);

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
            }

            return deliveries;
        }

}

}

