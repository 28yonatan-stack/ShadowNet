using System.Security.Cryptography; // ספריית הצפנה מתקדמת של מיקרוסופט
using System.Text; // מאפשרת להמיר מחרוזות טקסט למערך של בתים (bytes)

namespace ShadowNet.Server;

// =============================================================================
// מחלקת עזר לאבטחה והצפנה (SecurityHelper)
// מחלקה סטטית שמכילה פונקציות עזר שלא דורשות יצירת אובייקט עם new
// =============================================================================
public static class SecurityHelper
{
    // פונקציה שמייצרת קוד אימות חד-פעמי בן 6 ספרות
    public static string GenerateOtpCode()
    {
        // מייצר מספר אקראי מאובטח בין 100,000 ל-999,999 (תמיד 6 ספרות)
        int code = RandomNumberGenerator.GetInt32(100000, 1000000);

        // הופך את המספר לטקסט ומחזיר אותו
        return code.ToString();
    }

    // פונקציה שמצפינה סיסמה או מחרוזת טקסט באמצעות אלגוריתם SHA-256
    public static string HashPassword(string text)
    {
        // יצירת מופע של אלגוריתם SHA-256
        using var sha = SHA256.Create();

        // המרת מחרוזת הטקסט לרצף בתים בקידוד UTF-8
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        // חישוב ההאש המוצפן מתוך הבתים
        byte[] hash = sha.ComputeHash(bytes);

        // המרת תוצאת ההאש למחרוזת בפורמט Hexadecimal (אותיות ומספרים)
        return Convert.ToHexString(hash);
    }
}
