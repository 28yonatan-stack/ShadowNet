using System.Net; // ספרייה לניהול כתובות IP ופורטים ברשת
using System.Net.Sockets; // ספריית הסוקטים הרשמית של C# לעבודה מול Windows Sockets API
using System.Text; // ספרייה להמרת טקסט למערך של בתים (Bytes) ובחזרה

namespace TacticalSocketDemo;

// =============================================================================
// ShadowNet Tactical Socket Demo - הדגמת תקשורת סוקטים טקטית
// קובץ זה ממחיש תקשורת ישירה ברמת פרוטוקול TCP בין שרת מרכזי לסוכן שטח
// =============================================================================
class Program
{
    // הגדרת כתובת ה-IP והפורט של ערוץ התקשורת המסווג
    private const string ServerIp = "127.0.0.1"; // כתובת מקומית (Localhost)
    private const int ServerPort = 5050; // פורט ייעודי לערוץ הסוקטים

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8; // תמיכה מלאה בהצגת עברית בטרמינל
        Console.ForegroundColor = ConsoleColor.Cyan; // צבע כותרת ציאן טקטי
        Console.WriteLine("==================================================");
        Console.WriteLine("    ShadowNet - Tactical TCP Socket System");
        Console.WriteLine("    ערוץ תקשורת סוקטים ישיר (TCP/IP)");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine("בחר מצב הפעלה להצגה למורה:");
        Console.WriteLine("1. הפעל שרת סוקט (Command Server - מאזין לסוכנים)");
        Console.WriteLine("2. הפעל סוכן שטח (Agent Client - שולח שדר מסווג)");
        Console.WriteLine("3. בדיקה חיה מהירה (מריץ שרת ולקוח יחד להדגמה מיידית)");
        Console.Write("הקלד 1, 2 או 3 ולחץ Enter: ");

        string choice = Console.ReadLine()?.Trim() ?? "3";
        Console.WriteLine();

        if (choice == "1")
        {
            // הפעלת שרת הסוקט בלבד
            RunServer();
        }
        else if (choice == "2")
        {
            // הפעלת לקוח הסוקט בלבד
            RunClient();
        }
        else
        {
            // בדיקה משולבת: מריצים את השרת ברקע, והלקוח מתחבר אליו מיידית
            RunLiveDemo();
        }
    }

    // =========================================================================
    // 1. שרת הסוקטים של המפקדה (Command Server)
    // =========================================================================
    public static void RunServer()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[SERVER] מתחיל תהליך פתיחת שרת סוקט...");
        Console.ResetColor();

        // שלב 1: יצירת אובייקט ה-Socket עבור פרוטוקול TCP
        // AddressFamily.InterNetwork = שימוש בכתובת IPv4
        // SocketType.Stream = זרם נתונים רציף ואמין (מאפיין של TCP)
        // ProtocolType.Tcp = בחירה מפורשת בפרוטוקול TCP
        using Socket listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // שלב 2: קשירת הסוקט לכתובת IP ולפורט (Bind)
        IPAddress ip = IPAddress.Parse(ServerIp); // המרת מחרוזת ה-IP לאובייקט
        IPEndPoint localEndPoint = new IPEndPoint(ip, ServerPort); // נקודת קצה (IP + Port)
        listenerSocket.Bind(localEndPoint); // קשירת הסוקט לכתובת

        // שלב 3: העמדת הסוקט במצב האזנה (Listen)
        // המספר 10 מגדיר כמה בקשות חיבור יכולות להמתין בתור בו-זמנית
        listenerSocket.Listen(10);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SERVER] השרת מאזין בהצלחה בכתובת: {ServerIp}:{ServerPort}");
        Console.WriteLine("[SERVER] ממתין לחיבור סוכן שטח (Waiting for client)...");
        Console.ResetColor();

        // שלב 4: קבלת חיבור של לקוח (Accept) - הפקודה עוצרת וממתינה עד שסוכן מתחבר
        using Socket clientSocket = listenerSocket.Accept();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SERVER] סוכן התחבר בהצלחה מכתובת: {clientSocket.RemoteEndPoint}");
        Console.ResetColor();

        // שלב 5: קבלת נתונים מהסוכן (Receive)
        byte[] buffer = new byte[1024]; // זיכרון זמני לקליטת הבתים (עד 1024 בתים)
        int bytesReceived = clientSocket.Receive(buffer); // קריאת המידע שנשלח דרך הסוקט
        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived); // המרת הבתים לטקסט

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[SERVER] שדר התקבל מהסוכן: \"{receivedMessage}\"");
        Console.ResetColor();

        // שלב 6: שליחת תשובה בחזרה לסוכן דרך הסוקט (Send)
        string responseMessage = $"[ACK] שדר התקבל במפקדה: {receivedMessage} // סיווג: TOP SECRET // סוקט פעיל!";
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage); // המרת הטקסט למערך בתים
        clientSocket.Send(responseBytes); // שליחה פיזית של הבתים לסוכן

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[SERVER] אישור קבלה (ACK) נשלח בהצלחה לסוכן.");
        Console.ResetColor();

        // שלב 7: סגירת החיבור בצורה מסודרת
        clientSocket.Shutdown(SocketShutdown.Both); // הפסקת שידור וקליטה
        clientSocket.Close(); // סגירת שקע הלקוח
        listenerSocket.Close(); // סגירת שקע ההאזנה

        Console.WriteLine("[SERVER] החיבור נסגר בהצלחה.");
    }

    // =========================================================================
    // 2. סוכן השטח (Agent Client)
    // =========================================================================
    public static void RunClient()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[CLIENT] מכין סוכן שטח להתחברות...");
        Console.ResetColor();

        // שלב 1: יצירת סוקט בצד הלקוח
        using Socket senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // שלב 2: הגדרת כתובת השרת שאליו רוצים להתחבר
        IPAddress ip = IPAddress.Parse(ServerIp);
        IPEndPoint remoteEndPoint = new IPEndPoint(ip, ServerPort);

        try
        {
            // שלב 3: התחברות לשרת (Connect) דרך TCP Handshake
            Console.WriteLine($"[CLIENT] מתחבר לשרת המפקדה ב-{ServerIp}:{ServerPort}...");
            senderSocket.Connect(remoteEndPoint);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[CLIENT] החיבור בוצע בהצלחה (Connected)!");
            Console.ResetColor();

            // שלב 4: שליחת הודעה מסווגת לשרת
            string secretMessage = "סוכן 007 - משימה הושלמה בהצלחה, ממתין להוראות הבאות.";
            byte[] messageBytes = Encoding.UTF8.GetBytes(secretMessage);
            senderSocket.Send(messageBytes); // שידור המידע בסוקט

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[CLIENT] שדר נשלח למפקדה: \"{secretMessage}\"");
            Console.ResetColor();

            // שלב 5: קבלת אישור בחזרה מהשרת (Receive)
            byte[] responseBuffer = new byte[1024];
            int bytesReceived = senderSocket.Receive(responseBuffer);
            string serverResponse = Encoding.UTF8.GetString(responseBuffer, 0, bytesReceived);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[CLIENT] תשובה התקבלה מהמפקדה: \"{serverResponse}\"");
            Console.ResetColor();

            // שלב 6: סגירת החיבור
            senderSocket.Shutdown(SocketShutdown.Both);
            senderSocket.Close();

            Console.WriteLine("[CLIENT] החיבור נסגר.");
        }
        catch (SocketException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CLIENT ERROR] לא ניתן להתחבר לשרת: {ex.Message}");
            Console.WriteLine("[TIP] וודא שהשרת (RunServer) פועל ומאזין לפני הפעלת הלקוח.");
            Console.ResetColor();
        }
    }

    // =========================================================================
    // 3. בדיקה חיה מהירה (הפעלת שרת ולקוח יחד בתהליכון מקבילי להדגמה למורה)
    // =========================================================================
    public static void RunLiveDemo()
    {
        Console.WriteLine("--- מתחיל הדגמה חיה של חיבור סוקט TCP ---");
        Console.WriteLine();

        // הפעלת השרת ברקע בתהליכון (Thread) נפרד כדי שלא יחסום את המסך
        Thread serverThread = new Thread(() =>
        {
            try
            {
                RunServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex.Message}");
            }
        });
        serverThread.Start(); // הפעלת תהליכון השרת

        // המתנה קלה של חצי שנייה כדי שהשרת יספיק לפתוח את הפורט ולהאזין
        Thread.Sleep(600);

        Console.WriteLine();
        // כעת מריצים את הלקוח שיתחבר לשרת
        RunClient();

        // ממתינים לסיום תהליכון השרת
        serverThread.Join();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("==================================================");
        Console.WriteLine("  ההדגמה הושלמה בהצלחה! התקשורת עברה דרך TCP Socket");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }
}
