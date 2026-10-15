import { Component, type ErrorInfo, type ReactNode } from 'react';
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  componentDidCatch(error: Error, info: ErrorInfo) { console.error('Interface error', error, info.componentStack); }
  render() {
    if (this.state.failed) return <main className="state-panel"><h1>Не удалось отобразить страницу</h1><p>Ваши сохранённые данные доступны после обновления страницы.</p><button className="btn btn-primary" onClick={() => window.location.reload()}>Обновить страницу</button><a href="/runs">Открыть прогоны</a></main>;
    return this.props.children;
  }
}
