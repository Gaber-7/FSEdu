// fseduLessonProgress — periodic video-position tracker.
//
// Wires up to a <video id="lesson-player"> element and:
//   • Reports current position + watched% to .NET every 15 s while playing
//   • Reports on pause and before page unload (best-effort)
//   • Optionally seeks to a stored resume position on first metadata load
//
// Usage from Blazor:
//   var dotnet = DotNetObjectReference.Create(this);
//   await JS.InvokeVoidAsync("fseduLessonProgress.attach", "lesson-player", dotnet, resumeAtSec);
//   ...
//   await JS.InvokeVoidAsync("fseduLessonProgress.detach");
//
// On the .NET side, expose a [JSInvokable] method named "OnProgressTick"
// that accepts (int positionSec, decimal watchedPct).
window.fseduLessonProgress = (function () {
    let video = null;
    let dotnet = null;
    let intervalId = null;
    let pageHideHandler = null;
    let pauseHandler = null;
    let lastReportedSec = -1;

    const TICK_MS = 15000;

    function pct() {
        if (!video || !video.duration || isNaN(video.duration) || video.duration <= 0) return 0;
        return Math.min(100, Math.max(0, (video.currentTime / video.duration) * 100));
    }

    async function report(force) {
        if (!video || !dotnet) return;
        const sec = Math.max(0, Math.floor(video.currentTime || 0));
        if (!force && sec === lastReportedSec) return;
        lastReportedSec = sec;
        try {
            await dotnet.invokeMethodAsync("OnProgressTick", sec, +pct().toFixed(2));
        } catch (e) {
            // Component may have been disposed — silently ignore.
        }
    }

    function attach(elementId, dotnetRef, resumeAtSec) {
        detach();

        video = document.getElementById(elementId);
        if (!video) return false;

        dotnet = dotnetRef;
        lastReportedSec = -1;

        // Resume from stored position once metadata is ready.
        if (resumeAtSec && resumeAtSec > 0) {
            const seek = function () {
                try { video.currentTime = resumeAtSec; } catch (e) { }
            };
            if (video.readyState >= 1) seek();
            else video.addEventListener('loadedmetadata', seek, { once: true });
        }

        // Restore the user's chosen playback rate (if any).
        applyStoredSpeed();

        // Periodic tick while playing — but only when the tab is visible
        // and the video is actually advancing.
        intervalId = setInterval(function () {
            if (!video) return;
            if (document.hidden) return;
            if (video.paused || video.ended) return;
            report(false);
        }, TICK_MS);

        pauseHandler = function () { report(true); };
        video.addEventListener('pause', pauseHandler);
        video.addEventListener('ended', pauseHandler);

        // Last-chance save when the user closes the tab / navigates away.
        // pagehide fires more reliably on mobile than beforeunload.
        pageHideHandler = function () { report(true); };
        window.addEventListener('pagehide', pageHideHandler);
        window.addEventListener('beforeunload', pageHideHandler);

        return true;
    }

    function detach() {
        if (intervalId) { clearInterval(intervalId); intervalId = null; }
        if (video && pauseHandler) {
            video.removeEventListener('pause', pauseHandler);
            video.removeEventListener('ended', pauseHandler);
        }
        if (pageHideHandler) {
            window.removeEventListener('pagehide', pageHideHandler);
            window.removeEventListener('beforeunload', pageHideHandler);
        }
        video = null;
        dotnet = null;
        pauseHandler = null;
        pageHideHandler = null;
    }

    // Playback-speed control — persists the user's last chosen rate in
    // localStorage so it's restored across lessons.
    const SPEED_KEY = 'fsedu.lesson.speed';

    function setSpeed(rate) {
        const r = parseFloat(rate);
        if (isNaN(r) || r < 0.25 || r > 4) return null;
        try { localStorage.setItem(SPEED_KEY, String(r)); } catch { }
        if (video) {
            try { video.playbackRate = r; } catch (e) { }
        }
        return r;
    }

    function getSpeed() {
        try {
            const stored = parseFloat(localStorage.getItem(SPEED_KEY));
            if (!isNaN(stored) && stored >= 0.25 && stored <= 4) return stored;
        } catch { }
        return 1.0;
    }

    function applyStoredSpeed() {
        if (!video) return;
        const r = getSpeed();
        const apply = function () { try { video.playbackRate = r; } catch { } };
        if (video.readyState >= 1) apply();
        else video.addEventListener('loadedmetadata', apply, { once: true });
    }

    return { attach, detach, setSpeed, getSpeed, applyStoredSpeed };
})();
