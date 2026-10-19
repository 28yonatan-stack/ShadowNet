using System.Net; // ספרייה לניהול כתובות IP ופורטים ברשת
using System.Net.Sockets; // ספריית הסוקטים הרשמית של מיקרוסופט לעבודה מול מערכת ההפעלה (Windows Sockets)
using System.Text; // ספרייה להמרת מחרוזות טקסט למערכי בתים (Bytes) ובחזרה

namespace TacticalSocketDemo;

// =============================================================================
// ShadowNet Tactical TCP Socket Subsystem
// קובץ זה מדגים תקשורת סוקטים ישירה ברמת פרוטוקול TCP בין שרת מרכזי לסוכן שטח
// כל ההודעות מודפסות באנגלית טקטית, וכל שורת קוד מוסברת בעברית פשוטה
// =============================================================================
class Program
{
    // הגדרת כתובת ה-IP והפורט של ערוץ התקשורת
    private const string ServerIp = "127.0.0.1"; // כתובת מקומית (Localhost)
    private const int ServerPort = 5050; // פורט ייעודי לערוץ הסוקט

    static void Main(string[] args)
    {
        // הדפסת כותרת מסוגננת בצבע תכלת (Cyan)
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("    ShadowNet - Tactical TCP Socket Subsystem     ");
        Console.WriteLine("    Encrypted Channel Architecture (TCP/IP)       ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        // תפריט בחירה באנגלית
        Console.WriteLine();
        Console.WriteLine("Select operating mode:");
        Console.WriteLine("  [1] Run Command Server  (Listens for incoming agent connections)");
        Console.WriteLine("  [2] Run Agent Operative (Connects and transmits payload)");
        Console.WriteLine("  [3] Live Auto Demo      (Executes Server and Client simultaneously)");
        Console.Write("Enter choice [1, 2, or 3] and press Enter: ");

        // קריאת בחירת המשתמש מהמקלדת
        string choice = Console.ReadLine()?.Trim() ?? "3";
        Console.WriteLine();

        // בדיקה מה המשתמש בחר והרצה של הפונקציה המתאימה
        if (choice == "1")
        {
            // הרצת שרת הסוקטים בלבד
            RunServer();
        }
        else if (choice == "2")
        {
            // הרצת לקוח הסוקטים בלבד
            RunClient();
        }
        else
        {
            // הרצה משולבת בלייב להצגה מהירה למורה
            RunLiveDemo();
        }
    }

    // =========================================================================
    // 1. שרת הסוקטים של המפקדה (Command Server)
    // =========================================================================
    public static void RunServer()
    {
        // הודעת פתיחה של השרת בצבע צהוב
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[SERVER] Initializing Tactical Socket Listener...");
        Console.ResetColor();

        // שלב 1: יצירת אובייקט הסוקט של השרת
        // AddressFamily.InterNetwork = שימוש בכתובות IPv4
        // SocketType.Stream = זרם נתונים רציף ואמין (מאפיין של TCP)
        // ProtocolType.Tcp = בחירה מפורשת בפרוטוקול TCP
        using Socket listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // שלב 2: קשירת הסוקט לכתובת IP ולפורט (Bind)
        IPAddress ip = IPAddress.Parse(ServerIp); // המרת מחרוזת ה-IP לאובייקט מתאים
        IPEndPoint localEndPoint = new IPEndPoint(ip, ServerPort); // יצירת נקודת קצה (IP + Port)
        listenerSocket.Bind(localEndPoint); // קשירת הסוקט לנקודת הקצה במחשב

        // שלב 3: העברת הסוקט למצב האזנה לקבלת חיבורים נכנסים (Listen)
        // המספר 10 קובע את גודל התור של בקשות חיבור שיכולות להמתין
        listenerSocket.Listen(10);

        // הדפסה שהשרת מוכן ומאזין
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SERVER] Listening on {ServerIp}:{ServerPort}");
        Console.WriteLine("[SERVER] Awaiting operative connection...");
        Console.ResetColor();

        // שלב 4: קבלת חיבור של לקוח (Accept) - הפקודה ממתינה ועוצרת עד שסוכן מתחבר
        using Socket clientSocket = listenerSocket.Accept();

        // הדפסה שהסוכן התחבר בהצלחה
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SERVER] Operative connected from: {clientSocket.RemoteEndPoint}");
        Console.ResetColor();

        // שלב 5: קבלת נתונים מהסוכן דרך הסוקט (Receive)
        byte[] buffer = new byte[1024]; // זיכרון זמני לקליטת הבתים (עד 1024 בתים)
        int bytesReceived = clientSocket.Receive(buffer); // קריאת הנתונים מהסוקט לתוך המערך
        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived); // המרת הבתים חזרה לטקסט

        // הדפסת השדר שהתקבל מהסוכן
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[SERVER] Inbound transmission: \"{receivedMessage}\"");
        Console.ResetColor();

        // שלב 6: שליחת תשובה ואישור קבלה חזרה לסוכן (Send)
        string responseMessage = $"[ACK] DISPATCH LOGGED // CLASSIFICATION: TOP SECRET // COMM_CHANNEL: ACTIVE";
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage); // המרת התשובה למערך בתים
        clientSocket.Send(responseBytes); // שידור הבתים דרך הסוקט אל הסוכן

        // הדפסה שהאישור נשלח
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[SERVER] Acknowledgment packet transmitted to operative.");
        Console.ResetColor();

        // שלב 7: סגירת החיבור בצורה מסודרת ומאובטחת
        clientSocket.Shutdown(SocketShutdown.Both); // הפסקה מוחלטת של קליטה ושידור
        clientSocket.Close(); // סגירת שקע התקשורת של הלקוח
        listenerSocket.Close(); // סגירת שקע ההאזנה של השרת

        Console.WriteLine("[SERVER] Socket session terminated gracefully.");
    }

    // =========================================================================
    // 2. סוכן השטח (Agent Client)
    // =========================================================================
    public static void RunClient()
    {
        // הודעת פתיחה של הלקוח בצבע תכלת
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[CLIENT] Initializing Field Operative Socket...");
        Console.ResetColor();

        // שלב 1: יצירת אובייקט הסוקט בצד הלקוח
        using Socket senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // שלב 2: הגדרת כתובת היעד של שרת המפקדה
        IPAddress ip = IPAddress.Parse(ServerIp); // כתובת ה-IP של השרת
        IPEndPoint remoteEndPoint = new IPEndPoint(ip, ServerPort); // נקודת הקצה של השרת שאליו מתחברים

        try
        {
            // שלב 3: התחברות לשרת (Connect) באמצעות לחיצת יד של פרוטוקול TCP
            Console.WriteLine($"[CLIENT] Connecting to Command HQ at {ServerIp}:{ServerPort}...");
            senderSocket.Connect(remoteEndPoint); // ביצוע החיבור הפיזי ברשת

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[CLIENT] Tactical handshake established (Connected).");
            Console.ResetColor();

            // שלב 4: שליחת הודעה מסווגת מהסוכן אל השרת
            string payload = "OPERATIVE-007 // RECON COMPLETE // TARGET SECURED";
            byte[] messageBytes = Encoding.UTF8.GetBytes(payload); // המרת הטקסט למערך של בתים
            senderSocket.Send(messageBytes); // שידור הנתונים דרך הסוקט

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[CLIENT] Outbound payload: \"{payload}\"");
            Console.ResetColor();

            // שלב 5: קבלת אישור ותשובה בחזרה מהשרת (Receive)
            byte[] responseBuffer = new byte[1024]; // זיכרון זמני לקליטת תשובת השרת
            int bytesReceived = senderSocket.Receive(responseBuffer); // קריאת הנתונים מהסוקט
            string serverResponse = Encoding.UTF8.GetString(responseBuffer, 0, bytesReceived); // המרת הבתים לטקסט

            // הדפסת התשובה שהתקבלה מהשרת בצבע סגול
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[CLIENT] HQ Confirmation received: \"{serverResponse}\"");
            Console.ResetColor();

            // שלב 6: סגירת החיבור בצורה מסודרת
            senderSocket.Shutdown(SocketShutdown.Both); // הפסקת שידור וקליטה
            senderSocket.Close(); // סגירת הסוקט

            Console.WriteLine("[CLIENT] Tactical socket session closed.");
        }
        catch (SocketException ex)
        {
            // טיפול בשגיאת חיבור (למשל אם השרת עדיין לא דולק)
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CLIENT ERROR] Connection failed: {ex.Message}");
            Console.WriteLine("[TIP] Ensure the server is listening before launching the client.");
            Console.ResetColor();
        }
    }

    // =========================================================================
    // 3. הדגמה חיה אוטומטית (מריץ שרת ולקוח יחד להצגה קלה מול המורה)
    // =========================================================================
    public static void RunLiveDemo()
    {
        Console.WriteLine("--- Starting Automated Live Socket Handshake ---");
        Console.WriteLine();

        // הפעלת השרת ברקע בתהליכון (Thread) נפרד כדי שלא יתקע את התוכנית
        Thread serverThread = new Thread(() =>
        {
            try
            {
                RunServer(); // הרצת שרת הסוקט
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex.Message}");
            }
        });
        serverThread.Start(); // התחלת פעולת השרת

        // המתנה קצרה של 600 מילי-שניות כדי לוודא שהשרת סיים לפתוח את הפורט ולהאזין
        Thread.Sleep(600);

        Console.WriteLine();

        // הפעלת הלקוח שיתחבר לשרת וישלח שדר
        RunClient();

        // המתנה לסיום מלא של תהליכון השרת
        serverThread.Join();

        // הדפסת הודעת סיכום חגיגית וירוקה
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("==================================================");
        Console.WriteLine("  SUCCESS: TCP Socket Handshake Completed!        ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }
}
