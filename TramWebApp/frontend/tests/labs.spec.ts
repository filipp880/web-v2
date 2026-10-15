import { test, expect, type APIRequestContext } from '@playwright/test';

async function login(request: APIRequestContext, username = 'admin', password = 'admin123') {
  const response = await request.post('/api/auth/login', { data: { username, password } });
  expect(response.ok()).toBeTruthy();
  return { Authorization: 'Bearer ' + (await response.json()).accessToken };
}
const signal = { sequenceNumber: 0, timestamp: 0, controllerHandlePosition: 0, frontBogieSpeed: 10, rearBogieSpeed: 10 };

test('Swagger documents REST, schemas and authentication and opens in the browser', async ({ page, request }) => {
  const response = await request.get('/swagger/v1/swagger.json'); expect(response.ok()).toBeTruthy();
  const schema = await response.json();
  expect(schema.paths['/api/tram-runs'].get.summary).toBeTruthy();
  expect(schema.paths['/api/tram-runs'].post.summary).toBeTruthy();
  expect(schema.paths['/api/tram-runs/{id}'].put.summary).toBeTruthy();
  expect(schema.paths['/api/tram-runs/{id}'].delete.summary).toBeTruthy();
  expect(schema.paths['/api/simulation/{sourceId}/start'].post.responses['201']).toBeTruthy();
  expect(schema.components.securitySchemes.Bearer.scheme).toBe('bearer');
  expect(schema.security ?? []).toEqual([]);
  expect(schema.paths['/api/auth/login'].post.security ?? []).toEqual([]);
  expect(schema.paths['/api/tram-runs'].get.security).toEqual([{ Bearer: [] }]);
  await page.goto('/swagger/index.html');
  await expect(page.locator('.swagger-ui .info h2')).toContainText('TramWebApp API');
  await expect(page.locator('.opblock-post')).not.toHaveCount(0);
});

test('REST CRUD validates inputs and keeps telemetry counters and unique sequence numbers consistent', async ({ request }) => {
  const headers = await login(request);
  const invalid = await request.post('/api/tram-runs', { headers, data: { name: '', description: '' } }); expect(invalid.status()).toBe(400);
  const created = await request.post('/api/tram-runs', { headers, data: { name: 'Lab REST test', description: 'Temporary' } });
  expect(created.status()).toBe(201); const run = await created.json();
  expect(created.headers().location).toContain('/api/tram-runs/' + run.id);
  let inferenceId = '';
  try {
    const updated = await request.put('/api/tram-runs/' + run.id, { headers, data: { name: 'Lab REST updated', description: 'Updated' } });
    expect((await updated.json()).name).toBe('Lab REST updated');
    const badSignal = await request.post('/api/tram-runs/' + run.id + '/telemetry', { headers, data: { ...signal, controllerHandlePosition: 2 } }); expect(badSignal.status()).toBe(400);
    expect((await request.post('/api/tram-runs/' + run.id + '/telemetry', { headers, data: signal })).status()).toBe(201);
    expect((await request.post('/api/tram-runs/' + run.id + '/telemetry', { headers, data: signal })).status()).toBe(409);
    const inference = await request.post('/api/model/inference', { headers, data: { tramRunId: run.id, sequenceNumber: 0, input: signal } }); expect(inference.status()).toBe(201);
    inferenceId = (await inference.json()).id;
    const stored = await (await request.get('/api/tram-runs/' + run.id, { headers })).json();
    expect(stored.telemetryPointsCount).toBe(1); expect(stored.inferencesCount).toBe(1);
  } finally { expect((await request.delete('/api/tram-runs/' + run.id, { headers })).status()).toBe(204); }
  expect((await request.get('/api/tram-runs/' + run.id, { headers })).status()).toBe(404);
  expect((await request.get('/api/model/inferences/' + inferenceId, { headers })).status()).toBe(404);
});

test('anonymous, user and admin access extends to telemetry, results and corrections', async ({ request }) => {
  expect((await request.get('/api/tram-runs')).status()).toBe(401);
  const admin = await login(request); const operator = await login(request, 'operator', 'user123');
  const created = await (await request.post('/api/tram-runs', { headers: admin, data: { name: 'Private admin lab test', description: 'Temporary' } })).json();
  const own = await (await request.post('/api/tram-runs', { headers: operator, data: { name: 'Private user lab test', description: 'Temporary' } })).json();
  try {
    expect((await request.get('/api/tram-runs/' + own.id, { headers: operator })).status()).toBe(200);
    expect((await request.post('/api/tram-runs/' + own.id + '/telemetry', { headers: operator, data: signal })).status()).toBe(201);
    await request.post('/api/tram-runs/' + created.id + '/telemetry', { headers: admin, data: signal });
    const record = await (await request.post('/api/model/inference', { headers: admin, data: { tramRunId: created.id, sequenceNumber: 0, input: signal } })).json();
    for (const path of ['/api/tram-runs/' + created.id, '/api/tram-runs/' + created.id + '/telemetry', '/api/tram-runs/' + created.id + '/telemetry/0', '/api/model/inferences/run/' + created.id, '/api/model/inferences/' + record.id])
      expect((await request.get(path, { headers: operator })).status(), path).toBe(403);
    expect((await request.post('/api/tram-runs/' + created.id + '/telemetry', { headers: operator, data: { ...signal, sequenceNumber: 1 } })).status()).toBe(403);
    expect((await request.post('/api/model/inference', { headers: operator, data: { tramRunId: created.id, sequenceNumber: 0, input: signal } })).status()).toBe(403);
    expect((await request.put('/api/model/inferences/' + record.id + '/intervene', { headers: operator, data: { adjustedSpeed: 1, adjustedPosition: 1 } })).status()).toBe(403);
    expect((await request.delete('/api/tram-runs/' + own.id, { headers: operator })).status()).toBe(403);
    expect((await request.put('/api/tram-runs/' + own.id, { headers: admin, data: { name: 'Admin can update user run', description: 'Temporary' } })).status()).toBe(200);
  } finally {
    await request.delete('/api/tram-runs/' + created.id, { headers: admin });
    await request.delete('/api/tram-runs/' + own.id, { headers: admin });
  }
});

test('Lab2 InMemory stores at most 50 runs without silently replacing existing data', async ({ request }) => {
  test.skip(process.env.TEST_STORAGE_MODE !== 'InMemory', 'Only on the isolated InMemory test instance');
  const headers = await login(request);
  const existing = await (await request.get('/api/tram-runs', { headers })).json();
  const created: string[] = [];
  try {
    for (let index = existing.length; index < 50; index++) {
      const response = await request.post('/api/tram-runs', { headers, data: { name: 'Capacity test ' + index, description: 'Temporary' } });
      expect(response.status()).toBe(201); created.push((await response.json()).id);
    }
    const overflow = await request.post('/api/tram-runs', { headers, data: { name: 'Must be rejected', description: 'Temporary' } }); expect(overflow.status()).toBe(409);
    const retained = await (await request.get('/api/tram-runs', { headers })).json(); expect(retained.length).toBe(50);
    for (const run of existing) expect(retained.map((item: { id: string }) => item.id)).toContain(run.id);
  } finally { for (const id of created) await request.delete('/api/tram-runs/' + id, { headers }); }
});
