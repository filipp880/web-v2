import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../services/api';
import { RouteMap } from '../components/RouteMap';
import { formatNumber as number, integrateTelemetry } from '../utils/telemetry';
import type { TramRun, TramTelemetryPoint } from '../types';

export function DrivePage() {
  const { id } = useParams(); const navigate = useNavigate();
  const [run, setRun] = useState<TramRun | null>(null);
  const [points, setPoints] = useState<TramTelemetryPoint[]>([]);
  const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  const [active, setActive] = useState(false); const [pending, setPending] = useState(false);
  const [winter, setWinter] = useState(true); const [initialSpeed, setInitialSpeed] = useState('36');
  const [handle, setHandle] = useState(0);
  const [distance, setDistance] = useState(0);
  const [inMemory, setInMemory] = useState(false);
  const handleRef = useRef(handle); handleRef.current = handle;
  const alive = useRef(false); const busy = useRef(false);
  const currentId = useRef(id); currentId.current = id;
  const last = points[points.length - 1];
  const simulation = run?.description.startsWith('simulation:v1;') ?? false;
  useEffect(() => {
    let cancelled = false; alive.current = false; setActive(false); setLoading(true); setError('');
    void Promise.all([api.getTramRun(id!), api.getTelemetry(id!), api.getStorageInfo()]).then(([details, data, storage]) => {
      if (cancelled) return;
      setInMemory(!storage.persistent);
      const own = details.description.startsWith('simulation:v1;');
      data.sort((a, b) => a.sequenceNumber - b.sequenceNumber);
      const distances = integrateTelemetry(data);
      setRun(details); setPoints(own ? data : []); setDistance(own ? data[data.length - 1]?.distanceMeters ?? distances[distances.length - 1] ?? 0 : 0);
      if (own) { setWinter(details.description.startsWith('simulation:v1;winter;')); setHandle(data[data.length - 1]?.controllerHandlePosition ?? 0); }
    }).catch(() => { if (!cancelled) setError('Не удалось загрузить прогон. Вернитесь к списку и повторите.'); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; alive.current = false; };
  }, [id]);

  async function tick() {
    if (!id || !last || busy.current) return;
    busy.current = true; setPending(true); setError('');
    try {
      const result = await api.simulationStep(id, handleRef.current, last.sequenceNumber + 1);
      const stored = await api.getTelemetryPoint(id, result.sequenceNumber);
      if (currentId.current !== id) return;
      setPoints(previous => previous.some(point => point.sequenceNumber === stored.sequenceNumber) ? previous : [...previous, stored]);
      setDistance(result.distance);
    } catch {
      if (currentId.current !== id) return;
      alive.current = false; setActive(false);
      setError('Запись не подтверждена сервером. Движение приостановлено. Нажмите «Шаг +1 с» для повтора — дубликата не будет.');
    } finally { busy.current = false; setPending(false); }
  }
  useEffect(() => {
    if (!active || !last || pending) return;
    const timer = window.setTimeout(() => { if (alive.current) void tick(); }, 1000);
    return () => window.clearTimeout(timer);
    // Each confirmed point schedules the next tick; moving the handle does not reset the timer.
  }, [active, last, pending, id]);
  async function start() {
    if (busy.current) return;
    if (simulation) { alive.current = true; setActive(true); return; }
    const speed = Number(initialSpeed);
    if (!initialSpeed.trim() || !Number.isFinite(speed) || speed < 0 || speed > 80) { setError('Начальная скорость должна быть от 0 до 80 км/ч.'); return; }
    busy.current = true; setPending(true);
    try { const created = await api.startSimulation(id!, speed, winter, handle); navigate('/runs/' + created.runId + '/drive', { replace: true }); }
    catch { setError('Не удалось создать прогон. Проверьте доступность API и выбранного хранилища.'); }
    finally { busy.current = false; setPending(false); }
  }
  if (loading) return <div className="card state-panel" role="status">Загружаем тренажёр…</div>;
  if (!run) return <div className="card state-panel" role="alert">{error}<Link to="/runs">К прогонам</Link></div>;
  const speed = last?.frontBogieSpeed ?? (Number(initialSpeed) || 0) / 3.6;
  return <div className="drive-page">
    <div className="page-header"><div><Link to="/runs" className="back-link">← Все прогоны</Link><h1 className="page-title">Тренажёр движения</h1><p className="page-subtitle">Ручка удерживает команду. Скорость меняется постепенно; каждый шаг записывается на сервере.</p></div><span className="badge badge-info">{winter ? '❄ Зима · сниженное сцепление' : 'Сухие рельсы'}</span></div>
    {error && <div className="map-notice" role="alert">{error}</div>}
    <div className="drive-layout"><section className="card drive-console"><div className="card-body">
      <div className="drive-speed"><strong data-testid="drive-speed">{number(speed * 3.6, 1)}</strong><span>км/ч</span></div>
      <div className="drive-statistics"><span>Путь <b data-testid="drive-distance">{number(distance, 1)} м</b></span><span>Время <b>{number(last?.timestamp ?? 0, 0)} с</b></span></div>
      <label className="form-label" htmlFor="drive-handle">Ручка управления · {number(handle)}</label>
      <input id="drive-handle" className="controller-slider" type="range" min="-1" max="1" step="0.01" value={handle} onChange={event => setHandle(Number(event.target.value))} />
      <div className="controller-scale"><span>−1 · Тормоз</span><span>0 · Нейтраль</span><span>+1 · Тяга</span></div>
      <div className="controller-presets"><button className="btn btn-outline" onClick={() => setHandle(-1)}>Тормоз</button><button className="btn btn-outline" onClick={() => setHandle(0)}>Нейтраль</button><button className="btn btn-primary" onClick={() => setHandle(1)}>Тяга</button></div>
      <p className="helper-text">{handle < 0 ? 'Плавное торможение с учетом сцепления.' : handle > 0 ? 'Постепенный разгон по заданному пути.' : 'Движение по инерции, без тяги.'}</p>
      {!simulation && <><label className="form-label" htmlFor="initial-speed">Начальная скорость, км/ч</label><input id="initial-speed" className="form-control" type="number" min="0" max="80" value={initialSpeed} onChange={event => setInitialSpeed(event.target.value)} /><label className="winter-switch"><input type="checkbox" checked={winter} onChange={event => setWinter(event.target.checked)} /> Зима: сниженное сцепление</label></>}
      <div className="drive-actions">{active ? <button className="btn btn-outline" onClick={() => { alive.current = false; setActive(false); }}>Пауза</button> : <button className="btn btn-primary" disabled={pending} onClick={() => void start()}>{simulation ? 'Продолжить движение' : 'Создать прогон'}</button>}{simulation && <button className="btn btn-outline" disabled={active || pending} onClick={() => void tick()}>Шаг +1 с</button>}</div>
      <p className="storage-status" role="status">{pending ? 'Запись в БД…' : simulation ? (inMemory ? '✓ В памяти сохранено точек: ' : '✓ В БД сохранено точек: ') + points.length : 'Будет создан отдельный прогон; исходные данные сохранятся.'}</p>
      {simulation && <Link className="back-link" to={'/runs/' + id}>Открыть записанную телеметрию →</Link>}
    </div></section><section className="card drive-map"><RouteMap telemetry={points} currentPoint={last} plannedRoute={run.plannedRoute} distanceMeters={distance} /></section></div>
    <section className="card"><div className="card-header"><h2>Данные, подтвержденные базой <span className="unit">последние 10 точек</span></h2></div><div className="table-container"><table className="table"><thead><tr><th>Шаг</th><th>Время, с</th><th>Ручка</th><th>Скорость, км/ч</th><th>Запись</th></tr></thead><tbody>{points.slice(-10).reverse().map(point => <tr key={point.id}><td>{point.sequenceNumber + 1}</td><td>{number(point.timestamp, 0)}</td><td>{number(point.controllerHandlePosition)}</td><td>{number(point.frontBogieSpeed * 3.6, 1)}</td><td><span className="badge badge-success">Сохранено</span></td></tr>)}{!points.length && <tr><td colSpan={5}>Создайте прогон, чтобы начать запись.</td></tr>}</tbody></table></div></section>
    <p className="helper-text">Учебная динамика с инерцией и ограничением ускорения. Параметры зимнего сцепления иллюстративные, не калиброванные по реальному вагону. При перезагрузке движение на паузе; записанные данные остаются.</p>
  </div>;
}
