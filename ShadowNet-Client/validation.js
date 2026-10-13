/**
 * ShadowNet - Client Validation & Form Utility
 * קובץ פונקציות משותף לאימות קלט (אימייל, סיסמה) ולטיפול בשדות
 */

// רשימת שגיאות נפוצות בסיומות דוא"ל (כמו .con במקום .com)
const BAD_TLDS = [
    'con', 'ocm', 'cmo', 'comm', 'cpm', 'vom', 'comn',
    'gmal', 'gmial', 'gmaill', 'gamil', 'gmai',
    'yaho', 'yahooo', 'hotmial', 'hotnail', 'outloook'
];

/**
 * בדיקת תקינות של כתובת דוא"ל
 * @param {string} email - כתובת האימייל לבדיקה
 * @returns {object} { ok: boolean, msg: string }
 */
function isValidEmail(email) {
    // בדיקה שהוזן ערך
    if (!email || typeof email !== 'string') {
        return { ok: false, msg: 'Please enter your email address' };
    }

    const trimmed = email.trim(); // הסרת רווחים מיותרים
    const re = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/; // רגקס למבנה אימייל תקין

    // בדיקת התאמה לפורמט הכללי
    if (!re.test(trimmed)) {
        return { ok: false, msg: 'Invalid email format (e.g. name@gmail.com)' };
    }

    // בדיקה שיש בדיוק שטרודל אחד (@)
    if ((trimmed.match(/@/g) || []).length !== 1) {
        return { ok: false, msg: 'Email must contain exactly one @' };
    }

    // פירוק הדומיין והסיומת
    const parts = trimmed.split('@'); // הפרדה בין שם המשתמש לדומיין
    const domain = parts[1].toLowerCase(); // אותיות קטנות בדומיין
    const domainParts = domain.split('.'); // פירוק חלקי הדומיין
    const tld = domainParts[domainParts.length - 1]; // הסיומת האחרונה (למשל com)

    // בדיקת אורך סיומת
    if (tld.length < 2) {
        return { ok: false, msg: 'Email domain ending is too short' };
    }
    if (tld.length > 10) {
        return { ok: false, msg: 'Email domain ending looks invalid' };
    }

    // זיהוי שגיאות הקלדה כמו .con
    if (BAD_TLDS.includes(tld)) {
        return { ok: false, msg: `Did you mean ".com"? "${tld}" looks like a typo` };
    }

    // זיהוי שגיאות בשם השירות (כמו gmal)
    if (BAD_TLDS.includes(domainParts[0])) {
        return { ok: false, msg: `"${domainParts[0]}" looks like a domain typo` };
    }

    return { ok: true };
}

/**
 * בדיקת חוקי סיסמה:
 * - לפחות 6 תווים
 * - לפחות אות אחת באנגלית
 * - לפחות מספר אחד או תו מיוחד
 * @param {string} password - הסיסמה לבדיקה
 */
function validatePasswordRules(password) {
    // בדיקה שהוזנה סיסמה
    if (!password) {
        return { ok: false, msg: 'Password is required' };
    }

    // בדיקת אורך מינימלי של 6 תווים
    if (password.length < 6) {
        return { ok: false, msg: 'Password must be at least 6 characters' };
    }

    const hasLetter = /[a-zA-Z]/.test(password); // בדיקה לקיום אות
    const hasDigit = /[0-9]/.test(password); // בדיקה לקיום ספרה
    const hasSymbol = /[^a-zA-Z0-9]/.test(password); // בדיקה לקיום תו מיוחד

    // חובה אות באנגלית
    if (!hasLetter) {
        return { ok: false, msg: 'Password must include at least one letter (a-z)' };
    }

    // חובה מספר או תו מיוחד
    if (!hasDigit && !hasSymbol) {
        return { ok: false, msg: 'Password must include at least one number or special character (!@#$...)' };
    }

    // חישוב ניקוד חוזק הסיסמה (סרגל חוזק)
    let score = 1; // ניקוד בסיס
    if (hasLetter) score++; // תוספת על אות
    if (hasDigit) score++; // תוספת על מספר
    if (hasSymbol) score++; // תוספת על תו מיוחד
    if (password.length >= 10) score++; // תוספת על אורך מעל 10

    // הגדרת תוויות וצבעים לפי ציון
    const levels = [
        null,
        { label: 'Weak', color: '#e53e3e' }, // חלשה - אדום
        { label: 'Fair', color: '#dd6b20' }, // בינונית - כתום
        { label: 'Good', color: '#3182ce' }, // טובה - כחול
        { label: 'Strong', color: '#38a169' }, // חזקה - ירוק
        { label: 'Very Strong', color: '#276749' } // חזקה מאוד - ירוק כהה
    ];

    const strength = levels[Math.min(score, 5)]; // בחירת הרמה המתאימה

    return {
        ok: true,
        score: score,
        strength: strength
    };
}

/**
 * סימון שדה כשגוי עם מסגרת אדומה והודעת שגיאה
 */
function setFieldError(el, errEl, msg) {
    if (el) {
        el.classList.add('input-invalid'); // מסגרת אדומה
        el.classList.remove('input-valid'); // ביטול מסגרת ירוקה
    }
    if (errEl) {
        errEl.innerText = msg; // כתיבת הודעת השגיאה
    }
}

/**
 * סימון שדה כתקין עם מסגרת ירוקה ומחיקת שגיאה
 */
function setFieldOk(el, errEl) {
    if (el) {
        el.classList.remove('input-invalid'); // הסרת מסגרת אדומה
        el.classList.add('input-valid'); // הוספת מסגרת ירוקה
    }
    if (errEl) {
        errEl.innerText = ''; // מחיקת השגיאה
    }
}

/**
 * איפוס מצב השדה למצב ניטרלי
 */
function clearField(el, errEl) {
    if (el) {
        el.classList.remove('input-invalid', 'input-valid'); // הסרת צבעים
    }
    if (errEl) {
        errEl.innerText = ''; // מחיקת הודעה
    }
}

/**
 * הצגה והסתרה של סיסמה בלחיצה על כפתור העין
 */
function toggleVisibility(inputId, eyeId, event) {
    if (event) {
        event.preventDefault(); // מניעת התנהגות ברירת מחדל
        event.stopPropagation(); // עצירת פעפוע האירוע
    }
    const inputElement = document.getElementById(inputId); // שדה הסיסמה
    const eyeElement = document.getElementById(eyeId); // כפתור העין
    if (!inputElement) return;

    if (inputElement.type === "password") {
        inputElement.type = "text"; // הצגת הסיסמה
        if (eyeElement) {
            eyeElement.innerHTML = '<svg class="eye-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>';
            eyeElement.title = "Hide Password";
        }
    } else {
        inputElement.type = "password"; // הסתרת הסיסמה
        if (eyeElement) {
            eyeElement.innerHTML = '<svg class="eye-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>';
            eyeElement.title = "Show Password";
        }
    }
}
