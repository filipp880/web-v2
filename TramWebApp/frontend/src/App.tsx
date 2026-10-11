// src/App.tsx

import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ToastProvider } from './contexts/ToastContext';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { RunsListPage } from './pages/RunsListPage';
import { RunDetailPage } from './pages/RunDetailPage';
import { ModelPage } from './pages/ModelPage';
import { HistoryPage } from './pages/HistoryPage';
import { useAuth } from './contexts/AuthContext';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth();
  
  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '50vh' }}>
        <span className="spinner" style={{ width: '2rem', height: '2rem', borderWidth: '3px' }}></span>
      </div>
    );
  }
  
  return user ? <>{children}</> : <Navigate to="/login" replace />;
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth();
  
  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '50vh' }}>
        <span className="spinner" style={{ width: '2rem', height: '2rem', borderWidth: '3px' }}></span>
      </div>
    );
  }
  
  return user ? <Navigate to="/runs" replace /> : <>{children}</>;
}

function App() {
  return (
    <ToastProvider>
      <Routes>
        <Route path="/login" element={
          <PublicRoute>
            <LoginPage />
          </PublicRoute>
        } />
        <Route path="/register" element={
          <PublicRoute>
            <RegisterPage />
          </PublicRoute>
        } />
        
        <Route element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }>
          <Route path="/runs" element={<RunsListPage />} />
          <Route path="/runs/:id" element={<RunDetailPage />} />
          <Route path="/runs/:id/model" element={<ModelPage />} />
          <Route path="/history" element={<HistoryPage />} />
        </Route>
        
        <Route path="/" element={<Navigate to="/runs" replace />} />
        <Route path="*" element={<Navigate to="/runs" replace />} />
      </Routes>
    </ToastProvider>
  );
}

export default App;