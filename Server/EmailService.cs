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

    // פונקציה ראשית לשליחת קוד אימות באימייל
    public bool SendCode(string toEmail, string subject, string otpCode, string action)
    {
        // שליפת הגדרות שרת הדואר מקובץ ההגדרות של האפליקציה
        var smtp = _config.GetSection("SmtpSettings");
        // כתובת שרת הדואר היוצא של גוגל
        string host = smtp["Host"] ?? "smtp.gmail.com";
        // פורט התקשורת המאובטח 587
        int port = int.TryParse(smtp["Port"], out int p) ? p : 587;
        // כתובת האימייל שממנה נשלחת ההודעה
        string senderEmail = smtp["SenderEmail"] ?? "shadownet.security@gmail.com";
        // שם המשתמש לחשבון השליחה
        string username = smtp["Username"] ?? "";
        // קריאת סיסמת השרת בצורה מאובטחת מתוך משתני הסביבה של המערכת
        string password = Environment.GetEnvironmentVariable("SHADOWNET_SMTP_PASSWORD") 
                       ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD") 
                       ?? smtp["Password"] 
                       ?? "";

        // בדיקה אם חסרים פרטי הזדהות כדי לא להכשיל את המערכת בזמן פיתוח
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // הדפסת הודעה לקונסול שהשליחה נדלגה בגלל חוסר בסיסמה
            Console.WriteLine("[Email Service] SMTP credentials not set, skipping real send.");
            // החזרת תשובה חיובית כדי לאפשר המשך בדיקות
            return true;
        }

        try
        {
            // פתיחת חיבור לקוח מול שרת הדואר של גוגל
            using var client = new SmtpClient(host, port)
            {
                // העברת שם המשתמש והסיסמה של חשבון השולח
                Credentials = new NetworkCredential(username, password),
                // הפעלת הצפנת רשת מאובטחת
                EnableSsl = true
            };

            // 4. בניית תוכן הודעת הדוא"ל המעוצבת בסגנון מיתוג ShadowNet
            string htmlContent = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>ShadowNet Security Code</title>
</head>
<body style=""margin:0; padding:0; background-color:#030712; font-family:'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, sans-serif; color:#f8fafc;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#030712; padding:40px 15px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" style=""max-width:520px; background-color:#0b1329; border:1px solid #1e293b; border-radius:16px; overflow:hidden; box-shadow:0 20px 40px rgba(0,0,0,0.6);"">
          <!-- כותרת ראשית ומיתוג ShadowNet -->
          <tr>
            <td style=""background: linear-gradient(135deg, #070d1e 0%, #0c1a38 100%); padding:32px 30px; text-align:center; border-bottom:1px solid #1e3a5f;"">
              <div style=""display:inline-block; padding:5px 12px; background:rgba(14,165,233,0.12); border:1px solid rgba(14,165,233,0.3); border-radius:20px; font-size:11px; font-weight:700; color:#38bdf8; letter-spacing:1.5px; text-transform:uppercase; margin-bottom:12px;"">
                SHADOWNET // SECURE DISPATCH
              </div>
              <h1 style=""margin:0; font-size:24px; font-weight:800; color:#ffffff; letter-spacing:0.5px;"">
                Verification Passcode
              </h1>
              <p style=""margin:6px 0 0 0; font-size:13px; color:#94a3b8;"">
                {action} Request
              </p>
            </td>
          </tr>

          <!-- גוף ההודעה -->
          <tr>
            <td style=""padding:32px 30px; text-align:center;"">
              <p style=""margin:0 0 22px 0; font-size:14px; line-height:1.6; color:#cbd5e1;"">
                Use the 6-digit one-time passcode below to verify your identity on <strong style=""color:#ffffff;"">ShadowNet</strong>:
              </p>

              <!-- תיבת קוד האימות המודגשת עם זוהר כחול -->
              <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                <tr>
                  <td align=""center"">
                    <div style=""display:inline-block; background-color:#020617; border:2px solid #0284c7; border-radius:12px; padding:18px 36px; box-shadow:0 0 25px rgba(2,132,199,0.35);"">
                      <span style=""font-family:'Courier New', Consolas, monospace; font-size:36px; font-weight:900; letter-spacing:8px; color:#38bdf8; text-shadow:0 0 10px rgba(56,189,248,0.5);"">
                        {otpCode}
                      </span>
                    </div>
                  </td>
                </tr>
              </table>

              <!-- התראת תוקף ואבטחה -->
              <p style=""margin:22px 0 0 0; font-size:12px; color:#94a3b8; display:inline-block; padding:6px 14px; background:rgba(255,255,255,0.03); border-radius:6px; border:1px solid rgba(255,255,255,0.08);"">
                ⏱ Valid for <strong style=""color:#f8fafc;"">10 minutes</strong>. Never share this code.
              </p>
            </td>
          </tr>

          <!-- תחתית ההודעה -->
          <tr>
            <td style=""background-color:#050a18; padding:20px 30px; text-align:center; border-top:1px solid #131e36; font-size:11px; color:#64748b; line-height:1.5;"">
              <p style=""margin:0 0 4px 0;"">
                Automated security dispatch from <strong style=""color:#94a3b8;"">ShadowNet Tactical Network</strong>.
              </p>
              <p style=""margin:0;"">
                If you did not initiate this request, your account remains secure and no action is required.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            // יצירת אובייקט הודעת אימייל חדשה
            var mail = new MailMessage
            {
                // הגדרת כתובת השולח ושם התצוגה בעברית ובאנגלית
                From = new MailAddress(senderEmail, "ShadowNet Security", Encoding.UTF8),
                // נושא הודעת הדואל
                Subject = $"ShadowNet - {subject}",
                // תוכן גוף המייל המעוצב בפורמט עשיר
                Body = htmlContent,
                // הגדרה שההודעה מכילה קוד תצוגה עשיר
                IsBodyHtml = true,
                // קידוד תוכן ההודעה
                BodyEncoding = Encoding.UTF8
            };

            // הוספת כתובת האימייל של הנמען שאליו שולחים את הקוד
            mail.To.Add(toEmail);

            // שליחת הודעת הדואל בפועל דרך שרת האינטרנט
            client.Send(mail);
            // הדפסת הודעת אישור לקונסול של השרת
            Console.WriteLine($"[Email Service] Sent verification code to {toEmail}");
            // החזרת תשובת הצלחה
            return true;
        }
        catch (Exception ex)
        {
            // הדפסת הודעת שגיאה אם שליחת המייל נכשלה
            Console.WriteLine($"[Email Error] Failed to send email: {ex.Message}");
            // החזרת תשובה שלילית במקרה של תקלה
            return false;
        }
    }
}
