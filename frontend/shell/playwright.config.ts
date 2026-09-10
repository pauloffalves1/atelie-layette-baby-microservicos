import { defineConfig, devices } from '@playwright/test';

// End-to-end tests exercise the real micro-frontend the way a browser sees it: the `shell`
// (host) resolving the `storefront` remote via Native Federation at runtime, same as documented
// in microservices/README.md's "Rodando localmente" section (ports 4210/4201/4202). They assume
// the backend (Gateway + services, via `dotnet run` or `docker compose up`) is already running
// locally — Playwright only starts the 3 Angular dev servers, never the .NET backend.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  // The 3 dev servers behind these tests compile chunks on demand (esbuild/Native Federation) —
  // two workers hitting them at once made the first navigation in a run flaky (cold-compile took
  // longer than the default 5s expect timeout, e.g. home-and-shop's initial hero assertion).
  // Serializing removes the contention; these specs are fast enough that this costs little time.
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4210',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      command: 'npx ng serve storefront --port 4201',
      url: 'http://localhost:4201/remoteEntry.json',
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
    {
      command: 'npx ng serve admin --port 4202',
      url: 'http://localhost:4202/remoteEntry.json',
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
    {
      command: 'npx ng serve shell --port 4210',
      url: 'http://localhost:4210',
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
  ],
});
