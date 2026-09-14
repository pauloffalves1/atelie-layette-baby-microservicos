// Prerenders every URL in the sitemap into static HTML snapshots for search-engine crawlers.
//
// The storefront is a client-rendered Angular app: every URL answers with the same near-empty
// index.html (generic <title>, no text, no links) until JavaScript runs. Googlebot renders JS in a
// deferred second pass and Bing/DuckDuckGo often don't render it at all, so pages were indexed late,
// with the wrong titles, or not at all. Nginx serves these snapshots to known search crawlers only
// (ops/seo/README.md) — the same content people see, just already rendered.
//
// Run by ops/seo/prerender.sh (cron, and after each frontend deploy). Env:
//   SITE_URL     origin to render (default https://layettebaby.com.br)
//   SITEMAP_URL  sitemap to read the URL list from (default SITE_URL/api/sitemap.xml)
//   OUT_DIR      where snapshots go: OUT_DIR/<path>/index.html, OUT_DIR/<path>/q/<query>/index.html
import { mkdir, rename, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const SITE_URL = (process.env.SITE_URL ?? 'https://layettebaby.com.br').replace(/\/$/, '');
const SITEMAP_URL = process.env.SITEMAP_URL ?? `${SITE_URL}/api/sitemap.xml`;
const OUT_DIR = process.env.OUT_DIR ?? '/var/www/atelie-bebe-microservices/__prerender';
const CONCURRENCY = 2;
/** Too many failures means something is broken (API down, bad deploy) — keep the previous snapshots. */
const MAX_FAILURE_RATIO = 0.2;
/** Must not match the bot user-agent maps in Nginx, or the renderer would be served a snapshot. */
const USER_AGENT = 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0 Safari/537.36 AtelieBebePrerender/1.0';
/** Third-party calls that would count the renderer as a visitor, or that it doesn't need. */
const BLOCKED_HOSTS = ['google-analytics.com', 'googletagmanager.com', 'nr-data.net', 'newrelic.com', 'doubleclick.net', 'facebook.net'];

const log = (...args) => console.log(new Date().toISOString(), ...args);

const sitemap = await (await fetch(SITEMAP_URL)).text();
const targets = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)]
  .map((match) => new URL(match[1].replaceAll('&amp;', '&')))
  .map((url) => ({ path: url.pathname, query: url.search.slice(1) }));
if (targets.length === 0) throw new Error(`No URLs in ${SITEMAP_URL}`);
log(`rendering ${targets.length} URLs from ${SITEMAP_URL} against ${SITE_URL}`);

const tmpDir = `${OUT_DIR}.tmp-${process.pid}`;
await rm(tmpDir, { recursive: true, force: true });

const browser = await chromium.launch();
const context = await browser.newContext({ userAgent: USER_AGENT, viewport: { width: 1280, height: 900 }, locale: 'pt-BR' });
// No cookie banner in the snapshot (and no analytics consent).
await context.addInitScript(() => localStorage.setItem('atelie-bebe.cookie-consent', 'declined'));
await context.route('**/*', (route) => {
  const host = new URL(route.request().url()).hostname;
  return BLOCKED_HOSTS.some((blocked) => host.endsWith(blocked)) ? route.abort() : route.continue();
});

const failures = [];

async function render(target) {
  const url = `${SITE_URL}${target.path}${target.query ? `?${target.query}` : ''}`;
  const page = await context.newPage();
  try {
    await page.goto(url, { waitUntil: 'networkidle', timeout: 45_000 });
    await page.waitForSelector('main#conteudo h1', { timeout: 20_000 });
    // Lists and product data load after the page shell — wait for their spinners to go away.
    await page.waitForFunction(() => !document.querySelector('main#conteudo .spinner-border'), null, { timeout: 20_000 }).catch(() => {});
    await page.waitForTimeout(300);

    const snapshot = await page.evaluate(() => {
      if (document.querySelector('meta[name="robots"][content*="noindex"]')) return null;
      // Crawlers get the rendered document, not the app: drop executable scripts (Angular, federation
      // import maps, analytics) but keep JSON-LD structured data.
      document.querySelectorAll('script:not([type="application/ld+json"]), link[rel="modulepreload"]').forEach((el) => el.remove());
      return `<!doctype html>\n${document.documentElement.outerHTML}`;
    });
    if (!snapshot) throw new Error('page rendered as "not found" (noindex)');
    if (!snapshot.includes('<h1') || snapshot.length < 5_000) throw new Error(`suspiciously small snapshot (${snapshot.length} chars)`);

    const dir = path.join(tmpDir, ...target.path.split('/').filter(Boolean), ...(target.query ? ['q', target.query] : []));
    await mkdir(dir, { recursive: true });
    await writeFile(path.join(dir, 'index.html'), snapshot, 'utf8');
    log('ok  ', url);
  } catch (error) {
    failures.push(url);
    log('FAIL', url, String(error?.message ?? error).split('\n')[0]);
  } finally {
    await page.close();
  }
}

const queue = [...targets];
await Promise.all(Array.from({ length: CONCURRENCY }, async () => {
  while (queue.length) await render(queue.shift());
}));
await browser.close();

if (failures.length / targets.length > MAX_FAILURE_RATIO) {
  await rm(tmpDir, { recursive: true, force: true });
  log(`aborting: ${failures.length}/${targets.length} failed — previous snapshots kept`);
  process.exit(1);
}

// Swap in the new snapshot set; a crawler hitting the gap in between just falls back to the SPA.
const oldDir = `${OUT_DIR}.old-${process.pid}`;
await rename(OUT_DIR, oldDir).catch(() => {});
await rename(tmpDir, OUT_DIR);
await rm(oldDir, { recursive: true, force: true });
log(`done: ${targets.length - failures.length} snapshots written to ${OUT_DIR}, ${failures.length} failed`);
