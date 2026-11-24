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
        // פתיחת חיבור חדש ומאובטח למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור בפועל
        conn.Open();

        // שאילתה להוספת שורה חדשה עם פרמטרים מבודדים למניעת הזרקת קוד
        string sql = @"INSERT INTO users (email, password_hash, username, full_name, is_verified, last_login) 
                       VALUES (@email, @password_hash, @username, @full_name, TRUE, CURRENT_TIMESTAMP)";

        // יצירת פקודת ההרצה של השאילתה מול החיבור הפתוח
        using var cmd = new NpgsqlCommand(sql, conn);
        // העברת כתובת האימייל בצורה מאובטחת
        cmd.Parameters.AddWithValue("@email", email);
        // העברת הסיסמה המוצפנת
        cmd.Parameters.AddWithValue("@password_hash", passwordHash);
        // העברת שם המשתמש
        cmd.Parameters.AddWithValue("@username", username);
        // העברת השם המלא של המשתמש
        cmd.Parameters.AddWithValue("@full_name", fullName);

        // הרצת פקודת ההוספה במסד הנתונים
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 4. שליפת משתמש לפי אימייל עבור התחברות
    // =========================================================================
    public UserRecord? GetUserByEmail(string email)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת ערוץ התקשורת
        conn.Open();

        // שאילתה לשליפת פרטי המשתמש לפי האימייל שלו
        string sql = "SELECT id, email, password_hash, username, full_name FROM users WHERE email = @email";
        // יצירת פקודת השאילתה
        using var cmd = new NpgsqlCommand(sql, conn);
        // העברת האימייל כפרמטר מאובטח
        cmd.Parameters.AddWithValue("@email", email);

        // הרצת השאילתה וקבלת קורא נתונים
        using var reader = cmd.ExecuteReader();
        // אם נמצאה שורה תואמת במסד הנתונים
        if (reader.Read())
        {
            // יצירת אובייקט משתמש עם הנתונים שנשלפו והחזרתו
            return new UserRecord
            {
                // המרת המספר המזהה
                Id = Convert.ToInt32(reader["id"]),
                // חילוץ האימייל
                Email = reader["email"].ToString()!,
                // חילוץ הסיסמה המוצפנת
                PasswordHash = reader["password_hash"].ToString()!,
                // חילוץ שם המשתמש או ברירת מחדל
                Username = reader["username"]?.ToString() ?? email.Split('@')[0],
                // חילוץ השם המלא
                FullName = reader["full_name"]?.ToString() ?? ""
            };
        }

        // אם המשתמש לא נמצא כלל במסד הנתונים
        return null;
    }

    // =========================================================================
    // 5. עדכון זמן כניסה אחרון למערכת
    // =========================================================================
    public void UpdateLastLogin(int userId)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור
        conn.Open();

        // שאילתה לעדכון שעת ההתחברות לשעה הנוכחית לפי מספר המשתמש
        string sql = "UPDATE users SET last_login = CURRENT_TIMESTAMP WHERE id = @id";
        // יצירת אובייקט הפקודה
        using var cmd = new NpgsqlCommand(sql, conn);
        // העברת מספר הזיהוי של המשתמש כפרמטר מאובטח
        cmd.Parameters.AddWithValue("@id", userId);
        // הרצת העדכון במסד
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 6. עדכון סיסמה חדשה למשתמש בעת איפוס סיסמה
    // =========================================================================
    public void UpdatePassword(string email, string newHash)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור
        conn.Open();

        // שאילתה לעדכון הסיסמה המוצפנת החדשה
        string sql = "UPDATE users SET password_hash = @newHash WHERE email = @email";
        // יצירת הפקודה
        using var cmd = new NpgsqlCommand(sql, conn);
        // העברת כתובת האימייל
        cmd.Parameters.AddWithValue("@email", email);
        // העברת ההאש המוצפן של הסיסמה החדשה
        cmd.Parameters.AddWithValue("@newHash", newHash);
        // הרצת פקודת העדכון
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 7. שמירת קוד אימות חדש עם תוקף ל-10 דקות
    // =========================================================================
    public void SaveVerificationCode(string email, string code, string purpose)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת ערוץ התקשורת
        conn.Open();

        // ביטול ופסילת קודים ישנים שלא נוצלו עבור אותו אימייל כדי למנוע כפילויות
        string cancelSql = "UPDATE verification_codes SET is_used = TRUE WHERE email = @email AND purpose = @purpose AND is_used = FALSE";
        using (var cancelCmd = new NpgsqlCommand(cancelSql, conn))
        {
            // העברת האימייל כפרמטר
            cancelCmd.Parameters.AddWithValue("@email", email);
            // העברת מטרת הקוד כפרמטר
            cancelCmd.Parameters.AddWithValue("@purpose", purpose);
            // ביצוע הפסילה בפועל
            cancelCmd.ExecuteNonQuery();
        }

        // הכנסת הקוד החדש עם תוקף מדויק ל-10 דקות מהרגע הנוכחי
        string insertSql = @"INSERT INTO verification_codes (email, code, purpose, expires_at) 
                             VALUES (@email, @code, @purpose, CURRENT_TIMESTAMP + INTERVAL '10 minutes')";
        using (var insertCmd = new NpgsqlCommand(insertSql, conn))
        {
            // העברת האימייל
            insertCmd.Parameters.AddWithValue("@email", email);
            // העברת קוד האימות בן שש הספרות
            insertCmd.Parameters.AddWithValue("@code", code);
            // העברת מטרת הקוד
            insertCmd.Parameters.AddWithValue("@purpose", purpose);
            // הרצת השאילתה ושמירת הקוד בטבלה
            insertCmd.ExecuteNonQuery();
        }
    }

    // =========================================================================
    // 8. בדיקה האם קוד אימות תקין ונמצא בתוקף
    // =========================================================================
    public bool IsCodeValid(string email, string code, string purpose)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור
        conn.Open();

        // שאילתה לבדיקת קוד פעיל שלא נוצל שטרם פג תוקפו
        string sql = @"SELECT id FROM verification_codes 
                       WHERE email = @email AND code = @code AND purpose = @purpose 
                         AND is_used = FALSE AND expires_at > CURRENT_TIMESTAMP 
                       ORDER BY id DESC LIMIT 1";

        // יצירת פקודת ההרצה
        using var cmd = new NpgsqlCommand(sql, conn);
        // העברת האימייל לבדיקה
        cmd.Parameters.AddWithValue("@email", email);
        // העברת הקוד לבדיקה
        cmd.Parameters.AddWithValue("@code", code);
        // העברת מטרת הקוד לבדיקה
        cmd.Parameters.AddWithValue("@purpose", purpose);

        // הרצת הקריאה מול מסד הנתונים
        using var reader = cmd.ExecuteReader();
        // אם הצליח לקרוא שורה סימן שהקוד תקין ופעיל
        return reader.Read();
    }

    // =========================================================================
    // 9. בדיקת קוד אימות וסימונו כמשומש מיידית
    // =========================================================================
    public bool ConsumeCode(string email, string code, string purpose)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור
        conn.Open();

        // שאילתה לאיתור הקוד התקף
        string sql = @"SELECT id FROM verification_codes 
                       WHERE email = @email AND code = @code AND purpose = @purpose 
                         AND is_used = FALSE AND expires_at > CURRENT_TIMESTAMP 
                       ORDER BY id DESC LIMIT 1";

        // משתנה לשמירת מזהה הרשומה שנמצאה
        int recordId = 0;
        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            // העברת האימייל
            cmd.Parameters.AddWithValue("@email", email);
            // העברת הקוד
            cmd.Parameters.AddWithValue("@code", code);
            // העברת המטרה
            cmd.Parameters.AddWithValue("@purpose", purpose);

            // הרצת השאילתה
            using var reader = cmd.ExecuteReader();
            // אם נמצאה רשומה תואמת
            if (reader.Read())
            {
                // שמירת מספר המזהה של הקוד
                recordId = Convert.ToInt32(reader["id"]);
            }
        }

        // אם אכן נמצא קוד חוקי נסמן אותו מיידית כמשומש
        if (recordId > 0)
        {
            // שאילתה לפסילת הקוד כדי למנוע שימוש חוזר
            string updateSql = "UPDATE verification_codes SET is_used = TRUE WHERE id = @id";
            using var upCmd = new NpgsqlCommand(updateSql, conn);
            // העברת מספר הזיהוי של הקוד
            upCmd.Parameters.AddWithValue("@id", recordId);
            // ביצוע הפסילה במסד הנתונים
            upCmd.ExecuteNonQuery();
            // החזרת הצלחה
            return true;
        }

        // אם לא נמצא קוד תקין
        return false;
    }

    // =========================================================================
    // 10. מחיקת משתמש לפי מזהה עבור פאנל הניהול
    // =========================================================================
    public void DeleteUser(int id)
    {
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור
        conn.Open();

        // שאילתה למחיקת רשומת המשתמש מטבלת המשתמשים
        string sql = "DELETE FROM users WHERE id = @id";
        // יצירת פקודת המחיקה
        using var cmd = new NpgsqlCommand(sql, conn);
        // העברת מספר הזיהוי כפרמטר מאובטח
        cmd.Parameters.AddWithValue("@id", id);
        // הרצת פקודת המחיקה
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 11. שליפת כל המשתמשים לטבלת הניהול של האדמין
    // =========================================================================
    public List<object> GetAllUsers()
    {
        // יצירת רשימה ריקה שתחזיק את כל המשתמשים
        var list = new List<object>();
        // פתיחת חיבור למסד הנתונים
        using var conn = GetConnection();
        // פתיחת החיבור
        conn.Open();

        // שאילתה לשליפת כל המשתמשים ממוינים לפי סדר ההרשמה שלהם
        string sql = "SELECT id, email, password_hash, username, full_name, is_verified, last_login, created_at FROM users ORDER BY id ASC";
        // יצירת פקודת השליפה
        using var cmd = new NpgsqlCommand(sql, conn);
        // הרצת הקורא לקבלת כל השורות
        using var reader = cmd.ExecuteReader();

        // מעבר שורה אחר שורה על כל המשתמשים שנמצאו
        while (reader.Read())
        {
            // בדיקה וטיפול בערכי זמן ריקים
            var lastLoginVal = reader["last_login"] == DBNull.Value ? null : reader["last_login"].ToString();
            var createdAtVal = reader["created_at"] == DBNull.Value ? null : reader["created_at"].ToString();

            // הוספת אובייקט משתמש מעוצב לתוך הרשימה
            list.Add(new
            {
                // מספר מזהה
                id = Convert.ToInt32(reader["id"]),
                // כתובת אימייל
                email = reader["email"]?.ToString() ?? "",
                // סיסמה מוצפנת
                password_hash = reader["password_hash"]?.ToString() ?? "",
                passwordHash = reader["password_hash"]?.ToString() ?? "",
                // שם משתמש
                username = reader["username"]?.ToString() ?? "",
                // שם מלא
                full_name = reader["full_name"]?.ToString() ?? "",
                // האם המשתמש מאומת
                is_verified = reader["is_verified"] != DBNull.Value && Convert.ToBoolean(reader["is_verified"]),
                // שעת התחברות אחרונה
                last_login = lastLoginVal,
                lastLogin = lastLoginVal,
                // שעת יצירת החשבון
                created_at = createdAtVal,
                createdAt = createdAtVal,
                // האם המשתמש מחובר
                is_online = lastLoginVal != null
            });
        }

        // החזרת רשימת כל המשתמשים
        return list;
    }
}
