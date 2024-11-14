using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

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
                                            GinnerID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            Name TEXT NOT NULL,
                                            Contact TEXT,
                                            Address TEXT,
                                            NTN TEXT,
                                            STN TEXT
                                        );";
                    command.ExecuteNonQuery();

                    // Mills Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Mills (
                                            MillID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            Name TEXT NOT NULL,
                                            Contact TEXT,
                                            Address TEXT,
                                            NTN TEXT,
                                            STN TEXT
                                        );";
                    command.ExecuteNonQuery();

                    // Contracts Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Contracts (
                                            ContractID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            GinnerID INTEGER NOT NULL,
                                            MillID INTEGER NOT NULL,
                                            DealID TEXT UNIQUE NOT NULL,
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
                                            DeliveryID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            ContractID INTEGER NOT NULL,
                                            TruckNumber TEXT,
                                            DriverContact TEXT,
                                            NumberOfBales INTEGER,
                                            DateBooked TEXT,
                                            DateDelivered TEXT,
                                            FOREIGN KEY (ContractID) REFERENCES Contracts (ContractID)
                                        );";
                    command.ExecuteNonQuery();

                    // Payments Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Payments (
                                            PaymentID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            ContractID INTEGER NOT NULL,
                                            AmountPaid REAL,
                                            Date TEXT,
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
    }
}

