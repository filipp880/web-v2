// src/pages/RunsListPage.tsx

import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { useApi } from '../hooks/useApi';
import { Modal } from '../components/Modal';
import type { TramRunListItem, CreateTramRunDto, UpdateTramRunDto } from '../types';
import { useAuth } from '../contexts/AuthContext';

export function RunsListPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [runs, setRuns] = useState<TramRunListItem[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRun, setEditingRun] = useState<TramRunListItem | null>(null);
  const [formData, setFormData] = useState<CreateTramRunDto>({ name: '', description: '' });
  const [formErrors, setFormErrors] = useState<Partial<CreateTramRunDto>>({});

  const { execute: loadRuns, loading: loadingRuns } = useApi(
    () => api.getTramRuns(),
    { onSuccess: setRuns, errorMessage: 'Не удалось загрузить список прогонов' }
  );

  const { execute: createRun, loading: creating } = useApi(
    (data: CreateTramRunDto) => api.createTramRun(data),
    { successMessage: 'Прогон создан', onSuccess: () => { closeModal(); loadRuns(); } }
  );

  const { execute: updateRun, loading: updating } = useApi(
    (data: { id: string; dto: UpdateTramRunDto }) => api.updateTramRun(data.id, data.dto),
    { successMessage: 'Прогон обновлен', onSuccess: () => { closeModal(); loadRuns(); } }
  );

  const { execute: deleteRun, loading: deleting } = useApi(
    (id: string) => api.deleteTramRun(id),
    { successMessage: 'Прогон удален', onSuccess: loadRuns }
  );

  useEffect(() => { loadRuns(); }, [loadRuns]);

  const openCreateModal = () => {
    setEditingRun(null);
    setFormData({ name: '', description: '' });
    setFormErrors({});
    setIsModalOpen(true);
  };

  const openEditModal = (run: TramRunListItem) => {
    setEditingRun(run);
    setFormData({ name: run.name, description: run.description });
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingRun(null);
    setFormErrors({});
  };

  const validate = () => {
    const newErrors: Partial<CreateTramRunDto> = {};
    if (!formData.name.trim()) newErrors.name = 'Введите название';
    else if (formData.name.length > 100) newErrors.name = 'Максимум 100 символов';
    if (formData.description.length > 500) newErrors.description = 'Максимум 500 символов';
    setFormErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    if (editingRun) {
      await updateRun({ id: editingRun.id, dto: formData });
    } else {
      await createRun(formData);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Удалить этот прогон? Это действие нельзя отменить.')) {
      await deleteRun(id);
    }
  };

  const handleViewDetails = (id: string) => {
    navigate(`/runs/${id}`);
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Прогоны трамвая</h1>
          <p className="page-subtitle">Управление тестовыми прогонами и телеметрией</p>
        </div>
        <button className="btn btn-primary" onClick={openCreateModal} disabled={loadingRuns}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <rect x="7" y="1" width="2" height="14" rx="1" fill="currentColor"/>
            <rect x="1" y="7" width="14" height="2" rx="1" fill="currentColor"/>
          </svg>
          Добавить прогон
        </button>
      </div>

      {loadingRuns && runs.length === 0 && (
        <div className="card">
          <div className="card-body" style={{ textAlign: 'center' }}>
            <span className="spinner" style={{ width: '2rem', height: '2rem', borderWidth: '3px' }}></span>
            <p style={{ marginTop: '1rem', color: '#6c757d' }}>Загрузка прогонов...</p>
          </div>
        </div>
      )}

      {!loadingRuns && runs.length === 0 && (
        <div className="card">
          <div className="empty-state">
            <div className="empty-state-icon">
              <svg width="64" height="64" viewBox="0 0 64 64" fill="none" aria-hidden="true">
                <rect x="8" y="16" width="48" height="32" rx="4" stroke="currentColor" strokeWidth="2"/>
                <path d="M20 24h24M20 32h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
              </svg>
            </div>
            <h3 className="empty-state-title">Прогоны не найдены</h3>
            <p className="empty-state-text">Создайте первый тестовый прогон для начала работы</p>
            <button className="btn btn-primary" style={{ marginTop: '1rem' }} onClick={openCreateModal}>
              Создать прогон
            </button>
          </div>
        </div>
      )}

      {!loadingRuns && runs.length > 0 && (
        <div className="card">
          <div className="table-container">
            <table className="table table-striped" role="table">
              <thead>
                <tr>
                  <th scope="col">Название</th>
                  <th scope="col">Описание</th>
                  <th scope="col">Создан</th>
                  <th scope="col">Статус</th>
                  <th scope="col">Точек телеметрии</th>
                  <th scope="col">Инференсов</th>
                  <th scope="col">Действия</th>
                </tr>
              </thead>
              <tbody>
                {runs.map(run => (
                  <tr key={run.id}>
                    <td>
                      <strong>{run.name}</strong>
                    </td>
                    <td style={{ maxWidth: '300px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {run.description || '—'}
                    </td>
                    <td>{new Date(run.createdAt).toLocaleString('ru-RU')}</td>
                    <td>
                      <span className={`badge ${run.isActive ? 'badge-success' : 'badge-secondary'}`}>
                        {run.isActive ? 'Активен' : 'Неактивен'}
                      </span>
                    </td>
                    <td>{run.telemetryPointsCount}</td>
                    <td>{run.inferencesCount}</td>
                    <td>
                      <div style={{ display: 'flex', gap: '0.375rem' }}>
                        <button
                          className="btn btn-outline btn-sm"
                          onClick={() => handleViewDetails(run.id)}
                          aria-label={`Просмотреть ${run.name}`}
                        >
                          <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
                            <path d="M7 1C3.686 1 1 3.686 1 7s2.686 6 6 6 6-2.686 6-6-2.686-6-6-6zm0 11c-2.757 0-5-2.243-5-5s2.243-5 5-5 5 2.243 5 5-2.243 5-5 5zm0-8a3 3 0 110 6 3 3 0 010-6z" fill="currentColor"/>
                          </svg>
                        </button>
                        <button
                          className="btn btn-outline btn-sm"
                          onClick={() => openEditModal(run)}
                          aria-label={`Редактировать ${run.name}`}
                        >
                          <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
                            <path d="M11.5 2.5L13 4l-8 8H2v-3.5l8-8zM2 11.5h3.5l8-8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                          </svg>
                        </button>
                        {user?.role === 'Admin' && (
                          <button
                            className="btn btn-danger btn-sm"
                            onClick={() => handleDelete(run.id)}
                            aria-label={`Удалить ${run.name}`}
                          >
                            <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
                              <path d="M3 3h8v8H3zM7 1v1M3 1h8M10 1v1" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                              <path d="M5 5v6M9 5v6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                            </svg>
                          </button>
                        )}
                      </div>
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
        onClose={closeModal}
        title={editingRun ? 'Редактировать прогон' : 'Создать прогон'}
        footer={
          <>
            <button className="btn btn-outline" onClick={closeModal} disabled={creating || updating}>
              Отмена
            </button>
            <button 
              className="btn btn-primary" 
              onClick={handleSubmit} 
              disabled={creating || updating}
            >
              {creating || updating ? (
                <>
                  <span className="spinner" aria-hidden="true"></span>
                  Сохранение...
                </>
              ) : (
                editingRun ? 'Сохранить' : 'Создать'
              )}
            </button>
          </>
        }
      >
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="name" className="form-label">Название *</label>
            <input
              type="text"
              id="name"
              name="name"
              className="form-control"
              value={formData.name}
              onChange={e => setFormData(prev => ({ ...prev, name: e.target.value }))}
              disabled={creating || updating}
              aria-invalid={!!formErrors.name}
              aria-describedby={formErrors.name ? 'name-error' : undefined}
            />
            {formErrors.name && <div id="name-error" className="form-error" role="alert">{formErrors.name}</div>}
          </div>

          <div className="form-group">
            <label htmlFor="description" className="form-label">Описание</label>
            <textarea
              id="description"
              name="description"
              className="form-control"
              value={formData.description}
              onChange={e => setFormData(prev => ({ ...prev, description: e.target.value }))}
              disabled={creating || updating}
              rows={3}
              aria-invalid={!!formErrors.description}
              aria-describedby={formErrors.description ? 'description-error' : undefined}
            />
            {formErrors.description && <div id="description-error" className="form-error" role="alert">{formErrors.description}</div>}
          </div>
        </form>
      </Modal>
    </div>
  );
}