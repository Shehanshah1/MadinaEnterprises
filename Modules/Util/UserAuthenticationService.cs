using System;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Linq;

namespace MadinaEnterprises.Modules.Util
{
    public class UserAuthenticationService
    {
        private readonly string _connectionString;
        private static UserAuthenticationService? _instance;
        private User? _currentUser;

        public static UserAuthenticationService Instance
        {
            get
            {
                _instance ??= new UserAuthenticationService();
                return _instance;
            }
        }

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        private UserAuthenticationService()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "madina_users.db3");
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            var cmd = new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    FullName TEXT,
                    Role TEXT DEFAULT 'User',
                    IsActive INTEGER DEFAULT 1,
                    CreatedDate TEXT,
                    LastLoginDate TEXT,
                    PasswordResetToken TEXT,
                    PasswordResetExpiry TEXT,
                    TwoFactorEnabled INTEGER DEFAULT 0,
                    TwoFactorSecret TEXT,
                    ProfileImage TEXT,
                    Phone TEXT,
                    Address TEXT,
                    Department TEXT,
                    EmployeeId TEXT
                );

                CREATE TABLE IF NOT EXISTS UserSessions (
                    SessionId TEXT PRIMARY KEY,
                    UserId INTEGER,
                    LoginTime TEXT,
                    LogoutTime TEXT,
                    IPAddress TEXT,
                    DeviceInfo TEXT,
                    IsActive INTEGER DEFAULT 1,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );

                CREATE TABLE IF NOT EXISTS UserPermissions (
                    PermissionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    Module TEXT,
                    CanView INTEGER DEFAULT 1,
                    CanCreate INTEGER DEFAULT 0,
                    CanEdit INTEGER DEFAULT 0,
                    CanDelete INTEGER DEFAULT 0,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );

                CREATE TABLE IF NOT EXISTS AuditLog (
                    LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    Action TEXT,
                    Module TEXT,
                    RecordId TEXT,
                    OldValue TEXT,
                    NewValue TEXT,
                    Timestamp TEXT,
                    IPAddress TEXT,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );

                CREATE TABLE IF NOT EXISTS PasswordHistory (
                    HistoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    PasswordHash TEXT,
                    ChangedDate TEXT,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );", conn);

            cmd.ExecuteNonQuery();

            // Create default admin user if not exists
            CreateDefaultAdminUser();
        }

        private void CreateDefaultAdminUser()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE Username = 'admin'", conn);
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (count == 0)
            {
                var salt = GenerateSalt();
                var passwordHash = HashPassword("admin123", salt);

                var cmd = new SQLiteCommand(@"
                    INSERT INTO Users (Username, Email, PasswordHash, Salt, FullName, Role, CreatedDate, IsActive)
                    VALUES (@Username, @Email, @PasswordHash, @Salt, @FullName, @Role, @CreatedDate, 1)", conn);

                cmd.Parameters.AddWithValue("@Username", "admin");
                cmd.Parameters.AddWithValue("@Email", "admin@madinaenterprises.com");
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Salt", salt);
                cmd.Parameters.AddWithValue("@FullName", "System Administrator");
                cmd.Parameters.AddWithValue("@Role", "Admin");
                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }
        }

        private string GenerateSalt()
        {
            var saltBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var combinedBytes = Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha256.ComputeHash(combinedBytes);
            return Convert.ToBase64String(hashBytes);
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
        {
            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new SQLiteCommand(@"
                    SELECT UserId, Username, Email, PasswordHash, Salt, FullName, Role, IsActive, 
                           TwoFactorEnabled, ProfileImage, Phone, Department, EmployeeId
                    FROM Users 
                    WHERE (Username = @Username OR Email = @Username) AND IsActive = 1", conn);

                cmd.Parameters.AddWithValue("@Username", username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var storedHash = reader["PasswordHash"].ToString();
                    var salt = reader["Salt"].ToString();
                    var passwordHash = HashPassword(password, salt!);

                    if (passwordHash == storedHash)
                    {
                        _currentUser = new User
                        {
                            UserId = Convert.ToInt32(reader["UserId"]),
                            Username = reader["Username"].ToString()!,
                            Email = reader["Email"].ToString()!,
                            FullName = reader["FullName"].ToString() ?? "",
                            Role = reader["Role"].ToString() ?? "User",
                            ProfileImage = reader["ProfileImage"].ToString(),
                            Phone = reader["Phone"].ToString(),
                            Department = reader["Department"].ToString(),
                            EmployeeId = reader["EmployeeId"].ToString()
                        };

                        // Update last login
                        await UpdateLastLoginAsync(_currentUser.UserId);

                        // Create session
                        await CreateSessionAsync(_currentUser.UserId);

                        // Log the login
                        await LogAuditAsync("Login", "Authentication", "", "");

                        return (true, "Login successful", _currentUser);
                    }
                    else
                    {
                        await LogAuditAsync("Failed Login", "Authentication", username, "Invalid password");
                        return (false, "Invalid username or password", null);
                    }
                }

                return (false, "Invalid username or password", null);
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password, string fullName, string role = "User")
        {
            try
            {
                // Validate password strength
                if (!IsPasswordStrong(password))
                {
                    return (false, "Password must be at least 8 characters and contain uppercase, lowercase, number, and special character");
                }

                using var conn = new SQLiteConnection(_connectionString);
                await conn.OpenAsync();

                // Check if user exists
                var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email", conn);
                checkCmd.Parameters.AddWithValue("@Username", username);
                checkCmd.Parameters.AddWithValue("@Email", email);

                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    return (false, "Username or email already exists");
                }

                // Create user
                var salt = GenerateSalt();
                var passwordHash = HashPassword(password, salt);

                var cmd = new SQLiteCommand(@"
                    INSERT INTO Users (Username, Email, PasswordHash, Salt, FullName, Role, CreatedDate, IsActive)
                    VALUES (@Username, @Email, @PasswordHash, @Salt, @FullName, @Role, @CreatedDate, 1)", conn);

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Salt", salt);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                await cmd.ExecuteNonQueryAsync();

                return (true, "Registration successful");
            }
            catch (Exception ex)
            {
                return (false, $"Registration failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                if (!IsPasswordStrong(newPassword))
                {
                    return (false, "New password does not meet security requirements");
                }

                using var conn = new SQLiteConnection(_connectionString);
                await conn.OpenAsync();

                // Verify current password
                var getCmd = new SQLiteCommand("SELECT PasswordHash, Salt FROM Users WHERE UserId = @UserId", conn);
                getCmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var storedHash = reader["PasswordHash"].ToString();
                    var salt = reader["Salt"].ToString();
                    var currentHash = HashPassword(currentPassword, salt!);

                    if (currentHash != storedHash)
                    {
                        return (false, "Current password is incorrect");
                    }

                    reader.Close();

                    // Check password history
                    if (await IsPasswordInHistoryAsync(userId, newPassword, salt!))
                    {
                        return (false, "Password has been used recently. Please choose a different password");
                    }

                    // Save old password to history
                    await SavePasswordHistoryAsync(userId, storedHash!);

                    // Update password
                    var newSalt = GenerateSalt();
                    var newHash = HashPassword(newPassword, newSalt);

                    var updateCmd = new SQLiteCommand(@"
                        UPDATE Users SET PasswordHash = @PasswordHash, Salt = @Salt 
                        WHERE UserId = @UserId", conn);

                    updateCmd.Parameters.AddWithValue("@PasswordHash", newHash);
                    updateCmd.Parameters.AddWithValue("@Salt", newSalt);
                    updateCmd.Parameters.AddWithValue("@UserId", userId);

                    await updateCmd.ExecuteNonQueryAsync();

                    await LogAuditAsync("Password Changed", "User Management", userId.ToString(), "");

                    return (true, "Password changed successfully");
                }

                return (false, "User not found");
            }
            catch (Exception ex)
            {
                return (false, $"Password change failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email)
        {
            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                await conn.OpenAsync();

                // Generate reset token
                var resetToken = Guid.NewGuid().ToString();
                var expiry = DateTime.Now.AddHours(24);

                var cmd = new SQLiteCommand(@"
                    UPDATE Users 
                    SET PasswordResetToken = @Token, PasswordResetExpiry = @Expiry 
                    WHERE Email = @Email", conn);

                cmd.Parameters.AddWithValue("@Token", resetToken);
                cmd.Parameters.AddWithValue("@Expiry", expiry.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@Email", email);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    // In production, send email with reset link
                    // For now, return the token
                    return (true, $"Password reset token: {resetToken}");
                }

                return (false, "Email not found");
            }
            catch (Exception ex)
            {
                return (false, $"Password reset failed: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                await EndSessionAsync(_currentUser.UserId);
                await LogAuditAsync("Logout", "Authentication", "", "");
                _currentUser = null;
            }
        }

        private async Task UpdateLastLoginAsync(int userId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand("UPDATE Users SET LastLoginDate = @Date WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@UserId", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task CreateSessionAsync(int userId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand(@"
                INSERT INTO UserSessions (SessionId, UserId, LoginTime, DeviceInfo, IsActive)
                VALUES (@SessionId, @UserId, @LoginTime, @DeviceInfo, 1)", conn);

            cmd.Parameters.AddWithValue("@SessionId", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@DeviceInfo", DeviceInfo.Current.Platform.ToString());

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task EndSessionAsync(int userId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand(@"
                UPDATE UserSessions 
                SET LogoutTime = @LogoutTime, IsActive = 0 
                WHERE UserId = @UserId AND IsActive = 1", conn);

            cmd.Parameters.AddWithValue("@LogoutTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@UserId", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task LogAuditAsync(string action, string module, string recordId, string details)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand(@"
                INSERT INTO AuditLog (UserId, Action, Module, RecordId, NewValue, Timestamp)
                VALUES (@UserId, @Action, @Module, @RecordId, @Details, @Timestamp)", conn);

            cmd.Parameters.AddWithValue("@UserId", _currentUser?.UserId ?? 0);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@Module", module);
            cmd.Parameters.AddWithValue("@RecordId", recordId);
            cmd.Parameters.AddWithValue("@Details", details);
            cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SavePasswordHistoryAsync(int userId, string passwordHash)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand(@"
                INSERT INTO PasswordHistory (UserId, PasswordHash, ChangedDate)
                VALUES (@UserId, @PasswordHash, @ChangedDate)", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@ChangedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            await cmd.ExecuteNonQueryAsync();

            // Keep only last 5 passwords
            var deleteCmd = new SQLiteCommand(@"
                DELETE FROM PasswordHistory 
                WHERE UserId = @UserId AND HistoryId NOT IN (
                    SELECT HistoryId FROM PasswordHistory 
                    WHERE UserId = @UserId 
                    ORDER BY ChangedDate DESC 
                    LIMIT 5
                )", conn);

            deleteCmd.Parameters.AddWithValue("@UserId", userId);
            await deleteCmd.ExecuteNonQueryAsync();
        }

        private async Task<bool> IsPasswordInHistoryAsync(int userId, string password, string salt)
        {
            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand(@"
                SELECT PasswordHash FROM PasswordHistory 
                WHERE UserId = @UserId 
                ORDER BY ChangedDate DESC 
                LIMIT 5", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            var newHash = HashPassword(password, salt);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader["PasswordHash"].ToString() == newHash)
                    return true;
            }

            return false;
        }

        private bool IsPasswordStrong(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;
            return true;
        }

        public async Task<bool> HasPermissionAsync(string module, string permission)
        {
            if (_currentUser == null) return false;
            if (_currentUser.Role == "Admin") return true;

            using var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SQLiteCommand($@"
                SELECT {permission} FROM UserPermissions 
                WHERE UserId = @UserId AND Module = @Module", conn);

            cmd.Parameters.AddWithValue("@UserId", _currentUser.UserId);
            cmd.Parameters.AddWithValue("@Module", module);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) == 1;
        }
    }

    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "User";
        public string? ProfileImage { get; set; }
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public string? EmployeeId { get; set; }
    }
}