// fseduSearchHistory — keeps the last N search queries in localStorage
// so the universal search page can show recent searches as quick links.
(function () {
    const KEY = 'fsedu.search.history';
    const MAX = 8;

    function read() {
        try {
            const raw = localStorage.getItem(KEY);
            if (!raw) return [];
            const arr = JSON.parse(raw);
            return Array.isArray(arr) ? arr : [];
        } catch { return []; }
    }

    function write(arr) {
        try { localStorage.setItem(KEY, JSON.stringify(arr.slice(0, MAX))); } catch { }
    }

    window.fseduSearchHistory = {
        get: function () { return read(); },
        add: function (query) {
            if (!query || typeof query !== 'string') return;
            const q = query.trim();
            if (q.length < 2 || q.length > 100) return;
            const arr = read().filter(x => x !== q);
            arr.unshift(q);
            write(arr);
        },
        remove: function (query) {
            write(read().filter(x => x !== query));
        },
        clear: function () {
            try { localStorage.removeItem(KEY); } catch { }
        }
    };
})();
