import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Chart, registerables } from 'chart.js';
import { api } from '../services/api';
import { useToast } from '../contexts/ToastContext';
import { RouteMap } from '../components/RouteMap';
import { ManualControls, type EditableSignals } from '../components/ManualControls';
import { formatNumber as number, integrateTelemetry, toModelInput } from '../utils/telemetry';
import type { ModelInference, ModelInferenceListItem, ModelOutput, TramRun, TramTelemetryPoint } from '../types';
Chart.register(...registerables);

export function ModelPage() {
  const { id } = useParams<{ id: string }>();
  const { showToast } = useToast();
  const [run, setRun] = useState<TramRun | null>(null);
  const [telemetry, setTelemetry] = useState<TramTelemetryPoint[]>([]);
  const [outputs, setOutputs] = useState<ModelOutput[]>([]);
  const [baselineOutputs, setBaselineOutputs] = useState<ModelOutput[]>([]);
  const [overrides, setOverrides] = useState<Record<number, Partial<EditableSignals>>>({});
  const [manualValid, setManualValid] = useState(true);
  const [editVersion, setEditVersion] = useState(0);
  const [predictionFor, setPredictionFor] = useState<TramTelemetryPoint[] | null>(null);
  const [predictionError, setPredictionError] = useState(false);
  const [predictionRetry, setPredictionRetry] = useState(0);
  const scenario = useMemo(() => Object.keys(overrides).length ? telemetry.map(point => ({ ...point, ...overrides[point.sequenceNumber] })) : telemetry, [telemetry, overrides]);
  const recalculating = predictionFor !== scenario;
  const [inferences, setInferences] = useState<ModelInferenceListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [retry, setRetry] = useState(0);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [playSpeed, setPlaySpeed] = useState(1);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState<{ index: number; inference: ModelInference } | null>(null);
  const [adjustedSpeed, setAdjustedSpeed] = useState('');
  const [adjustedPosition, setAdjustedPosition] = useState('');
  const speedCanvas = useRef<HTMLCanvasElement>(null);
  const positionCanvas = useRef<HTMLCanvasElement>(null);
  const saveLock = useRef(false);
  const generation = useRef(0);
  useEffect(() => {
    let cancelled = false;
    generation.current += 1;
    setLoading(true); setLoadError(false); setIsPlaying(false); setCurrentIndex(0); setSaved(null);
    setOutputs([]); setTelemetry([]);
    setOverrides({}); setManualValid(true); setPredictionFor(null); setPredictionError(false);
    async function load() {
      try {
        if (!id) throw new Error('Missing run');
        const [details, points, history] = await Promise.all([api.getTramRun(id), api.getTelemetry(id), api.getInferencesByRun(id)]);
        points.sort((a, b) => a.sequenceNumber - b.sequenceNumber);
        const predictions = points.length ? await api.predictBatch(points.map(toModelInput)) : [];
        if (cancelled) return;
        setRun(details); setTelemetry(points); setOutputs(predictions); setBaselineOutputs(predictions); setPredictionFor(points); setInferences(history);
      } catch { if (!cancelled) setLoadError(true); }
      finally { if (!cancelled) setLoading(false); }
    }
    void load();
    return () => { cancelled = true; };
  }, [id, retry]);
  useEffect(() => {
    if (loading || !scenario.length || predictionFor === scenario) return;
    let cancelled = false;
    setPredictionError(false);
    const timer = window.setTimeout(() => {
      void api.predictBatch(scenario.map(toModelInput)).then(predictions => {
        if (!cancelled) { setOutputs(predictions); setPredictionFor(scenario); }
      }).catch(() => { if (!cancelled) setPredictionError(true); });
    }, 180);
    return () => { cancelled = true; window.clearTimeout(timer); };
  }, [scenario, loading, predictionFor, predictionRetry]);
  const distances = useMemo(() => integrateTelemetry(scenario, outputs), [scenario, outputs]);
  const referenceDistances = useMemo(() => integrateTelemetry(telemetry), [telemetry]);
  const currentPoint = scenario[currentIndex];
  const output = outputs[currentIndex];
  const gpsSpeed = currentPoint?.gpsSpeed;
  const speedError = gpsSpeed != null && output ? Math.abs(output.predictedSpeed - gpsSpeed) : null;
  useEffect(() => {
    if (!isPlaying || saving || recalculating || !manualValid) return;
    if (currentIndex >= telemetry.length - 1) { setIsPlaying(false); return; }
    const interval = Math.max(0.05, telemetry[currentIndex + 1].timestamp - telemetry[currentIndex].timestamp);
    const timer = window.setTimeout(() => setCurrentIndex(index => index + 1), interval * 1000 / playSpeed);
    return () => window.clearTimeout(timer);
  }, [isPlaying, currentIndex, telemetry, playSpeed, saving, recalculating, manualValid]);
  useEffect(() => {
    if (loading || !telemetry.length || !speedCanvas.current || !positionCanvas.current) return;
    const points = scenario.slice(0, currentIndex + 1);
    const values = (get: (point: TramTelemetryPoint, index: number) => number | null | undefined) => points.map((point, index) => ({ x: point.timestamp, y: get(point, index) ?? null }));
    const options = { responsive: true, maintainAspectRatio: false, animation: false as const,
      plugins: { legend: { position: 'bottom' as const, labels: { boxWidth: 12, usePointStyle: true } } },
      scales: { x: { type: 'linear' as const, min: 0, max: telemetry[telemetry.length - 1].timestamp || 1, title: { display: true, text: 'Время, с' } }, y: { beginAtZero: true } } };
    const speedChart = new Chart(speedCanvas.current, { type: 'line', options, data: { datasets: [
      { label: 'Модель', data: values((_, index) => outputs[index]?.predictedSpeed), borderColor: '#2563eb', pointRadius: 0, borderWidth: 2 },
      ...(Object.keys(overrides).length ? [{ label: 'Исходная модель', data: values((_, index) => baselineOutputs[index]?.predictedSpeed), borderColor: '#94a3b8', pointRadius: 0, borderWidth: 1, borderDash: [4, 4] }] : []),
      { label: 'Передняя тележка', data: values(point => point.frontBogieSpeed), borderColor: '#0d9488', pointRadius: 0, borderWidth: 1.5 },
      { label: 'GPS', data: values(point => point.gpsSpeed), borderColor: '#d97706', pointRadius: 3, showLine: false },
    ] } });
    const positionChart = new Chart(positionCanvas.current, { type: 'line', options, data: { datasets: [
      { label: 'Модель', data: values((_, index) => distances[index]), borderColor: '#2563eb', pointRadius: 0, borderWidth: 2 },
      { label: 'По тележкам', data: values((_, index) => referenceDistances[index]), borderColor: '#0d9488', pointRadius: 0, borderWidth: 1.5, borderDash: [4, 4] },
    ] } });
    return () => { speedChart.destroy(); positionChart.destroy(); };
  }, [loading, telemetry, scenario, outputs, currentIndex, distances, referenceDistances, overrides, baselineOutputs]);
  function changeSignals(patch: Partial<EditableSignals>) {
    if (saving) return;
    setIsPlaying(false); setSaved(null); setAdjustedSpeed(''); setAdjustedPosition('');
    setOverrides(previous => ({ ...previous, [currentPoint.sequenceNumber]: { ...previous[currentPoint.sequenceNumber], ...patch } }));
  }
  function resetSignals(all = false) {
    setIsPlaying(false); setSaved(null); setManualValid(true); setEditVersion(value => value + 1);
    setAdjustedSpeed(''); setAdjustedPosition('');
    setOverrides(previous => { if (all) return {}; const next = { ...previous }; delete next[currentPoint.sequenceNumber]; return next; });
  }
  function seek(index: number) {
    setIsPlaying(false); setCurrentIndex(Math.max(0, Math.min(telemetry.length - 1, index)));
    setSaved(null); setAdjustedSpeed(''); setAdjustedPosition('');
    setManualValid(true);
  }
  async function saveResult(intervene = false) {
    if (!id || !currentPoint || !output || saveLock.current || recalculating || !manualValid) return;
    const speed = Number(adjustedSpeed), distance = Number(adjustedPosition);
    if (intervene && (adjustedSpeed.trim() === '' || adjustedPosition.trim() === '' || !Number.isFinite(speed) || !Number.isFinite(distance) || speed < 0 || distance < 0)) { showToast('error', 'Введите неотрицательные скорость и положение.'); return; }
    const requestGeneration = generation.current;
    saveLock.current = true; setSaving(true); setIsPlaying(false);
    try {
      let inference = saved?.index === currentIndex ? saved.inference : await api.runInference({ tramRunId: id, sequenceNumber: currentPoint.sequenceNumber, input: toModelInput(currentPoint), scenarioInputs: Object.keys(overrides).length ? scenario.slice(0, currentIndex + 1).map(toModelInput) : undefined });
      if (requestGeneration !== generation.current) return;
      setSaved({ index: currentIndex, inference });
      if (intervene) inference = await api.userIntervention(inference.id, { adjustedSpeed: speed, adjustedPosition: distance });
      if (requestGeneration !== generation.current) return;
      setSaved({ index: currentIndex, inference });
      setInferences(previous => [...previous.filter(item => item.id !== inference.id), inference]);
      showToast('success', intervene ? 'Корректировка сохранена в истории' : 'Результат сохранён в истории');
    } catch { if (requestGeneration === generation.current) showToast('error', 'Не удалось сохранить результат. Попробуйте ещё раз.'); }
    finally { saveLock.current = false; setSaving(false); }
  }
  if (loading) return <div className="card state-panel" role="status"><span className="spinner" /><h2>Подготовка прогона</h2><p>Загружаем телеметрию и рассчитываем модель…</p></div>;
  if (loadError || !run) return <div className="card state-panel" role="alert"><h2>Не удалось открыть прогон</h2><p>Проверьте подключение и доступ к данным.</p><button className="btn btn-primary" onClick={() => setRetry(value => value + 1)}>Повторить</button><Link to="/runs">К списку прогонов</Link></div>;
  if (!telemetry.length) return <div className="card state-panel"><h2>Нет телеметрии</h2><p>Для воспроизведения нужны точки этого прогона.</p><Link className="btn btn-primary" to={'/runs/' + id}>Открыть прогон</Link></div>;
  return <div className="model-page">
    <div className="page-header"><div><Link className="back-link" to={'/runs/' + id}>← К прогону</Link><p className="eyebrow">Анализ движения · демонстрационная модель</p><h1 className="page-title">{run.name}</h1><p className="page-subtitle">Телеметрия, маршрут и оценка движения на одной временной шкале</p></div><span className="badge badge-success">{isPlaying ? 'Воспроизведение' : 'Готов к анализу'}</span></div>
    <section className="card playback-card" aria-label="Управление воспроизведением"><div className="section-heading"><h2>Управление воспроизведением</h2><span className="text-muted">{number(currentPoint.timestamp, 1)} / {number(telemetry[telemetry.length - 1].timestamp, 1)} с</span></div>
      <div className="playback-controls"><div className="transport-buttons"><button className="btn btn-outline icon-button" onClick={() => seek(currentIndex - 1)} disabled={!currentIndex || saving} aria-label="Предыдущий шаг">‹</button><button className="btn btn-primary play-button" disabled={saving || recalculating || !manualValid} onClick={() => { if (currentIndex === telemetry.length - 1) seek(0); setIsPlaying(value => !value); }} aria-label={isPlaying ? 'Пауза' : 'Воспроизведение'}>{isPlaying ? 'Ⅱ Пауза' : '▶ Смотреть'}</button><button className="btn btn-outline icon-button" onClick={() => seek(currentIndex + 1)} disabled={currentIndex === telemetry.length - 1 || saving} aria-label="Следующий шаг">›</button></div>
      <div className="timeline"><label htmlFor="timeline">Шаг: {currentIndex + 1} / {telemetry.length}</label><input id="timeline" aria-label="Текущий шаг" type="range" min={0} max={telemetry.length - 1} value={currentIndex} disabled={saving} onChange={event => seek(Number(event.target.value))} /></div><label className="speed-select">Скорость<select aria-label="Скорость воспроизведения" value={playSpeed} onChange={event => setPlaySpeed(Number(event.target.value))} className="form-control"><option value={0.5}>0,5×</option><option value={1}>1×</option><option value={2}>2×</option><option value={5}>5×</option></select></label><button className="btn btn-outline" disabled={saving} onClick={() => seek(0)}>Сброс</button></div><p className="helper-text">Просмотр не создаёт записи в истории. Сохраните нужный результат отдельно.</p>
    </section>
    <div className="drive-banner"><div><strong>Хотите управлять движением?</strong><p>В тренажёре ручка удерживается, есть инерция и зимнее сцепление. Телеметрия записывается в БД автоматически.</p></div><Link className="btn btn-primary" to={'/runs/' + id + '/drive'}>Управлять трамваем →</Link></div>
    <ManualControls key={currentIndex + ':' + editVersion} point={currentPoint} disabled={saving} changed={!!overrides[currentPoint.sequenceNumber]} onChange={changeSignals} onValidity={setManualValid} onReset={() => resetSignals()} />
    <div className="scenario-status" role="status"><span>{predictionError ? 'Не удалось пересчитать сценарий.' : recalculating ? 'Пересчитываем модель…' : Object.keys(overrides).length ? 'Сценарий рассчитан · изменено шагов: ' + Object.keys(overrides).length : 'Показаны исходные данные прогона'}</span><div>{predictionError && <button className="btn btn-outline btn-sm" onClick={() => setPredictionRetry(value => value + 1)}>Повторить расчёт</button>}{Object.keys(overrides).length > 0 && <button className="btn btn-outline btn-sm" disabled={saving} onClick={() => resetSignals(true)}>Сбросить все правки</button>}</div></div>
    <div className="model-grid"><div className="analysis-column"><div className="charts-grid"><section className="card"><div className="card-header"><h2>Скорость <span className="unit">м/с</span></h2></div><div className="chart-body"><canvas ref={speedCanvas} aria-label="График скорости" /></div></section><section className="card"><div className="card-header"><h2>Пройденный путь <span className="unit">м</span></h2></div><div className="chart-body"><canvas ref={positionCanvas} aria-label="График положения" /></div></section></div><section className="card"><div className="card-header"><h2>Маршрут и положение трамвая</h2></div><div className="route-map-panel"><RouteMap distanceMeters={distances[currentIndex]} telemetry={scenario} currentPoint={currentPoint} plannedRoute={run.plannedRoute} /></div></section></div>
    <aside className="metrics-column"><section className="card"><div className="card-header"><h2>Текущий шаг <span className="unit">{currentIndex + 1}</span></h2></div><div className="card-body"><div className="metrics-grid">
      <div className="metric-card"><span className="metric-label">Скорость модели</span><strong className="metric-value">{number(output?.predictedSpeed)} <small>м/с</small></strong></div><div className="metric-card"><span className="metric-label">Пройденный путь</span><strong className="metric-value">{number(distances[currentIndex], 1)} <small>м</small></strong></div><div className="metric-card"><span className="metric-label">Ручка контроллера</span><strong className="metric-value">{number(currentPoint.controllerHandlePosition)}</strong></div><div className="metric-card"><span className="metric-label">Скорость GPS</span><strong className="metric-value" data-testid="gps-speed">{number(gpsSpeed)}{gpsSpeed != null && <small> м/с</small>}</strong></div><div className="metric-card"><span className="metric-label">Ошибка скорости</span><strong className="metric-value">{number(speedError)}{speedError != null && <small> м/с</small>}</strong></div><div className="metric-card"><span className="metric-label">Задняя тележка</span><strong className="metric-value">{number(currentPoint.rearBogieSpeed)} <small>м/с</small></strong></div></div>
      {gpsSpeed == null && <p className="helper-text gps-status">GPS недоступен на этом шаге. Оценка модели и движение по маршруту продолжаются.</p>}<button className="btn btn-primary w-100" disabled={saving || recalculating || !manualValid || saved?.index === currentIndex} onClick={() => void saveResult()}>{saving ? 'Сохраняем…' : saved?.index === currentIndex ? '✓ Результат сохранён' : 'Сохранить результат'}</button>
      <details className="intervention-panel"><summary>Корректировка оператора</summary><p className="helper-text">Укажите уточнённые значения для выбранного шага.</p><label className="form-label" htmlFor="adjusted-speed">Скорость, м/с</label><input id="adjusted-speed" className="form-control" type="number" min="0" step="0.01" disabled={saving} value={adjustedSpeed} onChange={event => setAdjustedSpeed(event.target.value)} /><label className="form-label" htmlFor="adjusted-position">Пройденный путь, м</label><input id="adjusted-position" className="form-control" type="number" min="0" step="0.1" disabled={saving} value={adjustedPosition} onChange={event => setAdjustedPosition(event.target.value)} /><button className="btn btn-outline w-100" disabled={saving || recalculating || !manualValid} onClick={() => void saveResult(true)}>Сохранить корректировку</button></details>
    </div></section><section className="card"><div className="card-header"><h2>Сохранённые результаты <span className="unit">{inferences.length}</span></h2></div><div className="card-body"><p className="helper-text">История сохраняется между сеансами.</p><Link to="/history" className="back-link">Открыть историю →</Link></div></section></aside></div>
  </div>;
}
