using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MadinaEnterprises.Modules.Models;
using ClosedXML.Excel;

namespace MadinaEnterprises.Services
{
    public class DataSyncService
    {
        private static DataSyncService? _instance;
        private readonly DatabaseService _db;
        private readonly string _backupDirectory;

        public static DataSyncService Instance
        {
            get
            {
                _instance ??= new DataSyncService();
                return _instance;
            }
        }

        private DataSyncService()
        {
            _db = App.DatabaseService!;
            _backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Backups");

            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);
        }

        // ========== BACKUP OPERATIONS ==========
        public async Task<BackupResult> CreateFullBackup(string? customName = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupName = customName ?? $"MadinaBackup_{timestamp}";
                var backupPath = Path.Combine(_backupDirectory, $"{backupName}.meb"); // Madina Enterprise Backup

                // Create backup data structure
                var backupData = new BackupData
                {
                    Version = "1.0",
                    CreatedAt = DateTime.Now,
                    DeviceInfo = DeviceInfo.Current.Platform.ToString(),
                    Contracts = await _db.GetAllContracts(),
                    Ginners = await _db.GetAllGinners(),
                    Mills = await _db.GetAllMills(),
                    Deliveries = await _db.GetAllDeliveries(),
                    Payments = await _db.GetAllPayments(),
                    GinnerLedger = await _db.GetAllGinnerLedger()
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Compress and encrypt
                var compressedData = CompressData(json);
                var encryptedData = SimpleEncrypt(compressedData);

                // Save to file
                await File.WriteAllBytesAsync(backupPath, encryptedData);

                // Clean old backups (keep last 10)
                CleanOldBackups(10);

                return new BackupResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    BackupSize = new FileInfo(backupPath).Length,
                    RecordCount = GetTotalRecordCount(backupData),
                    Message = "Backup created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = $"Backup failed: {ex.Message}"
                };
            }
        }

        public async Task<BackupResult> CreateIncrementalBackup(DateTime sinceDate)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(_backupDirectory, $"MadinaIncremental_{timestamp}.mib");

                // Get only changed records since the specified date
                var allContracts = await _db.GetAllContracts();
                var allDeliveries = await _db.GetAllDeliveries();
                var allPayments = await _db.GetAllPayments();

                var backupData = new BackupData
                {
                    Version = "1.0",
                    CreatedAt = DateTime.Now,
                    IsIncremental = true,
                    IncrementalSince = sinceDate,
                    Contracts = allContracts.Where(c => c.DateCreated >= sinceDate).ToList(),
                    Deliveries = allDeliveries.Where(d => d.DeliveryDate >= sinceDate).ToList(),
                    Payments = allPayments.Where(p => p.Date >= sinceDate).ToList(),
                    // Always include all master data
                    Ginners = await _db.GetAllGinners(),
                    Mills = await _db.GetAllMills(),
                    GinnerLedger = await _db.GetAllGinnerLedger()
                };

                var json = JsonSerializer.Serialize(backupData);
                var compressedData = CompressData(json);
                await File.WriteAllBytesAsync(backupPath, compressedData);

                return new BackupResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    BackupSize = new FileInfo(backupPath).Length,
                    RecordCount = GetTotalRecordCount(backupData),
                    Message = "Incremental backup created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = $"Incremental backup failed: {ex.Message}"
                };
            }
        }

        // ========== RESTORE OPERATIONS ==========
        public async Task<RestoreResult> RestoreFromBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "Backup file not found"
                    };
                }

                // Read and decrypt backup
                var encryptedData = await File.ReadAllBytesAsync(backupPath);
                var compressedData = SimpleDecrypt(encryptedData);
                var json = DecompressData(compressedData);

                // Deserialize backup data
                var backupData = JsonSerializer.Deserialize<BackupData>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (backupData == null)
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "Invalid backup file"
                    };
                }

                // Create a safety backup before restore
                await CreateFullBackup("PreRestore_AutoBackup");

                // Restore data
                var restoredCount = 0;

                // Restore in correct order (master data first)
                foreach (var ginner in backupData.Ginners)
                {
                    var existing = (await _db.GetAllGinners()).FirstOrDefault(g => g.GinnerID == ginner.GinnerID);
                    if (existing == null)
                    {
                        await _db.AddGinner(ginner);
                        restoredCount++;
                    }
                }

                foreach (var mill in backupData.Mills)
                {
                    var existing = (await _db.GetAllMills()).FirstOrDefault(m => m.MillID == mill.MillID);
                    if (existing == null)
                    {
                        await _db.AddMill(mill);
                        restoredCount++;
                    }
                }

                foreach (var contract in backupData.Contracts)
                {
                    var existing = (await _db.GetAllContracts()).FirstOrDefault(c => c.ContractID == contract.ContractID);
                    if (existing == null)
                    {
                        await _db.AddContract(contract);
                        restoredCount++;
                    }
                }

                foreach (var delivery in backupData.Deliveries)
                {
                    var existing = (await _db.GetAllDeliveries()).FirstOrDefault(d => d.DeliveryID == delivery.DeliveryID);
                    if (existing == null)
                    {
                        await _db.AddDelivery(delivery);
                        restoredCount++;
                    }
                }

                foreach (var payment in backupData.Payments)
                {
                    var existing = (await _db.GetAllPayments()).FirstOrDefault(p => p.PaymentID == payment.PaymentID);
                    if (existing == null)
                    {
                        await _db.AddPayment(payment);
                        restoredCount++;
                    }
                }

                foreach (var ledger in backupData.GinnerLedger)
                {
                    var existing = (await _db.GetAllGinnerLedger())
                        .FirstOrDefault(l => l.ContractID == ledger.ContractID && l.DealID == ledger.DealID);
                    if (existing == null)
                    {
                        await _db.AddGinnerLedger(ledger);
                        restoredCount++;
                    }
                }

                return new RestoreResult
                {
                    Success = true,
                    RestoredRecords = restoredCount,
                    Message = $"Successfully restored {restoredCount} records from backup"
                };
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = $"Restore failed: {ex.Message}"
                };
            }
        }

        // ========== EXPORT OPERATIONS ==========
        public async Task<string> ExportToExcel(ExportOptions options)
        {
            var fileName = $"MadinaExport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var filePath = Path.Combine(_backupDirectory, fileName);

            using var workbook = new XLWorkbook();

            if (options.IncludeContracts)
            {
                var contracts = await _db.GetAllContracts();
                var ws = workbook.Worksheets.Add("Contracts");
                ws.Cell(1, 1).InsertTable(contracts);
                ws.Columns().AdjustToContents();
            }

            if (options.IncludeGinners)
            {
                var ginners = await _db.GetAllGinners();
                var ws = workbook.Worksheets.Add("Ginners");
                ws.Cell(1, 1).InsertTable(ginners);
                ws.Columns().AdjustToContents();
            }

            if (options.IncludeMills)
            {
                var mills = await _db.GetAllMills();
                var ws = workbook.Worksheets.Add("Mills");
                ws.Cell(1, 1).InsertTable(mills);
                ws.Columns().AdjustToContents();
            }

            if (options.IncludeDeliveries)
            {
                var deliveries = await _db.GetAllDeliveries();
                var ws = workbook.Worksheets.Add("Deliveries");
                ws.Cell(1, 1).InsertTable(deliveries);
                ws.Columns().AdjustToContents();
            }

            if (options.IncludePayments)
            {
                var payments = await _db.GetAllPayments();
                var ws = workbook.Worksheets.Add("Payments");
                ws.Cell(1, 1).InsertTable(payments);
                ws.Columns().AdjustToContents();
            }

            if (options.IncludeLedger)
            {
                var ledger = await _db.GetAllGinnerLedger();
                var ws = workbook.Worksheets.Add("GinnerLedger");
                ws.Cell(1, 1).InsertTable(ledger);
                ws.Columns().AdjustToContents();
            }

            workbook.SaveAs(filePath);
            return filePath;
        }

        public async Task<string> ExportToCSV(string entityType)
        {
            var fileName = $"{entityType}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_backupDirectory, fileName);
            var csv = new StringBuilder();

            switch (entityType.ToLower())
            {
                case "contracts":
                    var contracts = await _db.GetAllContracts();
                    csv.AppendLine("ContractID,GinnerID,MillID,TotalBales,PricePerBatch,TotalAmount,CommissionPercentage,DateCreated");
                    foreach (var c in contracts)
                        csv.AppendLine($"{c.ContractID},{c.GinnerID},{c.MillID},{c.TotalBales},{c.PricePerBatch},{c.TotalAmount},{c.CommissionPercentage},{c.DateCreated:yyyy-MM-dd}");
                    break;

                case "ginners":
                    var ginners = await _db.GetAllGinners();
                    csv.AppendLine("GinnerID,GinnerName,Contact,IBAN,Address,NTN,STN");
                    foreach (var g in ginners)
                        csv.AppendLine($"{g.GinnerID},{g.GinnerName},{g.Contact},{g.IBAN},{g.Address},{g.NTN},{g.STN}");
                    break;

                case "payments":
                    var payments = await _db.GetAllPayments();
                    csv.AppendLine("PaymentID,ContractID,TotalAmount,AmountPaid,TotalBales,Date");
                    foreach (var p in payments)
                        csv.AppendLine($"{p.PaymentID},{p.ContractID},{p.TotalAmount},{p.AmountPaid},{p.TotalBales},{p.Date:yyyy-MM-dd}");
                    break;
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            return filePath;
        }

        // ========== UTILITY METHODS ==========
        private byte[] CompressData(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            using var output = new MemoryStream();
            using (var deflateStream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                deflateStream.Write(bytes, 0, bytes.Length);
            }
            return output.ToArray();
        }

        private string DecompressData(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var deflateStream = new DeflateStream(input, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(output);
            }
            return Encoding.UTF8.GetString(output.ToArray());
        }

        private byte[] SimpleEncrypt(byte[] data)
        {
            // Simple XOR encryption for demonstration
            // In production, use proper encryption like AES
            var key = Encoding.UTF8.GetBytes("MadinaEnterprises2024SecretKey!!");
            var encrypted = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                encrypted[i] = (byte)(data[i] ^ key[i % key.Length]);
            }

            return encrypted;
        }

        private byte[] SimpleDecrypt(byte[] data)
        {
            // XOR encryption is symmetric
            return SimpleEncrypt(data);
        }

        private void CleanOldBackups(int keepCount)
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, "*.meb")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Skip(keepCount)
                .ToList();

            foreach (var file in backupFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        private int GetTotalRecordCount(BackupData data)
        {
            return data.Contracts.Count + data.Ginners.Count + data.Mills.Count +
                   data.Deliveries.Count + data.Payments.Count + data.GinnerLedger.Count;
        }

        public List<BackupInfo> GetAvailableBackups()
        {
            var backups = new List<BackupInfo>();

            foreach (var file in Directory.GetFiles(_backupDirectory, "*.meb"))
            {
                var info = new FileInfo(file);
                backups.Add(new BackupInfo
                {
                    FileName = info.Name,
                    FilePath = info.FullName,
                    CreatedAt = info.CreationTime,
                    Size = info.Length,
                    SizeFormatted = FormatFileSize(info.Length)
                });
            }

            return backups.OrderByDescending(b => b.CreatedAt).ToList();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    // ========== SUPPORTING CLASSES ==========
    public class BackupData
    {
        public string Version { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string DeviceInfo { get; set; } = "";
        public bool IsIncremental { get; set; }
        public DateTime? IncrementalSince { get; set; }
        public List<Contracts> Contracts { get; set; } = new();
        public List<Ginners> Ginners { get; set; } = new();
        public List<Mills> Mills { get; set; } = new();
        public List<Deliveries> Deliveries { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public List<GinnerLedger> GinnerLedger { get; set; } = new();
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? BackupPath { get; set; }
        public long BackupSize { get; set; }
        public int RecordCount { get; set; }
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int RestoredRecords { get; set; }
    }

    public class ExportOptions
    {
        public bool IncludeContracts { get; set; } = true;
        public bool IncludeGinners { get; set; } = true;
        public bool IncludeMills { get; set; } = true;
        public bool IncludeDeliveries { get; set; } = true;
        public bool IncludePayments { get; set; } = true;
        public bool IncludeLedger { get; set; } = true;
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string SizeFormatted { get; set; } = "";
    }
}