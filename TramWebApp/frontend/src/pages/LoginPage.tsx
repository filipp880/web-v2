// src/pages/LoginPage.tsx

import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useApi } from '../hooks/useApi';
import type { LoginRequest } from '../types';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [formData, setFormData] = useState<LoginRequest>({ username: '', password: '' });
  const [errors, setErrors] = useState<Partial<LoginRequest>>({});

  const { execute, loading } = useApi(
    (data: LoginRequest) => login(data),
    {
      successMessage: 'Успешный вход!',
      onSuccess: () => navigate('/runs'),
    }
  );

  const validate = () => {
    const newErrors: Partial<LoginRequest> = {};
    if (!formData.username) newErrors.username = 'Введите имя пользователя';
    if (!formData.password) newErrors.password = 'Введите пароль';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) {
      await execute(formData);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
    if (errors[e.target.name as keyof LoginRequest]) {
      setErrors(prev => ({ ...prev, [e.target.name]: undefined }));
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card card">
        <div className="card-body">
          <h1 className="auth-title">Вход в систему</h1>
          <p className="auth-subtitle">Введите свои учетные данные для доступа к TramWebApp</p>
          
          <form onSubmit={handleSubmit} noValidate>
            <div className="form-group">
              <label htmlFor="username" className="form-label">Имя пользователя</label>
              <input
                type="text"
                id="username"
                name="username"
                className="form-control"
                value={formData.username}
                onChange={handleChange}
                disabled={loading}
                autoComplete="username"
                aria-invalid={!!errors.username}
                aria-describedby={errors.username ? 'username-error' : undefined}
              />
              {errors.username && (
                <div id="username-error" className="form-error" role="alert">{errors.username}</div>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="password" className="form-label">Пароль</label>
              <input
                type="password"
                id="password"
                name="password"
                className="form-control"
                value={formData.password}
                onChange={handleChange}
                disabled={loading}
                autoComplete="current-password"
                aria-invalid={!!errors.password}
                aria-describedby={errors.password ? 'password-error' : undefined}
              />
              {errors.password && (
                <div id="password-error" className="form-error" role="alert">{errors.password}</div>
              )}
            </div>

            <button type="submit" className="btn btn-primary w-100" disabled={loading}>
              {loading ? (
                <>
                  <span className="spinner" aria-hidden="true"></span>
                  Вход...
                </>
              ) : (
                'Войти'
              )}
            </button>
          </form>

          <p className="auth-footer">
            Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
          </p>

          <div className="demo-credentials">
            <h4>Демо-аккаунты:</h4>
            <ul>
              <li><strong>admin</strong> / admin123 — Администратор</li>
              <li><strong>operator</strong> / user123 — Оператор</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
