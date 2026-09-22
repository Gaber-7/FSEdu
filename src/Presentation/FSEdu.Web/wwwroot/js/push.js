// FSEdu — Web Push subscription helper.
// Called from Blazor C# via JS interop.

(function () {
    if (window.fseduPush) return;

    function urlBase64ToUint8Array(base64) {
        const padding = "=".repeat((4 - base64.length % 4) % 4);
        const b64 = (base64 + padding).replace(/-/g, "+").replace(/_/g, "/");
        const raw = atob(b64);
        const arr = new Uint8Array(raw.length);
        for (let i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
        return arr;
    }

    async function getStatus() {
        if (!("serviceWorker" in navigator) || !("PushManager" in window))
            return { supported: false };
        const reg = await navigator.serviceWorker.getRegistration("/sw.js");
        const sub = reg ? await reg.pushManager.getSubscription() : null;
        return {
            supported: true,
            permission: typeof Notification !== "undefined" ? Notification.permission : "default",
            subscribed: !!sub
        };
    }

    async function ensureRegistration() {
        const existing = await navigator.serviceWorker.getRegistration("/sw.js");
        if (existing) return existing;
        return await navigator.serviceWorker.register("/sw.js");
    }

    async function enable(opts) {
        if (!("serviceWorker" in navigator) || !("PushManager" in window))
            throw new Error("هذا المتصفح لا يدعم الإشعارات.");

        const reg = await ensureRegistration();

        const perm = await Notification.requestPermission();
        if (perm !== "granted") throw new Error("يجب السماح بالإشعارات من المتصفح.");

        let sub = await reg.pushManager.getSubscription();
        if (!sub) {
            sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(opts.vapidKey)
            });
        }

        // Send to backend
        const payload = sub.toJSON();
        const apiBase = (opts.apiBase || "/").replace(/\/+$/, "");
        const resp = await fetch(apiBase + "/api/v1/push/subscribe", {
            method: "POST",
            headers: { "Content-Type": "application/json", "Authorization": "Bearer " + opts.appAuthToken },
            body: JSON.stringify({ endpoint: payload.endpoint, keys: payload.keys })
        });
        if (!resp.ok) throw new Error("فشل تسجيل الاشتراك على السيرفر: " + resp.status);
        return { ok: true };
    }

    async function disable(opts) {
        const reg = await navigator.serviceWorker.getRegistration("/sw.js");
        if (!reg) return { ok: true };
        const sub = await reg.pushManager.getSubscription();
        if (!sub) return { ok: true };

        const endpoint = sub.endpoint;
        try { await sub.unsubscribe(); } catch { /* ignore */ }

        if (opts && opts.appAuthToken) {
            const apiBase = (opts.apiBase || "/").replace(/\/+$/, "");
            try {
                await fetch(apiBase + "/api/v1/push/unsubscribe", {
                    method: "POST",
                    headers: { "Content-Type": "application/json", "Authorization": "Bearer " + opts.appAuthToken },
                    body: JSON.stringify({ endpoint, keys: { p256dh: "", auth: "" } })
                });
            } catch { /* best-effort */ }
        }
        return { ok: true };
    }

    window.fseduPush = { getStatus, enable, disable };
})();
