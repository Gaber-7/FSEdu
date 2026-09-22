// FSEdu — Web Push Service Worker
// Receives push events from FSEdu backend and renders OS-level notifications.
// Click → opens the in-app URL specified in the push payload.

self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", e => e.waitUntil(self.clients.claim()));

self.addEventListener("push", event => {
    let data = { title: "FSEdu", body: "إشعار جديد", url: "/" };
    if (event.data) {
        try { data = Object.assign(data, event.data.json()); }
        catch { try { data.body = event.data.text(); } catch { /* ignore */ } }
    }
    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: "/favicon.png",
            badge: "/favicon.png",
            data: { url: data.url },
            dir: "rtl",
            lang: "ar"
        })
    );
});

self.addEventListener("notificationclick", event => {
    event.notification.close();
    const url = (event.notification.data && event.notification.data.url) || "/";
    event.waitUntil((async () => {
        const all = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
        for (const c of all) {
            // If a window for this app is already open, focus and navigate it.
            if ("focus" in c) { try { await c.focus(); c.navigate(url); return; } catch { /* navigate may fail cross-origin */ } }
        }
        if (self.clients.openWindow) await self.clients.openWindow(url);
    })());
});
