// src/pages/RunDetailPage.tsx

import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { api } from '../services/api';
import { useApi } from '../hooks/useApi';
import type { TramRun, TramTelemetryPoint } from '../types';
import { format } from 'date-fns';

export function RunDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [run, setRun] = useState<TramRun | null>(null);
  const [telemetry, setTelemetry] = useState<TramTelemetryPoint[]>([]);
  const [activeTab, setActiveTab] = useState<'info' | 'telemetry' | 'map'>('info');

  const { execute: loadRun, loading: loadingRun } = useApi(
    () => api.getTramRun(id!),
    { onSuccess: setRun, errorMessage: 'Не удалось загрузить прогон' }
  );

  const { execute: loadTelemetry, loading: loadingTelemetry } = useApi(
    () => api.getTelemetry(id!),
    { onSuccess: setTelemetry, errorMessage: 'Не удалось загрузить телеметрию' }
  );

  useEffect(() => {
    if (id) {
      loadRun();
      loadTelemetry();
    }
  }, [id, loadRun, loadTelemetry]);

  const handleBack = () => navigate('/runs');

  if (!id || (loadingRun && !run)) {
    return (
      <div className="card">
        <div className="card-body" style={{ textAlign: 'center' }}>
          <span className="spinner" style={{ width: '2rem', height: '2rem', borderWidth: '3px' }}></span>
          <p style={{ marginTop: '1rem', color: '#6c757d' }}>Загрузка...</p>
        </div>
      </div>
    );
  }

  if (!run) {
    return (
      <div className="card">
        <div className="empty-state">
          <h3 className="empty-state-title">Прогон не найден</h3>
          <button className="btn btn-primary" onClick={handleBack}>Назад к списку</button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <button className="btn btn-outline" onClick={handleBack} aria-label="Назад">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M10 4L6 8l4 4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            Назад
          </button>
          <div>
            <h1 className="page-title">{run.name}</h1>
            <p className="page-subtitle">{run.description || 'Без описания'}</p>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <Link to={`/runs/${run.id}/model`} className="btn btn-primary">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <rect x="2" y="3" width="12" height="10" rx="2" stroke="currentColor" strokeWidth="1.5"/>
              <path d="M5 8h6M8 5v6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            Запустить модель
          </Link>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <div className="card-header">
          <ul className="nav-tabs" role="tablist">
            <li role="presentation">
              <button
                role="tab"
                aria-selected={activeTab === 'info'}
                onClick={() => setActiveTab('info')}
                className={`nav-tab ${activeTab === 'info' ? 'active' : ''}`}
              >
                Информация
              </button>
            </li>
            <li role="presentation">
              <button
                role="tab"
                aria-selected={activeTab === 'telemetry'}
                onClick={() => setActiveTab('telemetry')}
                className={`nav-tab ${activeTab === 'telemetry' ? 'active' : ''}`}
              >
                Телеметрия ({telemetry.length})
              </button>
            </li>
            <li role="presentation">
              <button
                role="tab"
                aria-selected={activeTab === 'map'}
                onClick={() => setActiveTab('map')}
                className={`nav-tab ${activeTab === 'map' ? 'active' : ''}`}
              >
                Карта
              </button>
            </li>
          </ul>
        </div>
        <div className="card-body">
          {activeTab === 'info' && (
            <div className="run-info">
              <div className="info-grid">
                <div className="info-item">
                  <span className="info-label">ID</span>
                  <span className="info-value font-mono">{run.id}</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Статус</span>
                  <span className="info-value">
                    <span className={`badge ${run.isActive ? 'badge-success' : 'badge-secondary'}`}>
                      {run.isActive ? 'Активен' : 'Неактивен'}
                    </span>
                  </span>
                </div>
                <div className="info-item">
                  <span className="info-label">Создан</span>
                  <span className="info-value">{format(new Date(run.createdAt), 'PPpp', { locale: require('date-fns/locale/ru') })}</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Обновлен</span>
                  <span className="info-value">{run.updatedAt ? format(new Date(run.updatedAt), 'PPpp', { locale: require('date-fns/locale/ru') }) : '—'}</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Точек телеметрии</span>
                  <span className="info-value">{run.telemetryPointsCount}</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Инференсов</span>
                  <span className="info-value">{run.inferencesCount}</span>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'telemetry' && (
            <div>
              {loadingTelemetry ? (
                <div style={{ textAlign: 'center', padding: '2rem' }}>
                  <span className="spinner"></span>
                  <p style={{ marginTop: '0.5rem', color: '#6c757d' }}>Загрузка телеметрии...</p>
                </div>
              ) : telemetry.length === 0 ? (
                <div className="empty-state">
                  <div className="empty-state-icon">📊</div>
                  <h3 className="empty-state-title">Нет данных телеметрии</h3>
                  <p className="empty-state-text">Добавьте точки телеметрии для этого прогона</p>
                </div>
              ) : (
                <div className="table-container">
                  <table className="table table-striped" role="table">
                    <thead>
                      <tr>
                        <th scope="col">#</th>
                        <th scope="col">Время (с)</th>
                        <th scope="col">Ручка контроллера</th>
                        <th scope="col">Скорость передней</th>
                        <th scope="col">Скорость задней</th>
                        <th scope="col">GPS Широта</th>
                        <th scope="col">GPS Долгота</th>
                        <th scope="col">GPS Скорость</th>
                      </tr>
                    </thead>
                    <tbody>
                      {telemetry.slice(0, 100).map(point => (
                        <tr key={point.id}>
                          <td>{point.sequenceNumber}</td>
                          <td>{point.timestamp.toFixed(1)}</td>
                          <td>{point.controllerHandlePosition.toFixed(2)}</td>
                          <td>{point.frontBogieSpeed.toFixed(2)} м/с</td>
                          <td>{point.rearBogieSpeed.toFixed(2)} м/с</td>
                          <td>{point.gpsLatitude?.toFixed(6) ?? '—'}</td>
                          <td>{point.gpsLongitude?.toFixed(6) ?? '—'}</td>
                          <td>{point.gpsSpeed?.toFixed(2) ?? '—'} м/с</td>
                        </tr>
                      ))}
                      {telemetry.length > 100 && (
                        <tr>
                          <td colSpan={8} style={{ textAlign: 'center', color: '#6c757d' }}>
                            Показано 100 из {telemetry.length} записей
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {activeTab === 'map' && (
            <div>
              <div className="map-container" style={{ height: '400px', background: '#f0f0f0', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#6c757d' }}>
                <p>Карта трассы (требуется интеграция с Leaflet/MapLibre)</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}