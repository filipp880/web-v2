import { useEffect, useMemo, useState } from 'react';
import { MapContainer, TileLayer, Polyline, CircleMarker, GeoJSON, Tooltip, useMap } from 'react-leaflet';
import { latLngBounds, type LatLngTuple } from 'leaflet';
import type { GeoJsonObject } from 'geojson';
import type { TramTelemetryPoint } from '../types';
import { integrateTelemetry } from '../utils/telemetry';
import 'leaflet/dist/leaflet.css';
import { RouteDiagram } from './RouteDiagram';

const key = import.meta.env.VITE_2GIS_API_KEY;
function distance(a: LatLngTuple, b: LatLngTuple) { return Math.hypot((a[0] - b[0]) * 111195, (a[1] - b[1]) * 111195 * Math.cos(a[0] * Math.PI / 180)); }
function alongRoute(route: LatLngTuple[], meters: number): { position: LatLngTuple; passed: LatLngTuple[] } {
  const passed: LatLngTuple[] = [route[0]];
  for (let index = 1; index < route.length; index++) {
    const length = distance(route[index - 1], route[index]);
    if (meters <= length && length > 0) {
      const fraction = Math.max(0, meters / length);
      const position: LatLngTuple = [route[index - 1][0] + fraction * (route[index][0] - route[index - 1][0]), route[index - 1][1] + fraction * (route[index][1] - route[index - 1][1])];
      return { position, passed: [...passed, position] };
    }
    meters -= length; passed.push(route[index]);
  }
  return { position: route[route.length - 1], passed };
}
function MapViewport({ positions }: { positions: LatLngTuple[] }) {
  const map = useMap();
  useEffect(() => { map.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>'); }, [map]);
  useEffect(() => { map.fitBounds(latLngBounds(positions), { padding: [35, 35], maxZoom: 17 }); }, [map, positions]);
  useEffect(() => {
    const observer = new ResizeObserver(() => map.invalidateSize()); observer.observe(map.getContainer());
    return () => observer.disconnect();
  }, [map]);
  return null;
}
export function RouteMap({ telemetry, currentPoint, plannedRoute, distanceMeters }: { telemetry: TramTelemetryPoint[]; currentPoint?: TramTelemetryPoint; plannedRoute?: LatLngTuple[]; distanceMeters?: number }) {
  const [base, setBase] = useState<GeoJsonObject | null>(null);
  const [provider, setProvider] = useState(key ? '2gis' : 'local');
  const [tileError, setTileError] = useState(false);
  const [view, setView] = useState<'scheme' | 'map'>('scheme');
  useEffect(() => { let alive = true; void fetch('/maps/moscow.geojson').then(response => { if (!response.ok) throw new Error('Map unavailable'); return response.json(); }).then(data => { if (alive) setBase(data); }).catch(() => {}); return () => { alive = false; }; }, []);
  const gps = useMemo(() => telemetry.filter(point => Number.isFinite(point.gpsLatitude) && Number.isFinite(point.gpsLongitude)).map(point => [point.gpsLatitude!, point.gpsLongitude!] as LatLngTuple), [telemetry]);
  const route = useMemo(() => plannedRoute?.filter(point => Number.isFinite(point[0]) && Number.isFinite(point[1])) ?? [], [plannedRoute]);
  const bounds = route.length ? route : gps;
  const cumulative = useMemo(() => integrateTelemetry(telemetry), [telemetry]);
  const index = currentPoint ? telemetry.findIndex(point => point.sequenceNumber === currentPoint.sequenceNumber) : -1;
  const state = route.length ? alongRoute(route, distanceMeters ?? cumulative[Math.max(0, index)] ?? 0) : null;
  const currentGps: LatLngTuple | null = currentPoint?.gpsLatitude != null && currentPoint.gpsLongitude != null ? [currentPoint.gpsLatitude, currentPoint.gpsLongitude] : null;
  // Without a planned path, retain the last valid fix instead of inventing a road route.
  const lastFix = index >= 0 ? telemetry.slice(0, index + 1).reverse().find(point => point.gpsLatitude != null && point.gpsLongitude != null) : undefined;
  const current = state?.position ?? currentGps ?? (lastFix ? [lastFix.gpsLatitude!, lastFix.gpsLongitude!] as LatLngTuple : null);
  const total = useMemo(() => route.slice(1).reduce((sum, point, index) => sum + distance(route[index], point), 0), [route]);
  if (!bounds.length) return <div className="state-panel"><p>Нет маршрута или координат GPS.</p></div>;
  return <div className="route-map">
    <div className="map-toolbar"><div className="map-view-switch"><button className="btn btn-sm btn-outline" aria-pressed={view === 'scheme'} disabled={!route.length} onClick={() => setView('scheme')}>Схема</button><button className="btn btn-sm btn-outline" aria-pressed={view === 'map'} onClick={() => setView('map')}>Карта</button></div><span>{provider === '2gis' ? '2ГИС · карта' : 'Москва · автономная карта'}</span><div>{key && <select aria-label="Подложка карты" value={provider} onChange={event => { setProvider(event.target.value); setTileError(false); }}><option value="local">Автономная карта</option><option value="2gis">2ГИС</option></select>}<span className="badge badge-info">{currentPoint ? state ? 'По заданному пути' : currentGps ? 'GPS' : 'Последний GPS' : 'Обзор трассы'}</span></div></div>
    {tileError && <div className="map-notice" role="status">Подложка 2ГИС недоступна. Показана автономная карта.</div>}
    {view === 'scheme' && route.length > 0 ? <RouteDiagram route={route} passed={currentPoint ? state?.passed ?? [] : []} position={currentPoint ? current : null} meters={distanceMeters ?? cumulative[Math.max(0, index)] ?? 0} total={total} /> : <MapContainer center={bounds[0]} zoom={15} scrollWheelZoom={false} preferCanvas style={{ height: '100%', width: '100%' }}>
      <MapViewport positions={bounds} />
      {(provider === 'local' || tileError) && base && <GeoJSON data={base} attribution='Данные © <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> · ODbL' style={feature => {
        const kind = feature?.properties?.kind;
        return kind === 'building' ? { color: '#cbd5d9', fillColor: '#dce3e6', fillOpacity: 0.7, weight: 0.5 } : kind === 'water' ? { color: '#a5d7e8', fillColor: '#b9e1ef', fillOpacity: 0.9, weight: 1 } : kind === 'park' ? { color: '#b7d7b9', fillColor: '#d8e9d5', fillOpacity: 0.8, weight: 1 } : kind === 'tram' ? { color: '#99a5b0', weight: 2, dashArray: '3 3' } : { color: '#ffffff', weight: 7, opacity: 1 };
      }} onEachFeature={(feature, layer) => { if (feature.properties?.name) { const label = document.createElement('span'); label.textContent = feature.properties.name; layer.bindTooltip(label, { sticky: true }); } }} />}
      {provider === '2gis' && !tileError && <TileLayer url={'https://tile0.maps.2gis.com/v2/tiles/online_sd/{z}/{x}/{y}.png?key=' + encodeURIComponent(key)} attribution='© 2ГИС' eventHandlers={{ tileerror: () => setTileError(true) }} />}
      {route.length > 0 && <Polyline positions={route} pathOptions={{ color: '#ffffff', weight: 10 }} />}
      {route.length > 0 && <Polyline positions={route} pathOptions={{ color: '#475569', weight: 5, opacity: 0.9 }} />}
      {state && currentPoint && <Polyline positions={state.passed} pathOptions={{ color: '#2563eb', weight: 5 }} />}
      {gps.map((point, idx) => <CircleMarker key={idx} center={point} radius={3} pathOptions={{ color: '#0d9488', weight: 1, fillOpacity: 0.9 }} />)}
      <CircleMarker center={bounds[0]} radius={6} pathOptions={{ color: '#ffffff', fillColor: '#0d9488', fillOpacity: 1, weight: 2 }}><Tooltip>Начало маршрута</Tooltip></CircleMarker>
      {currentPoint && current && <CircleMarker center={current} radius={9} pathOptions={{ color: '#ffffff', fillColor: '#2563eb', fillOpacity: 1, weight: 3 }}><Tooltip permanent direction="top">Трамвай · шаг {index + 1}</Tooltip></CircleMarker>}
    </MapContainer>}
    <div className="map-legend"><span><i className="legend-route" />Заданный путь</span><span><i className="legend-gps" />Измерения GPS</span>{currentPoint && <span><i className="legend-tram" />Текущее положение</span>}</div>
  </div>;
}
