/**
 * ShadowNet - פאנל ניהול אדמין (Admin Console)
 * קובץ לניהול תצוגת משתמשים, בדיקת הרשאות מנהל, ומחיקת משתמשים
 */

// כתובת שרת ה-API של ShadowNet (פורט 5000)
const API_BASE = (window.location.port === '5000') ? window.location.origin : 'http://localhost:5000';

let allUsers = []; // מערך לשמירת רשימת המשתמשים שהתקבלה מהשרת

/**
 * 1. בדיקת הרשאת מנהל בעת טעינת העמוד
 * חוסם כל מי שאינו מחובר או שאינו 28yonatan@gmail.com
 */
function checkAdminAccessAndLoadProfile() {
    const rawUser = sessionStorage.getItem("currentUser"); // קריאת המשתמש המחובר מהסשן
    if (!rawUser) {
        // אין משתמש מחובר בכלל - הפניה מיידית לדף ההתחברות
        window.location.href = "../login.html";
        return false;
    }

    try {
        const user = JSON.parse(rawUser); // פענוח אובייקט המשתמש
        const userEmail = (user.email || '').toLowerCase().trim(); // קריאת כתובת האימייל באותיות קטנות

        // הרשאה קפדנית: רק 28yonatan@gmail.com מורשה לצפות באדמין!
        if (userEmail !== '28yonatan@gmail.com') {
            alert('Access Denied! The admin panel is restricted to 28yonatan@gmail.com only.');
            window.location.href = "../dashboard.html"; // משתמש רגיל מועבר לאזור האישי
            return false;
        }

        // עדכון פרטי המנהל בסרגל הצד
        const nameEl = document.getElementById("adminFullName");
        const emailEl = document.getElementById("adminEmailDisplay");
        const avatarEl = document.getElementById("adminAvatar");

        const displayName = user.fullName || user.username || "yonatan rafailov";
        if (nameEl) nameEl.innerText = displayName; // שם המנהל
        if (emailEl) emailEl.innerText = user.email || "28yonatan@gmail.com"; // אימייל
        if (avatarEl) avatarEl.innerText = displayName.charAt(0).toUpperCase(); // אות ראשונה לאוואטר
        return true;
    } catch (e) {
        console.error("Error reading session user:", e);
        window.location.href = "../login.html";
        return false;
    }
}

/**
 * 2. מעבר בין לשוניות בסרגל הצד (משתמשים / סטטוס שרת / יומן אבטחה)
 */
function switchView(viewName) {
    const usersPanel = document.getElementById("view-users-panel"); // פאנל משתמשים
    const securityPanel = document.getElementById("view-security-panel"); // פאנל יומן אבטחה
    const statusPanel = document.getElementById("view-status-panel"); // פאנל סטטוס שרת ותשתיות תקשורת
    const navUsers = document.getElementById("nav-users"); // כפתור משתמשים
    const navSecurity = document.getElementById("nav-security"); // כפתור יומן אבטחה
    const navStatus = document.getElementById("nav-status"); // כפתור סטטוס שרת
    const viewTitle = document.getElementById("currentViewTitle"); // כותרת עליונה

    // איפוס תצוגת כל הפאנלים
    if (usersPanel) usersPanel.style.display = 'none';
    if (securityPanel) securityPanel.style.display = 'none';
    if (statusPanel) statusPanel.style.display = 'none';

    // איפוס הדגשת הכפתורים
    if (navUsers) navUsers.classList.remove('active');
    if (navSecurity) navSecurity.classList.remove('active');
    if (navStatus) navStatus.classList.remove('active');

    if (viewName === 'users') {
        if (usersPanel) usersPanel.style.display = 'block'; // הצגת פאנל משתמשים
        if (navUsers) navUsers.classList.add('active'); // סימון כפתור כפעיל
        if (viewTitle) viewTitle.innerText = 'ברוך הבא yonatan rafailov'; // כותרת משתמשים
    } else if (viewName === 'status') {
        if (statusPanel) statusPanel.style.display = 'block'; // הצגת פאנל סטטוס שרת
        if (navStatus) navStatus.classList.add('active'); // סימון כפתור כפעיל
        if (viewTitle) viewTitle.innerText = 'סטטוס שרת ותשתיות תקשורת (Socket)'; // כותרת סטטוס
        checkSystemStatus(); // בדיקת מצב שרת עדכנית
    } else if (viewName === 'security') {
        if (securityPanel) securityPanel.style.display = 'block'; // הצגת יומן
        if (navSecurity) navSecurity.classList.add('active'); // סימון כפתור כפעיל
        if (viewTitle) viewTitle.innerText = 'יומן אבטחה, אימות והגנות (Security Audit)'; // כותרת יומן
        initSecurityPanel(); // אתחול פאנל האבטחה, הצפנה ויומן האירועים
    }
}

/**
 * 3. עדכון מספרי הסטטיסטיקה בכרטיסים העליונים (KPIs)
 */
function updateDashboardStats(users) {
    const totalUsers = users.length; // כמות משתמשים כוללת
    const onlineUsers = users.filter(u => u.is_online).length; // כמות מחוברים כעת

    const totalEl = document.getElementById('totalUsersCount');
    const onlineEl = document.getElementById('onlineUsersCount');
    const navPill = document.getElementById('navUsersCount');

    if (totalEl) totalEl.innerText = totalUsers; // עדכון סה"כ
    if (onlineEl) onlineEl.innerText = onlineUsers; // עדכון מחוברים
    if (navPill) navPill.innerText = totalUsers; // עדכון תגית מספר בסרגל
}

/**
 * 4. המרת מחרוזת תאריך לפורמט ישראלי נקי (DD/MM/YYYY HH:MM)
 */
function formatDate(dateString) {
    if (!dateString) return '—';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString; // אם התאריך כבר מעוצב
    
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');

    return `${day}/${month}/${year} ${hours}:${minutes}`;
}

/**
 * 5. בנייה והצגה של שורות הטבלה לפי רשימת המשתמשים
 */
function renderUsers(users) {
    const tableBody = document.getElementById('usersTableBody'); // גוף הטבלה
    const shownCountEl = document.getElementById('shownUsersCount'); // מונה שורות מוצגות
    if (!tableBody) return;

    tableBody.innerHTML = ''; // ניקוי טבלה קודמת
    if (shownCountEl) shownCountEl.innerText = users.length; // עדכון מונה

    // אם אין משתמשים להצגה
    if (!users || users.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-secondary); padding: 2.5rem;">לא נמצאו משתמשים במסד הנתונים ShadowNetDb</td></tr>`;
        return;
    }

    // מעבר על כל משתמש ויצירת שורת HTML
    users.forEach(user => {
        const displayUsername = user.username || (user.email ? user.email.split('@')[0] : 'user');
        const displayFullName = user.full_name || displayUsername;
        const firstLetter = displayUsername.charAt(0).toUpperCase();
        
        // תג סטטוס חיבור (Online / Offline)
        const statusBadge = user.is_online ? 
            `<span class="status-badge online"><span class="status-dot"></span>Online</span>` : 
            `<span class="status-badge offline"><span class="status-dot"></span>Offline</span>`;

        // תג אימות אימייל (Verified / Pending)
        const isVerified = user.is_verified !== false;
        const verificationBadge = isVerified ?
            `<span class="verified-badge"><i class="fa-solid fa-circle-check"></i> Verified</span>` :
            `<span class="unverified-badge"><i class="fa-solid fa-clock"></i> Pending</span>`;

        const row = document.createElement('tr'); // יצירת שורה
        row.innerHTML = ` 
            <td style="font-weight: 700; color: var(--text-secondary);">${user.id}</td>
            <td> 
                <div class="user-cell">
                    <div class="avatar-circle">${firstLetter}</div>
                    <div class="user-info-text">
                        <span class="user-full-name">${displayFullName}</span>
                        <span class="user-username">@${displayUsername}</span>
                    </div>
                </div>
            </td> 
            <td style="font-weight: 600; color: var(--text-primary);">${user.email}</td>
            <td>${verificationBadge}</td>
            <td>${statusBadge}</td>
            <td><code class="hash-code" title="${user.password_hash || user.passwordHash}">${user.password_hash || user.passwordHash}</code></td>
            <td style="color: var(--text-secondary); font-size: 0.85rem;" title="התחברות אחרונה: ${formatDate(user.last_login || user.lastLogin)}">${formatDate(user.created_at || user.createdAt)}</td>
            <td style="text-align: center;"> 
                <button class="btn-delete" onclick="deleteUser(${user.id})" title="מחק משתמש ממסד הנתונים">
                    <i class="fa-solid fa-trash-can"></i>
                </button> 
            </td> 
        `;
        tableBody.appendChild(row); // הוספת השורה לטבלה
    });

    updateDashboardStats(allUsers); // עדכון הסטטיסטיקות
}

/**
 * 6. שליפת המשתמשים מ-PostgreSQL דרך שרת ה-API של ShadowNet
 */
async function fetchUsers() {
    const tableBody = document.getElementById('usersTableBody');
    const refreshIco = document.getElementById('refreshIcon');
    if (refreshIco) refreshIco.classList.add('fa-spin'); // סיבוב אייקון הרענון

    if (tableBody && allUsers.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-secondary); padding: 2.5rem;"><i class="fa-solid fa-spinner fa-spin"></i> טוען נתונים ממסד הנתונים ShadowNet...</td></tr>`;
    }

    try {
        const response = await fetch(`${API_BASE}/api/users`); // קריאה לשרת
        if (!response.ok) {
            throw new Error(`שגיאת שרת: ${response.status}`);
        }
        const data = await response.json(); // פענוח רשימת המשתמשים
        allUsers = Array.isArray(data) ? data : (data.value || []); // שמירה במערך הגלובלי
        renderUsers(allUsers); // הצגה בטבלה
    } catch (err) {
        console.error("Error fetching users:", err);
        if (tableBody) {
            tableBody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: #ef4444; padding: 2.5rem;"><i class="fa-solid fa-triangle-exclamation"></i> שגיאה בטעינת נתונים: ${err.message}. וודא שהשרת פעיל ב-localhost:5000.</td></tr>`;
        }
    } finally {
        if (refreshIco) refreshIco.classList.remove('fa-spin'); // עצירת סיבוב האייקון
    }
}

/**
 * 7. חיפוש וסינון משתמשים בזמן אמת בתיבת החיפוש
 */
function setupSearch() {
    const searchInput = document.getElementById('userSearch');
    if (!searchInput) return;

    searchInput.addEventListener('input', function(e) {
        const term = e.target.value.toLowerCase().trim(); // מונח החיפוש
        if (!term) {
            renderUsers(allUsers); // הצגת כולם אם התיבה ריקה
            return;
        }
        // סינון לפי שם, אימייל, שם משתמש או מזהה
        const filtered = allUsers.filter(user => {
            const username = (user.username || '').toLowerCase();
            const email = (user.email || '').toLowerCase();
            const fullName = (user.full_name || '').toLowerCase();
            const id = (user.id || '').toString();
            return username.includes(term) || email.includes(term) || fullName.includes(term) || id.includes(term);
        });
        renderUsers(filtered); // הצגת התוצאות המסוננות
    });
}

/**
 * 8. מחיקת משתמש ממסד הנתונים
 */
async function deleteUser(userId) {
    if (!confirm(`האם אתה בטוח שברצונך למחוק את משתמש #${userId} ממסד הנתונים של ShadowNet?`)) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/api/users/${userId}`, {
            method: 'DELETE' // בקשת מחיקה
        });
        const data = await response.json();
        if (response.ok && data.success) {
            fetchUsers(); // רענון הטבלה לאחר מחיקה
        } else {
            alert('שגיאה במחיקת המשתמש: ' + (data.message || 'שגיאה כללית'));
        }
    } catch (err) {
        alert('שגיאת תקשורת עם שרת ShadowNet: ' + err.message);
    }
}

/**
 * 9. בדיקת סטטוס שרת (Socket) ומסד נתונים בזמן אמת
 */
async function checkSystemStatus() {
    const socketStatusDisplay = document.getElementById("socketStatusDisplay");
    const dbStatusDisplay = document.getElementById("dbStatusDisplay");

    try {
        const response = await fetch(`${API_BASE}/api/health`, { method: 'GET' });
        if (response.ok) {
            // השרת ומסד הנתונים מחוברים ותקינים
            if (socketStatusDisplay) {
                socketStatusDisplay.innerHTML = '<span class="status-dot-circle green"></span> מחובר ותקין';
            }
            if (dbStatusDisplay) {
                dbStatusDisplay.innerHTML = '<span class="status-dot-circle green"></span> מחובר ותקין';
            }
        } else {
            throw new Error("Server response not ok");
        }
    } catch (err) {
        // במקרה של חוסר תקשורת או שרת לא מופעל
        if (socketStatusDisplay) {
            socketStatusDisplay.innerHTML = '<span class="status-dot-circle red"></span> השרת מנותק';
        }
        if (dbStatusDisplay) {
            dbStatusDisplay.innerHTML = '<span class="status-dot-circle red"></span> מנותק';
        }
    }
}

// =============================================================================
// מודול אבטחה, הצפנה ויומן ביקורת בזמן אמת (Security & Audit Module)
// =============================================================================

/**
 * מפתח שמירת יומן אירועי האבטחה ב-LocalStorage
 */
const AUDIT_STORAGE_KEY = "shadownet_audit_logs_v1";

/**
 * רשימת אירועי ברירת מחדל מציאותיים ליומן האבטחה
 */
function getDefaultAuditLogs() {
    const now = new Date();
    const formatTimeOffset = (minutesAgo) => {
        const d = new Date(now.getTime() - minutesAgo * 60000);
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        const hours = String(d.getHours()).padStart(2, '0');
        const mins = String(d.getMinutes()).padStart(2, '0');
        const secs = String(d.getSeconds()).padStart(2, '0');
        return `${day}/${month}/${year} ${hours}:${mins}:${secs}`;
    };

    return [
        {
            time: formatTimeOffset(1),
            type: "אימות מנהל מערכת",
            color: "blue",
            target: "28yonatan@gmail.com",
            channel: "Web REST :5000",
            details: "כניסת אדמין מאומתת - הרשאות מלאות נפתחו",
            status: "VERIFIED"
        },
        {
            time: formatTimeOffset(4),
            type: "חיבור Socket טקטי",
            color: "cyan",
            target: "TacticalSocketDemo",
            channel: "TCP Socket :5057",
            details: "לחיצת יד TCP (Handshake) הושלמה בהצלחה",
            status: "SUCCESS"
        },
        {
            time: formatTimeOffset(8),
            type: "אתחול מסד נתונים",
            color: "green",
            target: "PostgreSQL Database",
            channel: "TCP Port :5432",
            details: "אימות טבלאות users ו-verification_codes",
            status: "CONNECTED"
        },
        {
            time: formatTimeOffset(15),
            type: "שירות שליחת אימיילים",
            color: "purple",
            target: "shadownet.security@gmail.com",
            channel: "SMTP TLS :587",
            details: "שירות מוכן לשליחת קודי OTP מאובטחים",
            status: "READY"
        }
    ];
}

/**
 * חישוב SHA-256 Digest באמצעות Web Crypto API המובנה בדפדפן
 */
async function calculateSha256(str) {
    if (!str) return '';
    const buffer = new TextEncoder().encode(str);
    const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

/**
 * הפעלת בדיקת הצפנה בזמן אמת ועדכון התצוגה
 */
async function runLiveCryptoHash() {
    const input = document.getElementById("cryptoInput");
    const display = document.getElementById("cryptoHashDisplay");
    if (!display) return;

    const textToHash = input ? input.value : "";
    if (!textToHash) {
        display.innerText = "הקלד טקסט כדי לראות את ה-Hash...";
        return;
    }

    try {
        const hash = await calculateSha256(textToHash);
        display.innerText = hash;
    } catch (e) {
        display.innerText = "שגיאה בחישוב ההצפנה: " + e.message;
    }
}

/**
 * העתקת ה-Hash ללוח עם פידבק ויזואלי
 */
async function copyCryptoHash() {
    const display = document.getElementById("cryptoHashDisplay");
    const icon = document.getElementById("copyHashIcon");
    const text = document.getElementById("copyHashText");
    if (!display) return;

    const hashValue = display.innerText.trim();
    if (!hashValue || hashValue.startsWith("...") || hashValue.startsWith("הקלד")) return;

    try {
        await navigator.clipboard.writeText(hashValue);
        if (icon) icon.className = "fa-solid fa-check";
        if (text) text.innerText = "הועתק!";
        setTimeout(() => {
            if (icon) icon.className = "fa-regular fa-copy";
            if (text) text.innerText = "העתק Hash";
        }, 2000);
    } catch (e) {
        alert("ה-Hash הועתק: " + hashValue);
    }
}

/**
 * שליפת רשימת אירועי האבטחה מה-LocalStorage
 */
function getAuditLogs() {
    try {
        const saved = localStorage.getItem(AUDIT_STORAGE_KEY);
        if (saved) {
            const parsed = JSON.parse(saved);
            if (Array.isArray(parsed) && parsed.length > 0) return parsed;
        }
    } catch (e) {
        console.error("Error reading audit logs:", e);
    }
    return getDefaultAuditLogs();
}

/**
 * שמירת רשימת אירועים ל-LocalStorage
 */
function saveAuditLogs(logs) {
    try {
        localStorage.setItem(AUDIT_STORAGE_KEY, JSON.stringify(logs));
    } catch (e) {
        console.error("Error saving audit logs:", e);
    }
}

/**
 * הוספת אירוע אבטחה חדש בראש הרשימה
 */
function addAuditLogEntry(entry) {
    const logs = getAuditLogs();
    logs.unshift(entry); // הוספה לראש הרשימה
    if (logs.length > 50) logs.pop(); // הגבלה ל-50 אירועים
    saveAuditLogs(logs);
    renderAuditLogs();
}

/**
 * רינדור טבלת אירועי האבטחה
 */
function renderAuditLogs() {
    const tbody = document.getElementById("auditLogsTableBody");
    if (!tbody) return;

    const logs = getAuditLogs();
    tbody.innerHTML = '';

    if (logs.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--text-secondary); padding: 2rem;">אין אירועי אבטחה ביומן</td></tr>`;
        return;
    }

    logs.forEach(log => {
        const tr = document.createElement("tr");
        const colorClass = log.color || "blue";

        tr.innerHTML = `
            <td style="font-size: 0.82rem; color: var(--text-secondary); font-family: var(--font-latin), monospace;">${log.time}</td>
            <td><span class="audit-event-tag ${colorClass}">${log.type}</span></td>
            <td style="font-weight: 600; color: var(--text-primary); font-size: 0.88rem;" dir="ltr">${log.target}</td>
            <td style="font-size: 0.85rem; font-family: monospace; color: var(--brand-blue); font-weight: 700;" dir="ltr">${log.channel}</td>
            <td style="font-size: 0.85rem; color: var(--text-secondary);">${log.details}</td>
            <td style="text-align: center;">
                <span class="sec-badge ok" style="padding: 0.2rem 0.55rem; font-size: 0.72rem;">
                    <span class="status-dot-circle green"></span> ${log.status}
                </span>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

/**
 * יצירת אירוע אבטחה מתוך כלי בדיקת ה-SHA-256
 */
async function generateAndLogAuditTest() {
    const input = document.getElementById("cryptoInput");
    const val = input ? input.value.trim() : "";
    if (!val) {
        alert("נא להקליד טקסט או סיסמה בתיבת הבדיקה");
        return;
    }

    const hash = await calculateSha256(val);
    const shortHash = hash.substring(0, 16) + '...';

    const now = new Date();
    const day = String(now.getDate()).padStart(2, '0');
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const year = now.getFullYear();
    const hours = String(now.getHours()).padStart(2, '0');
    const mins = String(now.getMinutes()).padStart(2, '0');
    const secs = String(now.getSeconds()).padStart(2, '0');
    const timeStr = `${day}/${month}/${year} ${hours}:${mins}:${secs}`;

    addAuditLogEntry({
        time: timeStr,
        type: "הצפנת SHA-256",
        color: "purple",
        target: `Hash (${shortHash})`,
        channel: "Web Crypto API",
        details: `אימות הצפנה חד-כיוונית עבור מחרוזת: "${val.length > 10 ? val.substring(0, 10) + '...' : val}"`,
        status: "VERIFIED"
    });

    alert("אירוע ההצפנה נרשם בהצלחה ביומן האבטחה!");
}

/**
 * שליחת בדיקת פינג לשרת והוספת אירוע אבטחה ליומן בזמן אמת
 */
async function runServerSecurityPing() {
    const start = performance.now();
    try {
        const res = await fetch(`${API_BASE}/api/health`);
        const duration = Math.round(performance.now() - start);

        const now = new Date();
        const day = String(now.getDate()).padStart(2, '0');
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const year = now.getFullYear();
        const hours = String(now.getHours()).padStart(2, '0');
        const mins = String(now.getMinutes()).padStart(2, '0');
        const secs = String(now.getSeconds()).padStart(2, '0');
        const timeStr = `${day}/${month}/${year} ${hours}:${mins}:${secs}`;

        if (res.ok) {
            addAuditLogEntry({
                time: timeStr,
                type: "בדיקת פינג לשרת",
                color: "green",
                target: "ShadowNet Backend API",
                channel: "HTTP REST :5000",
                details: `שרת פעיל ומגיב במהירות (${duration}ms latency)`,
                status: "200 OK"
            });
        } else {
            addAuditLogEntry({
                time: timeStr,
                type: "בדיקת פינג לשרת",
                color: "orange",
                target: "ShadowNet Backend API",
                channel: "HTTP REST :5000",
                details: `שרת החזיר סטטוס שגיאה ${res.status}`,
                status: "WARNING"
            });
        }
    } catch (e) {
        const now = new Date();
        const timeStr = now.toLocaleTimeString('he-IL');
        addAuditLogEntry({
            time: timeStr,
            type: "בדיקת פינג לשרת",
            color: "orange",
            target: "ShadowNet Server",
            channel: "Connection Error",
            details: `השרת אינו זמין כרגע (${e.message})`,
            status: "OFFLINE"
        });
    }
}

/**
 * רענון יומן האבטחה
 */
function refreshAuditLogs() {
    renderAuditLogs();
}

/**
 * איפוס יומן האבטחה לרשומות ברירת מחדל
 */
function clearAuditLogs() {
    if (confirm("האם לאפס את יומן אירועי האבטחה לרשומות המערכת המקוריות?")) {
        localStorage.removeItem(AUDIT_STORAGE_KEY);
        renderAuditLogs();
    }
}

/**
 * אתחול ראשוני של פאנל האבטחה
 */
function initSecurityPanel() {
    runLiveCryptoHash();
    renderAuditLogs();
}

/**
 * 10. התנתקות מסודרת מפאנל הניהול
 */
function handleLogout() {
    sessionStorage.removeItem("currentUser"); // מחיקת הסשן
    window.location.href = "../login.html"; // חזרה לדף ההתחברות
}

/**
 * 11. הפעלה בעת טעינת הדף
 */
document.addEventListener('DOMContentLoaded', () => {
    const isAllowed = checkAdminAccessAndLoadProfile(); // בדיקת הרשאה
    if (isAllowed) {
        setupSearch(); // הפעלת תיבת החיפוש
        fetchUsers(); // שליפת הנתונים
        checkSystemStatus(); // בדיקת סטטוס תשתיות תקשורת ומסד נתונים
        initSecurityPanel(); // הכנת רכיב ההצפנה ויומן האבטחה ברקע
    }
});