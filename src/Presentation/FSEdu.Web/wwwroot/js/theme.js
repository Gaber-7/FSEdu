// fseduTheme — light/dark theme toggle with localStorage persistence.
// Loads BEFORE Blazor renders so there's no flash of wrong theme.
(function () {
    const KEY = 'fsedu.theme';

    function apply(theme) {
        if (theme === 'dark') {
            document.documentElement.setAttribute('data-theme', 'dark');
        } else {
            document.documentElement.removeAttribute('data-theme');
        }
    }

    // Apply stored theme immediately (sync) to avoid flash.
    try {
        const stored = localStorage.getItem(KEY);
        if (stored === 'dark' || stored === 'light') {
            apply(stored);
        } else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            apply('dark');
        }
    } catch (e) { /* private mode etc. */ }

    window.fseduTheme = {
        get: function () {
            try { return localStorage.getItem(KEY) || 'auto'; } catch { return 'auto'; }
        },
        set: function (theme) {
            try { localStorage.setItem(KEY, theme); } catch { }
            apply(theme === 'auto'
                ? (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
                : theme);
        },
        toggle: function () {
            const current = document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
            const next = current === 'dark' ? 'light' : 'dark';
            this.set(next);
            return next;
        },
        isDark: function () {
            return document.documentElement.getAttribute('data-theme') === 'dark';
        }
    };
})();
