// src/pages/ModelPage.tsx

import React, { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { useApi } from '../hooks/useApi';
import type { TramRun, TramTelemetryPoint, ModelInput, ModelOutput, MetricsSnapshot, ModelInferenceListItem, InferenceRequest } from '../types';
import { Chart, registerables, ChartConfiguration } from 'chart.js';
import 'chartjs-adapter-date-fns';

Chart.register(...registerables);

export function ModelPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [run, setRun] = useState<TramRun | null>(null);
  const [telemetry, setTelemetry] = useState<TramTelemetryPoint[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [playSpeed, setPlaySpeed] = useState(1);
  const [modelOutput, setModelOutput] = useState<ModelOutput | null>(null);
  const [metrics, setMetrics] = useState<MetricsSnapshot | null>(null);
  const [inferences, setInferences] = useState<ModelInferenceListItem[]>([]);
  const [userAdjustedSpeed, setUserAdjustedSpeed] = useState<number | null>(null);
  const [userAdjustedPosition, setUserAdjustedPosition] = useState<number | null>(null);
  const [showIntervention, setShowIntervention] = useState(false);
  const [lastInferenceId, setLastInferenceId] = useState<string | null>(null);
  
  const speedChartRef = useRef<HTMLCanvasElement>(null);
  const positionChartRef = useRef<HTMLCanvasElement>(null);
  const mapRef = useRef<HTMLDivElement>(null);
  const animationRef = useRef<number>();

  const { execute: loadRun, loading: loadingRun } = useApi(
    () => api.getTramRun(id!),
    { onSuccess: setRun, errorMessage: 'Не удалось загрузить прогон' }
  );

  const { execute: loadTelemetry, loading: loadingTelemetry } = useApi(
    () => api.getTelemetry(id!),
    { onSuccess: setTelemetry, errorMessage: 'Не удалось загрузить телеметрию' }
  );

  const { execute: loadInferences, loading: loadingInferences } = useApi(
    () => api.getInferencesByRun(id!),
    { onSuccess: setInferences }
  );

  const { execute: runInference } = useApi(
    (request: InferenceRequest) => api.runInference(request),
    { 
      onSuccess: (inference) => {
        setLastInferenceId(inference.id);
        setShowIntervention(true);
      },
      errorMessage: 'Ошибка запуска модели'
    }
  );

  const { execute: submitIntervention } = useApi(
    (data: { id: string; speed: number; position: number }) => 
      api.userIntervention(data.id, { adjustedSpeed: data.speed, adjustedPosition: data.position }),
    { 
      successMessage: 'Вмешательство записано',
      onSuccess: () => { setShowIntervention(false); loadInferences(); }
    }
  );

  useEffect(() => {
    if (id) {
      loadRun();
      loadTelemetry();
      loadInferences();
    }
  }, [id, loadRun, loadTelemetry, loadInferences]);

  const currentPoint = telemetry[currentIndex];

  const handlePredict = async () => {
    if (!currentPoint) return;
    
    const input: ModelInput = {
      controllerHandlePosition: currentPoint.controllerHandlePosition,
      frontBogieSpeed: currentPoint.frontBogieSpeed,
      rearBogieSpeed: currentPoint.rearBogieSpeed,
      timestamp: currentPoint.timestamp,
      gpsLatitude: currentPoint.gpsLatitude ?? undefined,
      gpsLongitude: currentPoint.gpsLongitude ?? undefined,
      gpsSpeed: currentPoint.gpsSpeed ?? undefined,
    };

    const request: InferenceRequest = {
      tramRunId: id!,
      sequenceNumber: currentPoint.sequenceNumber,
      input,
    };

    await runInference(request);
  };

  const handleInterventionSubmit = () => {
    if (!lastInferenceId || userAdjustedSpeed === null || userAdjustedPosition === null) return;
    submitIntervention({ id: lastInferenceId, speed: userAdjustedSpeed, position: userAdjustedPosition });
    setUserAdjustedSpeed(null);
    setUserAdjustedPosition(null);
  };

  const togglePlay = () => {
    if (isPlaying) {
      setIsPlaying(false);
      if (animationRef.current) cancelAnimationFrame(animationRef.current);
    } else {
      setIsPlaying(true);
      animate();
    }
  };

  const animate = () => {
    if (!isPlaying || currentIndex >= telemetry.length - 1) {
      setIsPlaying(false);
      return;
    }
    
    setCurrentIndex(prev => prev + 1);
    const delay = 1000 / playSpeed;
    animationRef.current = setTimeout(animate, delay);
  };

  const stepForward = () => {
    if (currentIndex < telemetry.length - 1) {
      setCurrentIndex(prev => prev + 1);
    }
  };

  const stepBackward = () => {
    if (currentIndex > 0) {
      setCurrentIndex(prev => prev - 1);
    }
  };

  const reset = () => {
    setIsPlaying(false);
    if (animationRef.current) cancelAnimationFrame(animationRef.current);
    setCurrentIndex(0);
    setModelOutput(null);
    setMetrics(null);
    setLastInferenceId(null);
    setShowIntervention(false);
  };

  useEffect(() => {
    if (currentPoint) {
      handlePredict();
    }
    return () => {
      if (animationRef.current) cancelAnimationFrame(animationRef.current);
    };
  }, [currentIndex]);

  return (
    <div>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <button className="btn btn-outline" onClick={() => navigate(`/runs/${id}`)} aria-label="Назад">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M10 4L6 8l4 4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            К прогонам
          </button>
          <div>
            <h1 className="page-title">Модель: {run?.name || 'Загрузка...'}</h1>
            <p className="page-subtitle">Интерактивное тестирование модели оценки скорости и положения</p>
          </div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 400px', gap: '1.5rem' }}>
        <div>
          <div className="card" style={{ marginBottom: '1.5rem' }}>
            <div className="card-header">
              <h3>Управление воспроизведением</h3>
            </div>
            <div className="card-body">
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap' }}>
                <button className="btn btn-outline" onClick={stepBackward} disabled={currentIndex === 0} aria-label="Предыдущий шаг">
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                    <path d="M10 4L6 8l4 4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    <rect x="9" y="4" width="2" height="8" fill="currentColor"/>
                  </svg>
                </button>
                
                <button className={`btn ${isPlaying ? 'btn-warning' : 'btn-success'}`} onClick={togglePlay} aria-label={isPlaying ? 'Пауза' : 'Воспроизведение'}>
                  {isPlaying ? (
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                      <rect x="4" y="3" width="3" height="10" rx="0.5"/>
                      <rect x="9" y="3" width="3" height="10" rx="0.5"/>
                    </svg>
                  ) : (
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                      <path d="M5 3v10l8 5V3z"/>
                    </svg>
                  )}
                </button>
                
                <button className="btn btn-outline" onClick={stepForward} disabled={currentIndex >= telemetry.length - 1} aria-label="Следующий шаг">
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                    <path d="M6 4l4 4-4 4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    <rect x="5" y="4" width="2" height="8" fill="currentColor"/>
                  </svg>
                </button>

                <div style={{ flex: 1, minWidth: '150px' }}>
                  <label className="form-label" style={{ marginBottom: '0.25rem', display: 'block' }}>
                    Шаг: {currentIndex + 1} / {telemetry.length}
                  </label>
                  <input
                    type="range"
                    min="0"
                    max={telemetry.length - 1}
                    value={currentIndex}
                    onChange={e => { setCurrentIndex(Number(e.target.value)); setIsPlaying(false); }}
                    className="form-control"
                    style={{ cursor: 'pointer' }}
                    aria-label="Текущий шаг"
                  />
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <span style={{ fontSize: '0.875rem', color: '#6c757d' }}>Скорость:</span>
                  <select value={playSpeed} onChange={e => setPlaySpeed(Number(e.target.value))} className="form-control" style={{ width: 'auto' }}>
                    <option value={0.5}>0.5x</option>
                    <option value={1}>1x</option>
                    <option value={2}>2x</option>
                    <option value={5}>5x</option>
                  </select>
                </div>

                <button className="btn btn-outline" onClick={reset} disabled={loadingRun}>
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                    <path d="M13.5 2.5a8 8 0 11-8 8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                    <path d="M2.5 2.5v5h5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                  </svg>
                  Сброс
                </button>
              </div>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
            <div className="card">
              <div className="card-header">
                <h3>График скорости (м/с)</h3>
              </div>
              <div className="card-body" style={{ height: '300px' }}>
                <canvas ref={speedChartRef} aria-label="График скорости"></canvas>
              </div>
            </div>

            <div className="card">
              <div className="card-header">
                <h3>График положения (м)</h3>
              </div>
              <div className="card-body" style={{ height: '300px' }}>
                <canvas ref={positionChartRef} aria-label="График положения"></canvas>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h3>2D Карта трассы</h3>
            </div>
            <div className="card-body" style={{ height: '350px' }}>
              <div ref={mapRef} style={{ width: '100%', height: '100%', borderRadius: '8px', background: '#f8f9fa', border: '1px dashed var(--border-color)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#6c757d' }}>
                Мини-карта (Leaflet интеграция)
              </div>
            </div>
          </div>
        </div>

        <div>
          <div className="card" style={{ marginBottom: '1.5rem' }}>
            <div className="card-header">
              <h3>Текущие метрики</h3>
            </div>
            <div className="card-body">
              {currentPoint && (
                <div className="metrics-grid">
                  <div className="metric-card">
                    <span className="metric-label">Ручка контроллера</span>
                    <span className="metric-value">{currentPoint.controllerHandlePosition.toFixed(2)}</span>
                    <div className="metric-bar">
                      <div className="metric-bar-fill" style={{ width: `${((currentPoint.controllerHandlePosition + 1) / 2) * 100}%` }}></div>
                    </div>
                  </div>
                  
                  <div className="metric-card">
                    <span className="metric-label">Пред. скорость</span>
                    <span className="metric-value text-primary">{modelOutput?.predictedSpeed.toFixed(2) ?? '—'} м/с</span>
                  </div>
                  
                  <div className="metric-card">
                    <span className="metric-label">Пред. положение</span>
                    <span className="metric-value text-primary">{modelOutput?.predictedPosition.toFixed(1) ?? '—'} м</span>
                  </div>

                  {currentPoint.gpsSpeed !== null && (
                    <div className="metric-card">
                      <span className="metric-label">Эталонная скорость</span>
                      <span className="metric-value text-success">{currentPoint.gpsSpeed.toFixed(2)} м/с</span>
                    </div>
                  )}

                  {metrics && (
                    <>
                      <div className="metric-card">
                        <span className="metric-label">Ошибка скорости</span>
                        <span className="metric-value text-danger">{metrics.speedError.toFixed(2)} м/с</span>
                      </div>
                      <div className="metric-card">
                        <span className="metric-label">Ошибка положения</span>
                        <span className="metric-value text-danger">{metrics.positionError.toFixed(1)} м</span>
                      </div>
                    </>
                  )}
                </div>
              )}

              {showIntervention && lastInferenceId && (
                <div className="intervention-panel" style={{ marginTop: '1.5rem', padding: '1rem', background: '#fff3cd', border: '1px solid #ffc107', borderRadius: '8px' }}>
                  <h4 style={{ marginBottom: '1rem', color: '#856404' }}>Вмешательство оператора</h4>
                  <p style={{ marginBottom: '1rem', fontSize: '0.875rem', color: '#664d03' }}>
                    Модель предсказала: {modelOutput?.predictedSpeed.toFixed(2)} м/с, {modelOutput?.predictedPosition.toFixed(1)} м.
                    Введите скорректированные значения при необходимости.
                  </p>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                    <div className="form-group">
                      <label className="form-label">Скорр. скорость (м/с)</label>
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        className="form-control"
                        value={userAdjustedSpeed ?? ''}
                        onChange={e => setUserAdjustedSpeed(e.target.value ? parseFloat(e.target.value) : null)}
                        placeholder={modelOutput?.predictedSpeed.toFixed(2)}
                      />
                    </div>
                    <div className="form-group">
                      <label className="form-label">Скорр. положение (м)</label>
                      <input
                        type="number"
                        step="0.1"
                        min="0"
                        className="form-control"
                        value={userAdjustedPosition ?? ''}
                        onChange={e => setUserAdjustedPosition(e.target.value ? parseFloat(e.target.value) : null)}
                        placeholder={modelOutput?.predictedPosition.toFixed(1)}
                      />
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
                    <button className="btn btn-primary" onClick={handleInterventionSubmit} disabled={userAdjustedSpeed === null || userAdjustedPosition === null}>
                      Сохранить вмешательство
                    </button>
                    <button className="btn btn-outline" onClick={() => setShowIntervention(false)}>
                      Отмена
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h3>История инференсов</h3>
            </div>
            <div className="card-body">
              {loadingInferences ? (
                <div style={{ textAlign: 'center', padding: '2rem' }}>
                  <span className="spinner"></span>
                </div>
              ) : inferences.length === 0 ? (
                <div className="empty-state" style={{ padding: '2rem' }}>
                  <p>История пуста. Запустите модель для записи инференсов.</p>
                </div>
              ) : (
                <div className="table-container" style={{ maxHeight: '300px', overflow: 'auto' }}>
                  <table className="table table-striped" style={{ fontSize: '0.8125rem' }}>
                    <thead>
                      <tr>
                        <th scope="col">Время</th>
                        <th scope="col">Пред. скорость</th>
                        <th scope="col">Пред. положение</th>
                        <th scope="col">Ошибка скорости</th>
                        <th scope="col">Вмешательство</th>
                      </tr>
                    </thead>
                    <tbody>
                      {inferences.slice().reverse().slice(0, 20).map(inf => (
                        <tr key={inf.id} style={{ backgroundColor: inf.isUserIntervention ? '#fff3cd' : '' }}>
                          <td>{new Date(inf.inferenceTime).toLocaleTimeString('ru-RU')}</td>
                          <td>{inf.predictedSpeed.toFixed(2)}</td>
                          <td>{inf.predictedPosition.toFixed(1)}</td>
                          <td>{inf.errorSpeed?.toFixed(2) ?? '—'}</td>
                          <td>
                            {inf.isUserIntervention && (
                              <span className="badge badge-warning">Да</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}