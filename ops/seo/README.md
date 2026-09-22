# SEO: crawler snapshots and Nginx rules (production VPS)

The storefront is client-rendered: without JavaScript every URL is the same `index.html` with a
generic title and no content. Link-preview bots already get a server-rendered OG page from Catalog
(`/api/seo/*`); this adds the same idea for **search engines**, with the real rendered pages.

## Pieces

| File | Goes to | What it does |
|---|---|---|
| `frontend/shell/scripts/prerender.mjs` | repo (run in place) | Reads `/api/sitemap.xml`, renders each URL in headless Chromium, strips executable scripts (keeps JSON-LD), writes `__prerender/<path>/index.html` (+ `q/<query>/` for category URLs). Aborts and keeps the old set if more than 20% fail. |
| `ops/seo/prerender.sh` | run from the repo | Runs the script in `mcr.microsoft.com/playwright:v<same version as node_modules>-noble` (no browser libs on the host). |
| `ops/seo/bot-detect.conf` | `/etc/nginx/conf.d/bot-detect.conf` | `$is_unfurl_ua` → `$is_bot` (link previews), `$is_search_bot` → `$prerender_page`, `$prerender_query`. |

Cron (root): `15 */6 * * * /var/www/atelie-layette-baby-microservicos/ops/seo/prerender.sh >> /var/log/atelie-prerender.log 2>&1`
— also run it once after every frontend deploy. Price/stock changes reach crawlers within 6 hours.

## Site config (`/etc/nginx/sites-enabled/atelie-bebe`, 443 server block)

Note this file is a plain file in `sites-enabled`, not the usual symlink into `sites-available` —
there *is* an `/etc/nginx/sites-available/atelie-bebe`, but it is a stale copy from before the
microservices migration and nothing includes it (`nginx.conf` includes `sites-enabled/*` only).
Edit the one in `sites-enabled`.

```nginx
    # One canonical host: https://www.layettebaby.com.br/* → https://layettebaby.com.br/*
    if ($host = www.layettebaby.com.br) {
        return 301 https://layettebaby.com.br$request_uri;
    }

    # Conventional sitemap address (what Search Console and Bing try by default) serves the real
    # sitemap instead of falling through to the SPA index.html ("Sitemap is HTML" in Search Console).
    location = /sitemap.xml {
        proxy_pass http://127.0.0.1:5100/api/sitemap.xml;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Uploaded photos have unique (GUID) names and are never overwritten in place.
    location ^~ /api/uploads/ {
        proxy_pass http://127.0.0.1:5100/api/uploads/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_hide_header Cache-Control;
        add_header Cache-Control "public, max-age=2592000, immutable";
    }

    # Search crawlers get the prerendered page; anything without a snapshot falls back to the SPA.
    location ^~ /__prerender/ {
        internal;
        root /var/www/atelie-bebe-microservices;
        try_files $uri$prerender_query/index.html $uri/index.html /index.html;
        add_header X-Prerendered "1";
    }

    location /produto/ {
        if ($is_bot) { rewrite ^/produto/([^/]+)$ /api/seo/product/$1 last; }
        if ($prerender_page) { rewrite ^(.*)$ /__prerender$1 last; }
        try_files $uri $uri/ /index.html;
    }

    location / {
        if ($is_bot) { rewrite ^(.*)$ /api/seo/default last; }
        if ($prerender_page) { rewrite ^(.*)$ /__prerender$1 last; }
        try_files $uri $uri/ /index.html;
    }
```

Always `nginx -t` before `systemctl reload nginx`, and keep a copy of the previous config.

## Checking

```sh
curl -sI -A Googlebot https://layettebaby.com.br/loja | grep -i x-prerendered   # 1
curl -s  -A Googlebot https://layettebaby.com.br/produto/<slug> | grep -o '<title>[^<]*'
curl -sI https://www.layettebaby.com.br/loja                                    # 301 → apex
```

The preview's own image has to come back as an **image** to the bot that just read the preview —
the check that matters, and the one that was missing when every shared link unfurled as a grey box:

```sh
FB='facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)'
img=$(curl -s -A "$FB" https://layettebaby.com.br/ | grep -o 'og:image" content="[^"]*"' | sed 's/.*content="//;s/"$//')
curl -sI -A "$FB" "$img" | grep -i content-type    # image/jpeg — NOT text/html, NOT image/webp
```

`text/html` means a bot User-Agent is being rewritten to `/api/seo/*` for static files too
(the page-route guard missing from `$is_bot`); `image/webp` means `og:image` is pointing at the
stored photo instead of its `-og.jpg` copy. Facebook caches a failed scrape, so after fixing either
one, re-scrape the URL in [Sharing Debugger](https://developers.facebook.com/tools/debug/) —
WhatsApp keeps its own cache for about 7 days and cannot be purged.

Outside the code (owner's Google account): verify the domain in Google Search Console and Bing
Webmaster Tools, submit `https://layettebaby.com.br/sitemap.xml` (same content as `/api/sitemap.xml`), and keep the Google Business
Profile up to date — for a local ateliê that profile weighs as much as the site itself.
