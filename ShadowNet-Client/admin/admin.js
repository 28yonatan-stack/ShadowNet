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
 * 2. מעבר בין לשוניות בסרגל הצד (משתמשים / יומן אבטחה)
 */
function switchView(viewName) {
    const usersPanel = document.getElementById("view-users-panel"); // פאנל משתמשים
    const securityPanel = document.getElementById("view-security-panel"); // פאנל יומן אבטחה
    const navUsers = document.getElementById("nav-users"); // כפתור משתמשים
    const navSecurity = document.getElementById("nav-security"); // כפתור יומן אבטחה
    const viewTitle = document.getElementById("currentViewTitle"); // כותרת עליונה

    if (viewName === 'users') {
        if (usersPanel) usersPanel.style.display = 'block'; // הצגת פאנל משתמשים
        if (securityPanel) securityPanel.style.display = 'none'; // הסתרת יומן
        if (navUsers) navUsers.classList.add('active'); // סימון כפתור כפעיל
        if (navSecurity) navSecurity.classList.remove('active');
        if (viewTitle) viewTitle.innerText = 'ברוך הבא yonatan rafailov'; // כותרת משתמשים
    } else if (viewName === 'security') {
        if (usersPanel) usersPanel.style.display = 'none'; // הסתרת פאנל משתמשים
        if (securityPanel) securityPanel.style.display = 'block'; // הצגת יומן
        if (navUsers) navUsers.classList.remove('active');
        if (navSecurity) navSecurity.classList.add('active'); // סימון כפתור כפעיל
        if (viewTitle) viewTitle.innerText = 'יומן אבטחה ואימות'; // כותרת יומן
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
 * 9. התנתקות מסודרת מפאנל הניהול
 */
function handleLogout() {
    sessionStorage.removeItem("currentUser"); // מחיקת הסשן
    window.location.href = "../login.html"; // חזרה לדף ההתחברות
}

/**
 * 10. הפעלה בעת טעינת הדף
 */
document.addEventListener('DOMContentLoaded', () => {
    const isAllowed = checkAdminAccessAndLoadProfile(); // בדיקת הרשאה
    if (isAllowed) {
        setupSearch(); // הפעלת תיבת החיפוש
        fetchUsers(); // שליפת הנתונים
    }
});