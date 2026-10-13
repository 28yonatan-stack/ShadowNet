using Microsoft.Extensions.FileProviders; // ספרייה להגשת קבצים סטטיים מהתיקייה של האתר

namespace ShadowNet.Server;

// =============================================================================
// מחלקה ראשית (Program)
// נקודת ההתחלה של אפליקציית השרת (Entry Point)
// כאן מוגדרות כל כתובות ה-API (Endpoints) שהאתר מתקשר איתן
// =============================================================================
public class Program
{
    public static void Main(string[] args)
    {
        // 1. יצירת בנאי עבור אפליקציית האינטרנט
        var builder = WebApplication.CreateBuilder(args);

        // 2. הוספת תמיכה ב-Swagger (דף תיעוד ובדיקה אוטומטי של ה-APIs)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 3. הגדרת CORS - מאפשר לדפדפנים ממקורות שונים לשלוח בקשות לשרת ללא חסימות
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()   // מתיר כל דומיין / פורט
                      .AllowAnyMethod()   // מתיר כל סוג בקשה (GET, POST, DELETE וכו')
                      .AllowAnyHeader();  // מתיר כל כותרת HTTP
            });
        });

        // 4. קריאת מחרוזת החיבור למסד הנתונים מתוך appsettings.json
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

        // 5. בניית האפליקציה (יצירת אובייקט השרת)
        var app = builder.Build();

        // הפעלת דף ה-Swagger בדפדפן
        app.UseSwagger();
        app.UseSwaggerUI();

        // הפעלת מדיניות ה-CORS שהגדרנו למעלה
        app.UseCors("AllowAll");

        // 6. הגדרת תיקיית הלקוח (HTML, CSS, JS) כדי שהשרת יציג את דפי האתר
        string clientFolder = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "ShadowNet-Client"));
        if (Directory.Exists(clientFolder))
        {
            var provider = new PhysicalFileProvider(clientFolder);

            // הגדרת דף ברירת מחדל (login.html)
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = provider,
                DefaultFileNames = new List<string> { "login.html", "index.html" }
            });

            // הגשת קבצים סטטיים ומניעת שמירתם במטמון הדפדפן (כדי ששינויי קוד יתעדכנו מיד)
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider,
                RequestPath = "",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                    ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                    ctx.Context.Response.Headers.Append("Expires", "0");
                }
            });
        }

        // 7. יצירת מופעים של מחלקות העזר שלנו
        var db = new DatabaseHelper(connectionString);
        var emailSender = new EmailService(builder.Configuration);

        // 8. בדיקת מסד נתונים ויצירת הטבלאות אם עדיין לא קיימות
        db.InitializeTables();

        // =====================================================================
        // נקודות הקצה של השרת (Endpoints / API Routes)
        // =====================================================================

        // בדיקת תקינות שהשרת באוויר ועובד (Health Check)
        app.MapGet("/api/health", () =>
        {
            return Results.Ok(new { status = "online", project = "ShadowNet" });
        });

        // פאנל מנהל: שליפת רשימת כל המשתמשים מהמסד
        app.MapGet("/api/users", () =>
        {
            var usersList = db.GetAllUsers();
            return Results.Ok(usersList);
        });

        // פאנל מנהל: מחיקת משתמש לפי מספר המזהה שלו (ID)
        app.MapDelete("/api/users/{id:int}", (int id) =>
        {
            db.DeleteUser(id);
            return Results.Ok(new { success = true, message = "User deleted successfully." });
        });

        // הרשמה - שלב 1: שליחת קוד אימות למייל של הנרשם
        app.MapPost("/api/auth/register-send-code", (ResetSendCodeRequest req) =>
        {
            // בדיקה שהמשתמש הזין כתובת אימייל
            if (string.IsNullOrWhiteSpace(req.Email))
            {
                return Results.BadRequest(new { success = false, message = "Email address is required." });
            }

            string email = req.Email.Trim().ToLower();

            // בדיקה האם האימייל כבר קיים במערכת
            if (db.UserExists(email))
            {
                return Results.BadRequest(new { success = false, message = "An account with this email already exists." });
            }

            // יצירת קוד אימות אקראי בן 6 ספרות
            string code = SecurityHelper.GenerateOtpCode();

            // שמירת הקוד בטבלת verification_codes
            db.SaveVerificationCode(email, code, "register");

            // שליחת הקוד למייל של המשתמש
            emailSender.SendCode(email, "Registration Verification Code", code, "Account Registration");

            return Results.Ok(new { success = true, email = email, message = $"Verification code sent to {email}." });
        });

        // הרשמה - שלב 2: אימות הקוד שהמשתמש הקליד
        app.MapPost("/api/auth/verify-register-code", (OtpVerifyRequest req) =>
        {
            // בדיקה שנמסרו אימייל וקוד
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code))
            {
                return Results.BadRequest(new { success = false, message = "Email and verification code are required." });
            }

            string email = req.Email.Trim().ToLower();
            string code = req.Code.Trim();

            // בדיקה מול מסד הנתונים האם הקוד תקף
            if (!db.IsCodeValid(email, code, "register"))
            {
                return Results.BadRequest(new { success = false, message = "Invalid or expired verification code." });
            }

            return Results.Ok(new { success = true, message = "Email verified successfully!" });
        });

        // הרשמה - שלב 3: סיום ההרשמה ושמירת המשתמש החדש במסד
        app.MapPost("/api/auth/register-complete", (RegisterCompleteRequest req) =>
        {
            // בדיקה שכל השדות הנדרשים מולאו
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { success = false, message = "Email, code and password are required." });
            }

            string email = req.Email.Trim().ToLower();
            string code = req.Code.Trim();

            // בדיקת הקוד וסימונו כמשומש (כדי שלא יוכלו להשתמש בו שוב)
            if (!db.ConsumeCode(email, code, "register"))
            {
                return Results.BadRequest(new { success = false, message = "Verification code is invalid or expired." });
            }

            // הצפנת הסיסמה באלגוריתם SHA-256
            string hash = SecurityHelper.HashPassword(req.Password);

            // קביעת כינוי ושם מלא (אם ריק - משתמשים בחלק שלפני ה-@ באימייל)
            string username = string.IsNullOrWhiteSpace(req.Username) ? email.Split('@')[0] : req.Username.Trim();
            string fullName = string.IsNullOrWhiteSpace(req.FullName) ? username : req.FullName.Trim();

            // יצירת המשתמש בטבלת users
            db.CreateUser(email, hash, username, fullName);

            return Results.Ok(new { success = true, message = "Account created successfully! You can now log in." });
        });

        // התחברות - אימות אימייל וסיסמה
        app.MapPost("/api/auth/login", (UserRequest req) =>
        {
            // בדיקה שהוזנו אימייל וסיסמה
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { success = false, message = "Email and password are required." });
            }

            string email = req.Email.Trim().ToLower();

            // הצפנת הסיסמה שהוזנה כדי להשוות מול ההאש ששמור במסד
            string hash = SecurityHelper.HashPassword(req.Password);

            // שליפת המשתמש ממסד הנתונים
            var user = db.GetUserByEmail(email);

            // בדיקה האם המשתמש קיים והסיסמה המוצפנת תואמת
            if (user != null && user.PasswordHash == hash)
            {
                // עדכון זמן כניסה אחרון
                db.UpdateLastLogin(user.Id);

                // החזרת פרטי המשתמש לדפדפן
                return Results.Ok(new
                {
                    success = true,
                    requireOtp = false,
                    message = "Logged in successfully to ShadowNet!",
                    userId = user.Id,
                    email = user.Email,
                    username = user.Username,
                    fullName = user.FullName,
                    sha256 = hash
                });
            }

            // אם הפרטים שגויים - מחזירים שגיאת 401 Unauthorized
            return Results.Json(new { success = false, message = "Invalid email or password." }, statusCode: 401);
        });

        // איפוס סיסמה - שלב 1: שליחת קוד אימות לאימייל
        app.MapPost("/api/auth/reset-send-code", (ResetSendCodeRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email))
            {
                return Results.BadRequest(new { success = false, message = "Email address is required." });
            }

            string email = req.Email.Trim().ToLower();

            // בדיקה שהאימייל אכן קיים במערכת
            if (!db.UserExists(email))
            {
                return Results.NotFound(new { success = false, message = "No account found with this email address." });
            }

            // יצירת קוד חדש ושמירתו
            string code = SecurityHelper.GenerateOtpCode();
            db.SaveVerificationCode(email, code, "reset");

            // שליחת הקוד למייל
            emailSender.SendCode(email, "Password Reset Code", code, "Password Reset");

            return Results.Ok(new { success = true, email = email, message = $"Verification code sent to {email}." });
        });

        // איפוס סיסמה - שלב 2: אימות הקוד שהמשתמש הקליד
        app.MapPost("/api/auth/verify-reset-code", (OtpVerifyRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code))
            {
                return Results.BadRequest(new { success = false, message = "Email and verification code are required." });
            }

            string email = req.Email.Trim().ToLower();
            string code = req.Code.Trim();

            // בדיקה מול המסד
            if (!db.IsCodeValid(email, code, "reset"))
            {
                return Results.BadRequest(new { success = false, message = "Invalid or expired verification code." });
            }

            return Results.Ok(new { success = true, message = "Code verified successfully." });
        });

        // איפוס סיסמה - שלב 3: שמירת סיסמה חדשה
        app.MapPost("/api/auth/reset-password", (ResetWithOtpRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return Results.BadRequest(new { success = false, message = "Email and new password are required." });
            }

            string email = req.Email.Trim().ToLower();

            // אם נמסר קוד אימות - מסמנים אותו כמשומש
            if (!string.IsNullOrWhiteSpace(req.Code))
            {
                if (!db.ConsumeCode(email, req.Code.Trim(), "reset"))
                {
                    return Results.BadRequest(new { success = false, message = "Invalid or expired verification code." });
                }
            }

            // הצפנת הסיסמה החדשה ב-SHA-256
            string newHash = SecurityHelper.HashPassword(req.NewPassword);

            // עדכון הסיסמה בטבלת המשתמשים
            db.UpdatePassword(email, newHash);

            return Results.Ok(new { success = true, message = "Password updated successfully! You can now log in." });
        });

        // שליחה חוזרת של קוד אימות (Resend Code)
        app.MapPost("/api/auth/resend-code", (ResendRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email))
            {
                return Results.BadRequest(new { success = false, message = "Email address is required to resend code." });
            }

            string email = req.Email.Trim().ToLower();
            string purpose = string.IsNullOrWhiteSpace(req.Purpose) ? "login" : req.Purpose.Trim().ToLower();

            // יצירת קוד חדש ושליחתו
            string code = SecurityHelper.GenerateOtpCode();
            db.SaveVerificationCode(email, code, purpose);
            emailSender.SendCode(email, "New Verification Code", code, purpose);

            return Results.Ok(new { success = true, email = email, message = $"New verification code sent successfully to {email}!" });
        });

        // הפעלת השרת והאזנה לבקשות
        app.Run();
    }
}
