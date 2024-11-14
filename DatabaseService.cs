using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using MadinaEnterprises.Modules.Models;
using System.Reflection;

namespace MadinaEnterprises

{
    public class DatabaseService
    {
        private const string DatabaseFileName = "MadinaEnterprises.db";
        private readonly string _databasePath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Define the database path (within the project directory)
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
                                            DateCreated TEXT,
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



    }

}

