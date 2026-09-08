// Self-destructing service worker. The old monolith (client/) registered a real Angular service
// worker at this exact scope/URL before the production migration to microservices/. This new
// shell app never registers one, so any browser that still has the old one installed keeps
// running it forever — it tries to self-update by re-fetching this same URL, but nothing here
// used to exist, so Nginx's SPA fallback served index.html back to it, which isn't valid JS and
// can never look like a "new version" to the update check. That stale worker then goes on serving
// its own old cached bundle indefinitely, independent of whatever gets deployed here.
//
// This file exists purely so that check succeeds: it's a real, valid script that immediately
// unregisters itself and clears every Cache Storage entry, then forces every open tab to reload
// once so the real (non-service-worker) app loads fresh. Once no stale installs remain (safe to
// verify via a site's real users over time — a few weeks), this file can be deleted.
self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(keys.map((key) => caches.delete(key)));
      await self.registration.unregister();
      const clientsList = await self.clients.matchAll({ type: 'window' });
      clientsList.forEach((client) => client.navigate(client.url));
    })(),
  );
});
