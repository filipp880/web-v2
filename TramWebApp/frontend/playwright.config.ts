import { defineConfig } from '@playwright/test';
export default defineConfig({ testDir: './tests', timeout: 45000, workers: 1,
  use: { baseURL: process.env.TEST_BASE_URL || 'http://127.0.0.1:5173', headless: true, viewport: { width: 1440, height: 1000 }, screenshot: 'only-on-failure' },
  reporter: [['list']],
});
