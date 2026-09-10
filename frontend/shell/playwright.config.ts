import { defineConfig, devices } from '@playwright/test';

// End-to-end tests exercise the real micro-frontend the way a browser sees it: the `shell`
// (host) resolving the `storefront` remote via Native Federation at runtime, same as documented
// in microservices/README.md's "Rodando localmente" section (ports 4210/4201/4202). They assume
// the backend (Gateway + services, via `dotnet run` or `docker compose up`) is already running
// locally — Playwright only starts the 3 Angular dev servers, never the .NET backend.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
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
