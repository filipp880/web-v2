import { useState } from 'react';
import type { TramTelemetryPoint } from '../types';
import { formatNumber } from '../utils/telemetry';

export type EditableSignals = Pick<TramTelemetryPoint, 'controllerHandlePosition' | 'frontBogieSpeed' | 'rearBogieSpeed' | 'gpsSpeed'>;

export function ManualControls({ point, disabled, changed, onChange, onValidity, onReset }: {
  point: TramTelemetryPoint; disabled: boolean; changed: boolean;
  onChange: (patch: Partial<EditableSignals>) => void; onValidity: (valid: boolean) => void; onReset: () => void;
}) {
  const [draft, setDraft] = useState<Record<keyof EditableSignals, string>>({
    controllerHandlePosition: String(point.controllerHandlePosition), frontBogieSpeed: String(point.frontBogieSpeed),
    rearBogieSpeed: String(point.rearBogieSpeed), gpsSpeed: point.gpsSpeed == null ? '' : String(point.gpsSpeed),
  });
  const validField = (name: keyof EditableSignals, value: string) => name === 'gpsSpeed' && value === '' || value.trim() !== '' && Number.isFinite(Number(value)) && Number(value) >= (name === 'controllerHandlePosition' ? -1 : 0) && Number(value) <= (name === 'controllerHandlePosition' ? 1 : 100);
  const invalid = Object.entries(draft).some(([name, value]) => !validField(name as keyof EditableSignals, value));
  function change(name: keyof EditableSignals, value: string) {
    const next = { ...draft, [name]: value }; setDraft(next);
    onValidity(Object.entries(next).every(([field, text]) => validField(field as keyof EditableSignals, text)));
    if (validField(name, value)) onChange({ [name]: value === '' ? null : Number(value) });
  }
  const handle = Number(draft.controllerHandlePosition) || 0;
  return <section className="card manual-card" aria-label="Ручное управление">
    <div className="section-heading"><div><h2>Редактор записанных сигналов</h2><p className="helper-text">Изменение одной точки записи: шаг {point.sequenceNumber + 1}. Для непрерывного движения откройте тренажёр выше.</p></div><span className={'badge ' + (changed ? 'badge-info' : 'badge-secondary')}>{changed ? 'Шаг изменён' : 'Исходные данные'}</span></div>
    <div className="manual-grid"><div className="controller-console">
      <div className="controller-display"><span>Ручка контроллера</span><strong>{formatNumber(handle)}</strong><span>{handle < 0 ? 'Торможение' : handle > 0 ? 'Тяга' : 'Нейтраль'}</span></div>
      <input className="controller-slider" aria-label="Ручка контроллера" type="range" min="-1" max="1" step="0.01" value={Math.max(-1, Math.min(1, handle))} disabled={disabled} onChange={event => change('controllerHandlePosition', event.target.value)} />
      <div className="controller-scale"><span>−1 · Тормоз</span><span>0</span><span>+1 · Тяга</span></div>
      <div className="controller-presets">{[[-1, 'Тормоз'], [0, 'Нейтраль'], [1, 'Тяга']].map(([value, label]) => <button key={value} className="btn btn-outline btn-sm" disabled={disabled} onClick={() => change('controllerHandlePosition', String(value))}>{label}</button>)}</div>
    </div><div className="signal-inputs">
      {([['controllerHandlePosition', 'Положение ручки', -1, 1], ['frontBogieSpeed', 'Передняя тележка, м/с', 0, 100], ['rearBogieSpeed', 'Задняя тележка, м/с', 0, 100], ['gpsSpeed', 'GPS, м/с', 0, 100]] as const).map(([name, label, min, max]) => <div key={name}><label className="form-label" htmlFor={'signal-' + name}>{label}</label><input id={'signal-' + name} className="form-control" type="number" min={min} max={max} step="0.01" value={draft[name]} disabled={disabled} placeholder={name === 'gpsSpeed' ? 'Нет сигнала' : undefined} aria-invalid={!validField(name, draft[name])} onChange={event => change(name, event.target.value)} /></div>)}
      <p className="helper-text">Скорости: 0–100 м/с. Пустое поле GPS означает отсутствие сигнала. Модель оценивает скорость по датчикам: при нулевых скоростях тележек одна ручка не разгоняет трамвай.</p>
      {invalid && <p className="signal-error" role="alert">Проверьте диапазоны значений. Сохранение доступно после исправления.</p>}
    </div></div>
    <div className="manual-footer"><span className="helper-text">Правки сохраняются при переключении шагов в этой сессии. Исходная телеметрия остаётся в базе.</span><button className="btn btn-outline btn-sm" disabled={disabled || !changed && !invalid} onClick={onReset}>Вернуть данные шага</button></div>
  </section>;
}
