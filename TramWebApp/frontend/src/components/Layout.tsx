// src/components/Layout.tsx

import React, { ReactNode } from 'react';
import { Outlet, Link, useLocation, NavLink } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export function Layout() {
  const { user, logout } = useAuth();
  const location = useLocation();

  const navItems = [
    { path: '/runs', label: 'Прогоны', roles: ['User', 'Admin'] as const },
    { path: '/model', label: 'Модель', roles: ['User', 'Admin'] as const },
    { path: '/history', label: 'История', roles: ['User', 'Admin'] as const },
  ];

  const filteredNavItems = navItems.filter(item => 
    user && item.roles.includes(user.role)
  );

  return (
    <div className="layout">
      <header className="header">
        <div className="header-content">
          <Link to="/runs" className="header-brand">
            <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
              <rect width="32" height="32" rx="8" fill="#2c5aa0"/>
              <path d="M8 16L14 22L24 10" stroke="white" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            <span>TramWebApp</span>
          </Link>
          
          <nav className="header-nav" aria-label="Main navigation">
            <ul className="nav-list">
              {filteredNavItems.map(item => (
                <li key={item.path}>
                  <NavLink 
                    to={item.path} 
                    className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
                    end={item.path === '/runs'}
                  >
                    {item.label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>

          <div className="header-actions">
            {user ? (
              <div className="user-menu">
                <span className="user-info">
                  <span className="user-avatar" aria-hidden="true">
                    {user.username.charAt(0).toUpperCase()}
                  </span>
                  <span>{user.username}</span>
                  <span className={`badge badge-${user.role === 'Admin' ? 'warning' : 'info'}`}>
                    {user.role === 'Admin' ? 'Админ' : 'Оператор'}
                  </span>
                </span>
                <button onClick={logout} className="btn btn-outline btn-sm">
                  Выйти
                </button>
              </div>
            ) : (
              <div className="auth-buttons">
                <Link to="/login" className="btn btn-outline btn-sm">Войти</Link>
                <Link to="/register" className="btn btn-primary btn-sm">Регистрация</Link>
              </div>
            )}
          </div>
        </div>
      </header>

      <main className="main-content" role="main">
        <div className="container">
          <Outlet />
        </div>
      </main>

      <footer className="footer">
        <div className="container">
          <p>TramWebApp - Система мониторинга трамвая &copy; 2024</p>
        </div>
      </footer>
    </div>
  );
}