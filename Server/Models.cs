namespace ShadowNet.Server;

// 
// קובץ זה מגדיר את ה-DTOs (Data Transfer Objects).
// אלו מחלקות פשוטות שמחזיקות את הנתונים שעוברים בין הדפדפן לשרת שלנו.
// 

// מודל שמייצג משתמש שנשלף מטבלת users במסד הנתונים
public class UserRecord
{
    // מזהה ייחודי של המשתמש (מספר רץ בטבלה)
    public int Id { get; set; }

    // כתובת האימייל של המשתמש
    public string Email { get; set; } = "";

    // הסיסמה המוצפנת (בפורמט SHA-256 hex)
    public string PasswordHash { get; set; } = "";

    // כינוי המשתמש במערכת
    public string Username { get; set; } = "";

    // השם המלא של המשתמש
    public string FullName { get; set; } = "";
}

// מודל לקבלת אימייל וסיסמה בטופס התחברות (Login)
public class UserRequest
{
    // האימייל שהמשתמש הקליד
    public string Email { get; set; } = "";

    // הסיסמה הרגילה שהמשתמש הקליד
    public string Password { get; set; } = "";
}

// מודל לבדיקת קוד אימות שהמשתמש הזין בדף
public class OtpVerifyRequest
{
    // האימייל שעבורו נשלח הקוד
    public string Email { get; set; } = "";

    // קוד האימות בן 6 ספרות שהמשתמש הקליד
    public string Code { get; set; } = "";
}

// מודל לסיום תהליך ההרשמה (שלב 3 ברישום)
public class RegisterCompleteRequest
{
    // האימייל המאומת
    public string Email { get; set; } = "";

    // קוד האימות שאושר
    public string Code { get; set; } = "";

    // השם המלא שהמשתמש בחר
    public string FullName { get; set; } = "";

    // שם המשתמש שהמשתמש בחר
    public string Username { get; set; } = "";

    // הסיסמה החדשה שהמשתמש בחר
    public string Password { get; set; } = "";
}

// מודל לבקשת שליחת קוד אימות לאיפוס סיסמה או הרשמה
public class ResetSendCodeRequest
{
    // האימייל שאליו יש לשלוח את הקוד
    public string Email { get; set; } = "";
}

// מודל לאיפוס סיסמה בפועל (הזנת קוד וסיסמה חדשה)
public class ResetWithOtpRequest
{
    // האימייל של החשבון
    public string Email { get; set; } = "";

    // קוד האימות שהתקבל במייל
    public string Code { get; set; } = "";

    // הסיסמה החדשה שהמשתמש רוצה להגדיר
    public string NewPassword { get; set; } = "";
}

// מודל לבקשת שליחה חוזרת של קוד אימות
public class ResendRequest
{
    // האימייל לשליחה חוזרת
    public string Email { get; set; } = "";

    // מטרת הקוד: "register" או "reset"
    public string Purpose { get; set; } = "login";
}
