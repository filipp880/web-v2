// src/pages/HistoryPage.tsx

import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { useApi } from '../hooks/useApi';
import { Modal } from '../components/Modal';
import type { ModelInferenceListItem, ModelInference } from '../types';
import { format } from 'date-fns';

export function HistoryPage() {
  const navigate = useNavigate();
  const [inferences, setInferences] = useState<ModelInferenceListItem[]>([]);
  const [selectedInference, setSelectedInference] = useState<ModelInference | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const { execute: loadInferences, loading } = useApi(
    () => api.getMyInferences(),
    { onSuccess: setInferences, errorMessage: 'Не удалось загрузить историю' }
  );

  useEffect(() => { loadInferences(); }, [loadInferences]);

  const handleViewDetails = async (inferenceId: string) => {
    try {
      const inference = await api.getInference(inferenceId);
      setSelectedInference(inference);
      setIsModalOpen(true);
    } catch {
      // error handled by api interceptor
    }
  };

  const getRoleBadge = (role: string) => {
    switch (role) {
      case 'Admin': return 'badge-warning';
      case 'User': return 'badge-info';
      default: return 'badge-secondary';
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">История инференсов</h1>
          <p className="page-subtitle">Просмотр результатов работы модели</p>
        </div>
      </div>

      {loading && inferences.length === 0 && (
        <div className="card">
          <div className="card-body" style={{ textAlign: 'center' }}>
            <span className="spinner" style={{ width: '2rem', height: '2rem', borderWidth: '3px' }}></span>
            <p style={{ marginTop: '1rem', color: '#6c757d' }}>Загрузка истории...</p>
          </div>
        </div>
      )}

      {!loading && inferences.length === 0 && (
        <div className="card">
          <div className="empty-state">
            <div className="empty-state-icon">📋</div>
            <h3 className="empty-state-title">История пуста</h3>
            <p className="empty-state-text">Запустите модель на странице прогона для записи инференсов</p>
          </div>
        </div>
      )}

      {!loading && inferences.length > 0 && (
        <div className="card">
          <div className="table-container">
            <table className="table table-striped" role="table">
              <thead>
                <tr>
                  <th scope="col">ID</th>
                  <th scope="col">Прогон</th>
                  <th scope="col">Время</th>
                  <th scope="col">Пред. скорость</th>
                  <th scope="col">Пред. положение</th>
                  <th scope="col">Ошибка скорости</th>
                  <th scope="col">Ошибка положения</th>
                  <th scope="col">Вмешательство</th>
                  <th scope="col">Действия</th>
                </tr>
              </thead>
              <tbody>
                {inferences.map(inf => (
                  <tr key={inf.id} style={{ backgroundColor: inf.isUserIntervention ? '#fff3cd' : '' }}>
                    <td className="font-mono" style={{ fontSize: '0.75rem' }}>{inf.id.slice(0, 8)}...</td>
                    <td>{inf.tramRunId.slice(0, 8)}...</td>
                    <td>{format(new Date(inf.inferenceTime), 'PPpp', { locale: require('date-fns/locale/ru') })}</td>
                    <td>{inf.predictedSpeed.toFixed(2)} м/с</td>
                    <td>{inf.predictedPosition.toFixed(1)} м</td>
                    <td>{inf.errorSpeed?.toFixed(2) ?? '—'} м/с</td>
                    <td>{inf.errorPosition?.toFixed(1) ?? '—'} м</td>
                    <td>
                      {inf.isUserIntervention && (
                        <span className="badge badge-warning">Да</span>
                      )}
                    </td>
                    <td>
                      <button
                        className="btn btn-outline btn-sm"
                        onClick={() => handleViewDetails(inf.id)}
                        aria-label={`Просмотреть инференс ${inf.id.slice(0, 8)}`}
                      >
                        <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
                          <path d="M7 1C3.686 1 1 3.686 1 7s2.686 6 6 6 6-2.686 6-6-2.686-6-6-6zm0 11c-2.757 0-5-2.243-5-5s2.243-5 5-5 5 2.243 5 5-2.243 5-5 5zm0-8a3 3 0 110 6 3 3 0 010-6z" fill="currentColor"/>
                        </svg>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <Modal
        isOpen={isModalOpen}
        onClose={() => { setIsModalOpen(false); setSelectedInference(null); }}
        title="Детали инференса"
        size="lg"
      >
        {selectedInference && (
          <div>
            <div className="info-grid">
              <div className="info-item">
                <span className="info-label">ID</span>
                <span className="info-value font-mono">{selectedInference.id}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Прогон</span>
                <span className="info-value font-mono">{selectedInference.tramRunId}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Время</span>
                <span className="info-value">{format(new Date(selectedInference.inferenceTime), 'PPpp', { locale: require('date-fns/locale/ru') })}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Позиция ручки</span>
                <span className="info-value">{selectedInference.inputControllerPosition.toFixed(2)}</span>
              </div>
              <div className="info-item">
                <span className="info-label">Скорость передней</span>
                <span className="info-value">{selectedInference.inputFrontSpeed.toFixed(2)} м/с</span>
              </div>
              <div className="info-item">
                <span className="info-label">Скорость задней</span>
                <span className="info-value">{selectedInference.inputRearSpeed.toFixed(2)} м/с</span>
              </div>
              <div className="info-item">
                <span className="info-label">Пред. скорость</span>
                <span className="info-value text-primary">{selectedInference.predictedSpeed.toFixed(2)} м/с</span>
              </div>
              <div className="info-item">
                <span className="info-label">Пред. положение</span>
                <span className="info-value text-primary">{selectedInference.predictedPosition.toFixed(1)} м</span>
              </div>
              <div className="info-item">
                <span className="info-label">Эталонная скорость</span>
                <span className="info-value text-success">{selectedInference.actualSpeed?.toFixed(2) ?? '—'} м/с</span>
              </div>
              <div className="info-item">
                <span className="info-label">Эталонное положение</span>
                <span className="info-value text-success">{selectedInference.actualPosition?.toFixed(1) ?? '—'} м</span>
              </div>
              {selectedInference.isUserIntervention && (
                <>
                  <div className="info-item">
                    <span className="info-label">Вмешательство</span>
                    <span className="info-value"><span className="badge badge-warning">Да</span></span>
                  </div>
                  <div className="info-item">
                    <span className="info-label">Скорр. скорость</span>
                    <span className="info-value">{selectedInference.userAdjustedSpeed?.toFixed(2) ?? '—'} м/с</span>
                  </div>
                  <div className="info-item">
                    <span className="info-label">Скорр. положение</span>
                    <span className="info-value">{selectedInference.userAdjustedPosition?.toFixed(1) ?? '—'} м</span>
                  </div>
                </>
              )}
              <div className="info-item">
                <span className="info-label">Ошибка скорости</span>
                <span className="info-value text-danger">{selectedInference.errorSpeed?.toFixed(2) ?? '—'} м/с</span>
              </div>
              <div className="info-item">
                <span className="info-label">Ошибка положения</span>
                <span className="info-value text-danger">{selectedInference.errorPosition?.toFixed(1) ?? '—'} м</span>
              </div>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}