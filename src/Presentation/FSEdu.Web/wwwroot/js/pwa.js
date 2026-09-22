// fseduPwa — captures the beforeinstallprompt event so we can trigger
// installation from a button in our UI. Without this, Chrome's native
// "Install app" prompt only appears at its own discretion.
(function () {
    let deferredPrompt = null;

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        try { window.dispatchEvent(new CustomEvent('fsedu-install-available')); } catch (e) { }
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        try { window.dispatchEvent(new CustomEvent('fsedu-install-completed')); } catch (e) { }
    });

    window.fseduPwa = {
        canInstall: function () { return deferredPrompt !== null; },
        isStandalone: function () {
            return (window.matchMedia && window.matchMedia('(display-mode: standalone)').matches)
                || window.navigator.standalone === true;
        },
        prompt: async function () {
            if (!deferredPrompt) return 'unavailable';
            try {
                deferredPrompt.prompt();
                const choice = await deferredPrompt.userChoice;
                deferredPrompt = null;
                return choice.outcome; // 'accepted' or 'dismissed'
            } catch (e) {
                return 'error';
            }
        }
    };
})();
