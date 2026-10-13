using Npgsql; // ספריית החיבור הרשמית של .NET למסד נתונים PostgreSQL

namespace ShadowNet.Server;

// =============================================================================
// מחלקת עזר למסד הנתונים (DatabaseHelper)
// מחלקה זו מרכזת את כל הפקודות והשאילתות מול מסד הנתונים PostgreSQL
// =============================================================================
public class DatabaseHelper
{
    // מחרוזת החיבור למסד הנתונים (כוללת שרת, משתמש, סיסמה ושם המסד)
    private readonly string _connectionString;

    // בנאי (Constructor) שמקבל את מחרוזת החיבור ושומר אותה
    public DatabaseHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    // פונקציה פנימית שמייצרת חיבור חדש למסד
    private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

    // =========================================================================
    // 1. אתחול הטבלאות במסד הנתונים בעליית השרת
    // =========================================================================
    public void InitializeTables()
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        conn.Open();

        // שאילתה ליצירת טבלת משתמשים וטבלת קודי אימות אם הן אינן קיימות
        string sql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                email VARCHAR(100) UNIQUE NOT NULL,
                password_hash VARCHAR(256) NOT NULL,
                username VARCHAR(50),
                full_name VARCHAR(100),
                is_verified BOOLEAN DEFAULT TRUE,
                last_login TIMESTAMP,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS verification_codes (
                id SERIAL PRIMARY KEY,
                email VARCHAR(100) NOT NULL,
                code VARCHAR(10) NOT NULL,
                purpose VARCHAR(30) NOT NULL,
                payload TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                expires_at TIMESTAMP NOT NULL,
                is_used BOOLEAN DEFAULT FALSE
            );
        ";

        // הרצת השאילתה ב-PostgreSQL
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();

        Console.WriteLine("[ShadowNet Server] Database tables ready.");
    }

    // =========================================================================
    // 2. בדיקה האם כתובת אימייל כבר קיימת במסד הנתונים
    // =========================================================================
    public bool UserExists(string email)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = "SELECT id FROM users WHERE email = @email";
        using var cmd = new NpgsqlCommand(sql, conn);
        // שימוש בפרמטר מאובטח למניעת הזרקות SQL
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = cmd.ExecuteReader();
        // אם הצליח לקרוא שורה, סימן שהמשתמש קיים
        return reader.Read();
    }

    // =========================================================================
    // 3. הוספת משתמש חדש לטבלה
    // =========================================================================
    public void CreateUser(string email, string passwordHash, string username, string fullName)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = @"INSERT INTO users (email, password_hash, username, full_name, is_verified, last_login) 
                       VALUES (@email, @password_hash, @username, @full_name, TRUE, CURRENT_TIMESTAMP)";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@password_hash", passwordHash);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@full_name", fullName);

        // ביצוע פקודת ההכנסה
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 4. שליפת משתמש לפי אימייל (עבור התחברות)
    // =========================================================================
    public UserRecord? GetUserByEmail(string email)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = "SELECT id, email, password_hash, username, full_name FROM users WHERE email = @email";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = cmd.ExecuteReader();
        // אם נמצאה שורה תואמת, בונים אובייקט משתמש ומחזירים אותו
        if (reader.Read())
        {
            return new UserRecord
            {
                Id = Convert.ToInt32(reader["id"]),
                Email = reader["email"].ToString()!,
                PasswordHash = reader["password_hash"].ToString()!,
                Username = reader["username"]?.ToString() ?? email.Split('@')[0],
                FullName = reader["full_name"]?.ToString() ?? ""
            };
        }

        // אם המשתמש לא נמצא
        return null;
    }

    // =========================================================================
    // 5. עדכון זמן כניסה אחרון (last_login)
    // =========================================================================
    public void UpdateLastLogin(int userId)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = "UPDATE users SET last_login = CURRENT_TIMESTAMP WHERE id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 6. עדכון סיסמה חדשה למשתמש (איפוס סיסמה)
    // =========================================================================
    public void UpdatePassword(string email, string newHash)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = "UPDATE users SET password_hash = @newHash WHERE email = @email";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@newHash", newHash);
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 7. שמירת קוד אימות חדש (OTP) בטבלת verification_codes
    // =========================================================================
    public void SaveVerificationCode(string email, string code, string purpose)
    {
        using var conn = GetConnection();
        conn.Open();

        // קודם כל מבטלים קודים קודמים שלא נוצלו עבור אותו אימייל ואותה מטרה
        string cancelSql = "UPDATE verification_codes SET is_used = TRUE WHERE email = @email AND purpose = @purpose AND is_used = FALSE";
        using (var cancelCmd = new NpgsqlCommand(cancelSql, conn))
        {
            cancelCmd.Parameters.AddWithValue("@email", email);
            cancelCmd.Parameters.AddWithValue("@purpose", purpose);
            cancelCmd.ExecuteNonQuery();
        }

        // כעת מכניסים את הקוד החדש עם תוקף ל-10 דקות מהרגע הנוכחי
        string insertSql = @"INSERT INTO verification_codes (email, code, purpose, expires_at) 
                             VALUES (@email, @code, @purpose, CURRENT_TIMESTAMP + INTERVAL '10 minutes')";
        using (var insertCmd = new NpgsqlCommand(insertSql, conn))
        {
            insertCmd.Parameters.AddWithValue("@email", email);
            insertCmd.Parameters.AddWithValue("@code", code);
            insertCmd.Parameters.AddWithValue("@purpose", purpose);
            insertCmd.ExecuteNonQuery();
        }
    }

    // =========================================================================
    // 8. בדיקה האם קוד אימות תקין ובתוקף
    // =========================================================================
    public bool IsCodeValid(string email, string code, string purpose)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = @"SELECT id FROM verification_codes 
                       WHERE email = @email AND code = @code AND purpose = @purpose 
                         AND is_used = FALSE AND expires_at > CURRENT_TIMESTAMP 
                       ORDER BY id DESC LIMIT 1";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@purpose", purpose);

        using var reader = cmd.ExecuteReader();
        return reader.Read(); // מחזיר true אם הקוד תקין ובתוקף
    }

    // =========================================================================
    // 9. בדיקת קוד אימות וסימונו כמשומש (מונע שימוש חוזר באותו קוד)
    // =========================================================================
    public bool ConsumeCode(string email, string code, string purpose)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = @"SELECT id FROM verification_codes 
                       WHERE email = @email AND code = @code AND purpose = @purpose 
                         AND is_used = FALSE AND expires_at > CURRENT_TIMESTAMP 
                       ORDER BY id DESC LIMIT 1";

        int recordId = 0;
        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@purpose", purpose);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                recordId = Convert.ToInt32(reader["id"]);
            }
        }

        // אם נמצא קוד תקף - נסמן אותו כמשומש
        if (recordId > 0)
        {
            string updateSql = "UPDATE verification_codes SET is_used = TRUE WHERE id = @id";
            using var upCmd = new NpgsqlCommand(updateSql, conn);
            upCmd.Parameters.AddWithValue("@id", recordId);
            upCmd.ExecuteNonQuery();
            return true;
        }

        return false;
    }

    // =========================================================================
    // 10. מחיקת משתמש לפי מזהה ID (עבור פאנל הניהול)
    // =========================================================================
    public void DeleteUser(int id)
    {
        using var conn = GetConnection();
        conn.Open();

        string sql = "DELETE FROM users WHERE id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 11. שליפת כל המשתמשים לטבלת פאנל הניהול
    // =========================================================================
    public List<object> GetAllUsers()
    {
        var list = new List<object>();
        using var conn = GetConnection();
        conn.Open();

        string sql = "SELECT id, email, password_hash, username, full_name, is_verified, last_login, created_at FROM users ORDER BY id ASC";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            // בדיקה אם השדות אינם ריקים (null)
            var lastLoginVal = reader["last_login"] == DBNull.Value ? null : reader["last_login"].ToString();
            var createdAtVal = reader["created_at"] == DBNull.Value ? null : reader["created_at"].ToString();

            // הוספת המשתמש לרשימה
            list.Add(new
            {
                id = Convert.ToInt32(reader["id"]),
                email = reader["email"]?.ToString() ?? "",
                password_hash = reader["password_hash"]?.ToString() ?? "",
                passwordHash = reader["password_hash"]?.ToString() ?? "",
                username = reader["username"]?.ToString() ?? "",
                full_name = reader["full_name"]?.ToString() ?? "",
                is_verified = reader["is_verified"] != DBNull.Value && Convert.ToBoolean(reader["is_verified"]),
                last_login = lastLoginVal,
                lastLogin = lastLoginVal,
                created_at = createdAtVal,
                createdAt = createdAtVal,
                is_online = lastLoginVal != null
            });
        }

        return list;
    }
}
