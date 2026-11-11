using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TacticalSocketDemo;

/// <summary>
/// תוכנית תקשורת סוקטים נקייה וללא הדפסות מיותרות (TCP Client-Server)
/// קוד פשוט, ישיר ומדויק לבגרות עם הערה בעברית על כל שורה
/// </summary>
class Program
{
    // כתובת ה-IP של השרת (Localhost - המחשב המקומי)
    private const string ServerIp = "127.0.0.1";

    // מספר הפורט שדרכו מתבצעת התקשורת
    private const int ServerPort = 5050;

    static void Main(string[] args)
    {
        // קריאת בחירת המשתמש מהמקלדת: 1 עבור שרת, 2 עבור לקוח
        string choice = Console.ReadLine()?.Trim() ?? "1";

        // בדיקה מה נבחר והרצת הפונקציה המתאימה
        if (choice == "1")
        {
            // הפעלת שרת
            RunServer();
        }
        else if (choice == "2")
        {
            // הפעלת לקוח
            RunClient();
        }
    }

    // =========================================================================
    // 1. שרת הסוקטים (Server)
    // =========================================================================
    public static void RunServer()
    {
        // יצירת אובייקט סוקט חדש עבור השרת (IPv4, TCP)
        Socket listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // המרת מחרוזת ה-IP לכתובת רשת
            IPAddress ip = IPAddress.Parse(ServerIp);

            // יצירת נקודת קצה המשלבת את ה-IP והפורט
            IPEndPoint endPoint = new IPEndPoint(ip, ServerPort);

            // קשירת הסוקט לנקודת הקצה (Bind)
            listenerSocket.Bind(endPoint);

            // העברת הסוקט למצב האזנה לקבלת חיבורים נכנסים (Listen)
            listenerSocket.Listen(10);

            // קבלת חיבור של לקוח (Accept) - הפקודה ממתינה עד שלקוח מתחבר
            Socket clientSocket = listenerSocket.Accept();

            // מערך בתים (Buffer) לקליטת המידע מהלקוח
            byte[] buffer = new byte[1024];

            // לולאה לקבלת הודעות ברצף עד לסיום השיחה
            while (true)
            {
                // קבלת נתונים מהלקוח ומספר הבתים שהתקבלו
                int bytesReceived = clientSocket.Receive(buffer);

                // אם התקבלו 0 בתים סימן שהלקוח ניתק את הקשר
                if (bytesReceived == 0)
                {
                    break;
                }

                // המרת הבתים שהתקבלו לטקסט
                string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                // הדפסת ההודעה שהתקבלה מהלקוח
                Console.WriteLine(receivedText);

                // בדיקה אם הלקוח שלח פקודת יציאה
                if (receivedText.Trim().ToLower() == "exit")
                {
                    break;
                }

                // הכנת תשובה לשליחה חזרה ללקוח
                string responseText = "OK: " + receivedText;

                // המרת התשובה למערך בתים
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);

                // שליחת התשובה חזרה ללקוח (Send)
                clientSocket.Send(responseBytes);
            }

            // סגירת החיבור מול הלקוח
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
        catch
        {
            // שגיאות נתפסות כדי למנוע קריסה
        }
        finally
        {
            // סגירת סוקט ההאזנה של השרת
            listenerSocket.Close();
        }
    }

    // =========================================================================
    // 2. לקוח הסוקטים (Client)
    // =========================================================================
    public static void RunClient()
    {
        // יצירת אובייקט סוקט עבור הלקוח (IPv4, TCP)
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // המרת כתובת ה-IP של השרת
            IPAddress ip = IPAddress.Parse(ServerIp);

            // יצירת נקודת קצה של השרת
            IPEndPoint serverEndPoint = new IPEndPoint(ip, ServerPort);

            // התחברות לשרת (Connect)
            clientSocket.Connect(serverEndPoint);

            // מערך בתים לקליטת התשובות מהשרת
            byte[] buffer = new byte[1024];

            // לולאה להקלדת ושליחת הודעות
            while (true)
            {
                // קריאת הודעה מהמקלדת שהמשתמש מקליד
                string message = Console.ReadLine() ?? "";

                // אם המחרוזת ריקה מדלגים וממתינים לקלט תקין
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                // המרת הטקסט למערך בתים
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                // שליחת ההודעה לשרת (Send)
                clientSocket.Send(messageBytes);

                // אם הוקלד exit יוצאים מהלולאה
                if (message.Trim().ToLower() == "exit")
                {
                    break;
                }

                // קבלת תשובה מהשרת (Receive)
                int bytesReceived = clientSocket.Receive(buffer);

                // אם התקבלה תשובה
                if (bytesReceived > 0)
                {
                    // המרת הבתים לטקסט
                    string serverReply = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                    // הדפסת התשובה שהתקבלה מהשרת
                    Console.WriteLine(serverReply);
                }
            }

            // סגירת הסוקט של הלקוח
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
        catch
        {
            // שגיאות נתפסות כדי למנוע קריסה
        }
    }
}
