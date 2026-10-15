import type { LatLngTuple } from 'leaflet';

export function RouteDiagram({ route, passed, position, meters, total }: { route: LatLngTuple[]; passed: LatLngTuple[]; position?: LatLngTuple | null; meters: number; total: number }) {
  const centerLat = route[0][0] * Math.PI / 180;
  const xs = route.map(point => point[1] * Math.cos(centerLat)); const ys = route.map(point => -point[0]);
  const minX = Math.min(...xs), minY = Math.min(...ys);
  const extentX = Math.max(...xs) - minX, extentY = Math.max(...ys) - minY;
  const scale = Math.min(620 / (extentX || .001), 240 / (extentY || .001));
  const offsetX = (800 - extentX * scale) / 2, offsetY = (360 - extentY * scale) / 2;
  const project = (point: LatLngTuple) => [offsetX + (point[1] * Math.cos(centerLat) - minX) * scale, offsetY + (-point[0] - minY) * scale];
  const coordinates = (points: LatLngTuple[]) => points.map(point => project(point).join(',')).join(' ');
  const [sx, sy] = project(route[0]); const [ex, ey] = project(route[route.length - 1]);
  const train = position ? project(position) : null;
  return <div className="route-diagram"><svg viewBox="0 0 800 360" role="img" aria-label="Схема маршрута с положением трамвая">
    <defs><pattern id="diagram-grid" width="24" height="24" patternUnits="userSpaceOnUse"><path d="M24 0H0V24" fill="none" stroke="#e2e8f0" strokeWidth=".5" /></pattern></defs>
    <rect width="800" height="360" fill="#f8fafc" /><rect width="800" height="360" fill="url(#diagram-grid)" />
    <polyline points={coordinates(route)} fill="none" stroke="#fff" strokeWidth="20" strokeLinecap="round" strokeLinejoin="round" />
    <polyline points={coordinates(route)} fill="none" stroke="#94a3b8" strokeWidth="9" strokeLinecap="round" strokeLinejoin="round" />
    {passed.length > 1 && <polyline points={coordinates(passed)} fill="none" stroke="#2563eb" strokeWidth="9" strokeLinecap="round" strokeLinejoin="round" />}
    <circle cx={sx} cy={sy} r="9" fill="#059669" stroke="#fff" strokeWidth="3" /><text x={sx + 16} y={sy - 16} className="diagram-label">Старт</text>
    <circle cx={ex} cy={ey} r="9" fill="#ea580c" stroke="#fff" strokeWidth="3" /><text x={ex + 16} y={ey + 26} className="diagram-label">Финиш</text>
    {train && <g transform={'translate(' + train.join(',') + ')'}><circle r="23" fill="#dbeafe" stroke="#fff" strokeWidth="3" /><rect x="-11" y="-16" width="22" height="30" rx="6" fill="#1d4ed8" /><rect x="-7" y="-10" width="14" height="10" rx="2" fill="#bfdbfe" /><path d="M-7 10h14M-5 -21h10" stroke="#1e3a8a" strokeWidth="3" /><circle cx="-5" cy="7" r="2" fill="#fff" /><circle cx="5" cy="7" r="2" fill="#fff" /></g>}
    <text x="24" y="30" className="diagram-title">Заданный путь · движение от старта к финишу</text>
    <text x="24" y="338" className="diagram-label">{Math.round(Math.min(meters, total))} / {Math.round(total)} м</text>
    <text x="776" y="338" textAnchor="end" className="diagram-label">Север ↑</text>
  </svg><div className="route-progress"><span style={{ width: Math.min(100, meters / Math.max(1, total) * 100) + '%' }} /></div></div>;
}
