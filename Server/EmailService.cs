using System.Net; // מחלקות לניהול כתובות ואישורי רשת
using System.Net.Mail; // ספריית .NET הרשמית לשליחת דואר אלקטרוני (SMTP)
using System.Text; // קידוד טקסט בעברית ובאנגלית

namespace ShadowNet.Server;

// 
// מחלקת שירות לשליחת אימיילים (EmailService)
// אחראית לשליחת קודי אימות אמיתיים לתיבת הדוא"ל של המשתמשים
// 
public class EmailService
{
    // משתנה לקריאת הגדרות מתוך appsettings.json
    private readonly IConfiguration _config;

    // בנאי שמקבל את אובייקט ההגדרות של השרת
    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // פונקציה לשליחת קוד אימות באימייל
    public bool SendCode(string toEmail, string subject, string otpCode, string action)
    {
        // 1. קריאת הגדרות שרת הדואר מקובץ ההגדרות
        var smtp = _config.GetSection("SmtpSettings");
        string host = smtp["Host"] ?? "smtp.gmail.com";
        int port = int.TryParse(smtp["Port"], out int p) ? p : 587;
        string senderEmail = smtp["SenderEmail"] ?? "shadownet.security@gmail.com";
        string username = smtp["Username"] ?? "";
        string password = smtp["Password"] ?? "";

        // 2. אם לא הוגדרו שם משתמש וסיסמה, לא נכשיל את הפעולה
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("[Email Service] SMTP credentials not set, skipping real send.");
            return true;
        }

        try
        {
            // 3. הגדרת לקוח SMTP מול שרת Gmail
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password), // אימות מול גוגל
                EnableSsl = true // חיבור מאובטח ומוצפן ב-SSL/TLS
            };

            // 4. בניית תוכן הודעת הדוא"ל
            var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, "ShadowNet Security", Encoding.UTF8),
                Subject = $"ShadowNet - {subject}",
                Body = $"Hello,\n\nYour verification code for {action} is:\n\n    {otpCode}\n\nThis code is valid for 10 minutes.\nIf you did not request this, please ignore this email.",
                BodyEncoding = Encoding.UTF8
            };

            // הוספת כתובת הנמען
            mail.To.Add(toEmail);

            // שליחת המייל בפועל
            client.Send(mail);
            Console.WriteLine($"[Email Service] Sent verification code to {toEmail}");
            return true;
        }
        catch (Exception ex)
        {
            // אם הייתה שגיאה בשליחה, נדפיס אותה למסך השרת
            Console.WriteLine($"[Email Error] Failed to send email: {ex.Message}");
            return false;
        }
    }
}
