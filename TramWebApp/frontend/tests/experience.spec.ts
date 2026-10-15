import { test, expect, type Page } from '@playwright/test';

test('login, validation, search and run creation/editing/deletion through the interface', async ({ page }) => {
  await page.goto('/login');
  await page.getByRole('button', { name: 'Войти', exact: true }).click();
  await expect(page.getByText('Введите имя пользователя', { exact: true })).toBeVisible();
  await page.getByLabel('Имя пользователя', { exact: true }).fill('admin');
  await page.getByLabel('Пароль', { exact: true }).fill('admin123');
  await page.getByRole('button', { name: 'Войти', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Прогоны трамвая' })).toBeVisible();
  const name = 'UI test ' + Date.now();
  await page.getByRole('button', { name: 'Добавить прогон' }).click();
  await page.getByLabel('Название *', { exact: true }).fill(name);
  await page.getByLabel('Описание', { exact: true }).fill('Temporary browser test');
  await page.getByRole('button', { name: 'Создать', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeHidden();
  await page.getByLabel('Поиск прогонов').fill(name);
  await page.getByRole('button', { name: 'Редактировать ' + name, exact: true }).click();
  await page.getByLabel('Описание', { exact: true }).fill('Updated in browser');
  await page.getByRole('button', { name: 'Сохранить', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'Updated in browser', exact: true })).toBeVisible();
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Удалить ' + name, exact: true }).click();
  await expect(page.getByText('По вашему запросу ничего не найдено.', { exact: true })).toBeVisible();
});

async function setup(page: Page) {
  const response = await page.request.post('/api/auth/login', { data: { username: 'admin', password: 'admin123' } });
  expect(response.ok()).toBeTruthy();
  const login = await response.json();
  await page.addInitScript(token => localStorage.setItem('accessToken', token), login.accessToken);
  const headers = { Authorization: 'Bearer ' + login.accessToken };
  const runs = await (await page.request.get('/api/tram-runs', { headers })).json();
  return { headers, run: runs.find((run: { name: string }) => run.name.includes('Городской центр')) };
}
async function step(page: Page, value: number) {
  await page.getByRole('slider', { name: 'Текущий шаг' }).evaluate((element, index) => {
    Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')!.set!.call(element, String(index));
    element.dispatchEvent(new Event('input', { bubbles: true }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
  }, value);
  await expect(page.getByText('Шаг: ' + (value + 1) + ' /', { exact: false })).toBeVisible();
}

test('all telemetry steps, absent GPS and playback remain stable without writing history', async ({ page }) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  const { run } = await setup(page);
  const writes: string[] = []; page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/model/inference')) writes.push(request.url()); });
  await page.goto('/runs/' + run.id + '/model');
  await expect(page.locator('#timeline')).toBeVisible();
  for (let index = 0; index < 200; index++) await step(page, index);
  await step(page, 5);
  await expect(page.getByTestId('gps-speed')).toHaveText('—');
  await expect(page.getByText('GPS недоступен на этом шаге.', { exact: false })).toBeVisible();
  await page.getByLabel('Скорость воспроизведения').selectOption('5');
  await page.getByRole('button', { name: 'Воспроизведение', exact: true }).click();
  await expect(page.locator('#timeline')).not.toHaveValue('5');
  await page.getByRole('button', { name: 'Пауза', exact: true }).click();
  const paused = await page.locator('#timeline').inputValue();
  await page.waitForTimeout(500);
  await expect(page.locator('#timeline')).toHaveValue(paused);
  await step(page, 199);
  await expect(page.getByRole('button', { name: 'Следующий шаг' })).toBeDisabled();
  await page.getByRole('button', { name: 'Сброс', exact: true }).click();
  await expect(page.locator('#timeline')).toHaveValue('0');
  expect(writes).toEqual([]); expect(errors).toEqual([]);
  await step(page, 99);
  await page.screenshot({ path: 'test-results/model-desktop.png', fullPage: true });
});

test('saving and intervention persist the selected result', async ({ page }) => {
  const { run, headers } = await setup(page);
  await page.goto('/runs/' + run.id + '/model');
  await expect(page.locator('#timeline')).toBeVisible(); await step(page, 5);
  const created = page.waitForResponse(response => response.url().endsWith('/model/inference') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Сохранить результат', exact: true }).click();
  const inference = await (await created).json();
  await expect(page.getByRole('button', { name: '✓ Результат сохранён' })).toBeDisabled();
  await page.getByText('Корректировка оператора', { exact: true }).click();
  await page.getByLabel('Скорость, м/с', { exact: true }).fill('1.2');
  await page.getByLabel('Пройденный путь, м', { exact: true }).fill('3.5');
  await page.getByRole('button', { name: 'Сохранить корректировку' }).click();
  await expect(page.getByText('Корректировка сохранена в истории', { exact: true })).toBeVisible();
  const persisted = await (await page.request.get('/api/model/inferences/' + inference.id, { headers })).json();
  expect(persisted.userAdjustedSpeed).toBe(1.2); expect(persisted.userAdjustedPosition).toBe(3.5);
  await page.goto('/history');
  await expect(page.getByRole('heading', { name: 'История инференсов' })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
});

test('local map, route geometry, telemetry pagination and responsive layouts', async ({ page }) => {
  const { run, headers } = await setup(page);
  const detail = await (await page.request.get('/api/tram-runs/' + run.id, { headers })).json();
  expect(detail.plannedRoute.length).toBeGreaterThan(20);
  const foreignRequests: string[] = [];
  page.on('request', request => { if (!new URL(request.url()).hostname.match(/localhost|127\.0\.0\.1/)) foreignRequests.push(request.url()); });
  await page.goto('/runs/' + run.id);
  await page.getByRole('tab', { name: 'Карта', exact: true }).click();
  await expect(page.getByText('Москва · автономная карта')).toBeVisible();
  await expect(page.getByRole('img', { name: 'Схема маршрута с положением трамвая' })).toBeVisible();
  await page.getByRole('button', { name: 'Карта', exact: true }).click();
  await expect(page.locator('.leaflet-container canvas')).toBeVisible();
  await page.getByRole('tab', { name: 'Телеметрия (200)' }).click();
  await expect(page.getByText('1–25 из 200')).toBeVisible();
  await page.getByRole('button', { name: 'Далее', exact: true }).click();
  await expect(page.getByText('26–50 из 200')).toBeVisible();
  for (const width of [1440, 1024, 768, 390]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/runs/' + run.id + '/model');
    await expect(page.locator('#timeline')).toBeVisible(); await step(page, 5);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(overflow, 'horizontal page overflow at width ' + width).toBeFalsy();
    if (width === 390) await page.screenshot({ path: 'test-results/model-mobile.png', fullPage: true });
  }
  expect(foreignRequests).toEqual([]);
});

test('winter dynamics persist gradual braking, coasting and acceleration with idempotent steps', async ({ page }) => {
  const { run, headers } = await setup(page);
  const created = await (await page.request.post('/api/simulation/' + run.id + '/start', { headers, data: { initialSpeedKmh: 36, winter: true } })).json();
  const id = created.runId; expect(id).toBeTruthy();
  try {
    const step = async (handle: number, sequenceNumber: number) => {
      const response = await page.request.post('/api/simulation/' + id + '/step', { headers, data: { handle, sequenceNumber } });
      expect(response.ok()).toBeTruthy(); return response.json();
    };
    const brake = await step(-1, 1); expect(brake.speed).toBeGreaterThan(9.6); expect(brake.speed).toBeLessThan(10);
    const duplicate = await step(-1, 1); expect(duplicate.sequenceNumber).toBe(1); expect(duplicate.distance).toBe(brake.distance);
    const coast = await step(0, 2); expect(coast.speed).toBeGreaterThan(brake.speed - .05);
    const traction = await step(1, 3); expect(traction.speed).toBeGreaterThan(coast.speed);
    expect(traction.distance).toBeGreaterThan(coast.distance);
    const telemetry = await (await page.request.get('/api/tram-runs/' + id + '/telemetry', { headers })).json();
    expect(telemetry.length).toBe(4); expect(telemetry[3].distanceMeters).toBeCloseTo(traction.distance);
    const history = await (await page.request.get('/api/model/inferences/run/' + id, { headers })).json(); expect(history.length).toBe(4);
    const bad = await page.request.post('/api/simulation/' + id + '/step', { headers, data: { handle: -2, sequenceNumber: 4 } }); expect(bad.status()).toBe(400);
    let speed = traction.speed;
    for (let index = 4; index <= 43; index++) {
      const state = await step(-1, index);
      expect(state.speed).toBeGreaterThanOrEqual(0);
      expect(speed - state.speed).toBeLessThan(.33);
      speed = state.speed;
    }
    expect(speed).toBe(0);
  } finally { await page.request.delete('/api/tram-runs/' + id, { headers }); }
});

test('live driving holds handle, writes visible telemetry and survives reload', async ({ page }) => {
  const { run, headers } = await setup(page);
  await page.goto('/runs/' + run.id + '/drive');
  await page.getByRole('button', { name: 'Создать прогон', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Продолжить движение', exact: true })).toBeVisible();
  const id = page.url().split('/runs/')[1].split('/')[0];
  try {
    await page.getByRole('button', { name: 'Тормоз', exact: true }).click();
    await page.getByRole('button', { name: 'Продолжить движение', exact: true }).click();
    await expect(page.getByText(/^✓ В (БД|памяти) сохранено точек: 4$/)).toBeVisible({ timeout: 12000 });
    await page.getByRole('button', { name: 'Пауза', exact: true }).click();
    await expect(page.locator('#drive-handle')).toHaveValue('-1');
    const speed = Number((await page.getByTestId('drive-speed').innerText()).replace(',', '.')); expect(speed).toBeGreaterThan(30); expect(speed).toBeLessThan(36);
    await page.getByRole('button', { name: 'Шаг +1 с', exact: true }).click();
    await expect(page.getByText(/^✓ В (БД|памяти) сохранено точек: 5$/)).toBeVisible();
    const distance = await page.getByTestId('drive-distance').innerText();
    await page.reload();
    await expect(page.getByText(/^✓ В (БД|памяти) сохранено точек: 5$/)).toBeVisible();
    await expect(page.getByTestId('drive-distance')).toHaveText(distance);
    await expect(page.locator('#drive-handle')).toHaveValue('-1');
    await page.evaluate(() => scrollTo(0, 0));
    await page.screenshot({ path: 'test-results/drive-desktop.png', fullPage: true, animations: 'disabled' });
    await page.setViewportSize({ width: 390, height: 900 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBeFalsy();
    await page.screenshot({ path: 'test-results/drive-mobile.png', fullPage: true, animations: 'disabled' });
    await page.getByRole('link', { name: 'Открыть записанную телеметрию →' }).click();
    await page.getByRole('tab', { name: 'Телеметрия (5)' }).click();
    await expect(page.getByRole('table')).toBeVisible();
  } finally { await page.request.delete('/api/tram-runs/' + id, { headers }); }
});

test('API failure and empty telemetry show recoverable states', async ({ page }) => {
  const { run } = await setup(page);
  await page.route('**/api/model/predict/batch', route => route.fulfill({ status: 503, body: '{}' }));
  await page.goto('/runs/' + run.id + '/model');
  await expect(page.getByText('Не удалось открыть прогон', { exact: true })).toBeVisible();
  await page.unroute('**/api/model/predict/batch');
  await page.getByRole('button', { name: 'Повторить', exact: true }).click();
  await expect(page.locator('#timeline')).toBeVisible();
  await page.route('**/api/tram-runs/*/telemetry', route => route.fulfill({ contentType: 'application/json', body: '[]' }));
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Нет телеметрии' })).toBeVisible();
});

test('manual signals recalculate, survive seeking, validate and save the same scenario', async ({ page }) => {
  const { run, headers } = await setup(page);
  await page.goto('/runs/' + run.id + '/model');
  await expect(page.locator('#timeline')).toBeVisible(); await step(page, 5);
  const handle = page.getByRole('slider', { name: 'Ручка контроллера', exact: true });
  await handle.focus(); await page.keyboard.press('End');
  await expect(page.getByLabel('Положение ручки', { exact: true })).toHaveValue('1');
  await page.getByLabel('Передняя тележка, м/с', { exact: true }).fill('10');
  await page.getByLabel('Задняя тележка, м/с', { exact: true }).fill('10');
  await expect(page.getByText('Сценарий рассчитан · изменено шагов: 1')).toBeVisible();
  await step(page, 6); await step(page, 5);
  await expect(page.getByLabel('Передняя тележка, м/с', { exact: true })).toHaveValue('10');
  await page.getByLabel('Передняя тележка, м/с', { exact: true }).fill('-1');
  await expect(page.getByRole('button', { name: 'Сохранить результат', exact: true })).toBeDisabled();
  await expect(page.getByText('Проверьте диапазоны значений.', { exact: false })).toBeVisible();
  await page.getByLabel('Передняя тележка, м/с', { exact: true }).fill('10');
  const created = page.waitForResponse(response => response.url().endsWith('/model/inference') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Сохранить результат', exact: true }).click();
  const response = await created; expect(response.ok()).toBeTruthy();
  const inference = await response.json();
  expect(inference.inputControllerPosition).toBe(1); expect(inference.inputFrontSpeed).toBe(10);
  expect(inference.predictedSpeed).toBeCloseTo(12);
  const body = response.request().postDataJSON();
  const preview = await (await page.request.post('/api/model/predict/batch', { headers, data: body.scenarioInputs })).json();
  expect(inference.predictedPosition).toBeCloseTo(preview.at(-1).predictedPosition);
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({ path: 'test-results/manual-desktop.png', fullPage: true, animations: 'disabled' });
  await page.getByRole('button', { name: 'Сбросить все правки', exact: true }).click();
  await expect(page.getByLabel('Передняя тележка, м/с', { exact: true })).not.toHaveValue('10');
  await expect(page.getByText('Показаны исходные данные прогона')).toBeVisible();
  await page.route('**/api/model/predict/batch', route => route.fulfill({ status: 503, body: '{}' }));
  await page.getByRole('button', { name: 'Тормоз', exact: true }).click();
  await expect(page.getByText('Не удалось пересчитать сценарий.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Сохранить результат', exact: true })).toBeDisabled();
  await page.unroute('**/api/model/predict/batch');
  await page.getByRole('button', { name: 'Повторить расчёт', exact: true }).click();
  await expect(page.getByText('Сценарий рассчитан · изменено шагов: 1')).toBeVisible();
  await page.setViewportSize({ width: 390, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBeFalsy();
});
