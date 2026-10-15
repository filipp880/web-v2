import type { ModelInput, ModelOutput, TramTelemetryPoint } from '../types';
export const formatNumber = (value: number | null | undefined, digits = 2) => typeof value === 'number' && Number.isFinite(value) ? value.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits }) : '—';
export function toModelInput(point: TramTelemetryPoint): ModelInput {
  return { controllerHandlePosition: point.controllerHandlePosition, frontBogieSpeed: point.frontBogieSpeed, rearBogieSpeed: point.rearBogieSpeed, timestamp: point.timestamp, gpsLatitude: point.gpsLatitude ?? undefined, gpsLongitude: point.gpsLongitude ?? undefined, gpsSpeed: point.gpsSpeed ?? undefined };
}
export function integrateTelemetry(points: TramTelemetryPoint[], outputs?: ModelOutput[]): number[] {
  let distance = 0;
  return points.map((point, index) => {
    if (index > 0) {
      const previous = points[index - 1];
      const before = outputs?.[index - 1]?.predictedSpeed ?? (previous.frontBogieSpeed + previous.rearBogieSpeed) / 2;
      const after = outputs?.[index]?.predictedSpeed ?? (point.frontBogieSpeed + point.rearBogieSpeed) / 2;
      distance += (before + after) / 2 * Math.max(0, point.timestamp - previous.timestamp);
    }
    return distance;
  });
}
