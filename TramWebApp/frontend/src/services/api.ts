// src/services/api.ts

import axios, { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import type { LoginResponse, RegisterRequest, LoginRequest } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

class ApiService {
  private client: AxiosInstance;
  private accessToken: string | null = null;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 30000,
    });

    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        if (this.accessToken && config.headers) {
          config.headers.Authorization = `Bearer ${this.accessToken}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401 && error.config?.url !== '/auth/login') {
          this.logout();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );

    const storedToken = localStorage.getItem('accessToken');
    if (storedToken) {
      this.setToken(storedToken);
    }
  }

  setToken(token: string) {
    this.accessToken = token;
    localStorage.setItem('accessToken', token);
  }

  clearToken() {
    this.accessToken = null;
    localStorage.removeItem('accessToken');
  }

  getToken(): string | null {
    return this.accessToken;
  }

  isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  logout() {
    this.clearToken();
  }

  // Auth
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await this.client.post<LoginResponse>('/auth/login', credentials);
    this.setToken(response.data.accessToken);
    return response.data;
  }

  async register(data: RegisterRequest): Promise<LoginResponse['user']> {
    const response = await this.client.post<LoginResponse['user']>('/auth/register', data);
    return response.data;
  }

  async getProfile(): Promise<LoginResponse['user']> {
    const response = await this.client.get<LoginResponse['user']>('/auth/me');
    return response.data;
  }

  // Tram Runs
  async getTramRuns(): Promise<import('../types').TramRunListItem[]> {
    const response = await this.client.get<import('../types').TramRunListItem[]>('/tram-runs');
    return response.data;
  }

  async getTramRun(id: string): Promise<import('../types').TramRun> {
    const response = await this.client.get<import('../types').TramRun>(`/tram-runs/${id}`);
    return response.data;
  }

  async createTramRun(data: import('../types').CreateTramRunDto): Promise<import('../types').TramRun> {
    const response = await this.client.post<import('../types').TramRun>('/tram-runs', data);
    return response.data;
  }

  async updateTramRun(id: string, data: import('../types').UpdateTramRunDto): Promise<import('../types').TramRun> {
    const response = await this.client.put<import('../types').TramRun>(`/tram-runs/${id}`, data);
    return response.data;
  }

  async deleteTramRun(id: string): Promise<void> {
    await this.client.delete(`/tram-runs/${id}`);
  }

  // Telemetry
  async getStorageInfo(): Promise<{ mode: 'PostgreSQL' | 'InMemory'; runCapacity: number | null; persistent: boolean }> {
    return (await this.client.get('/system/storage')).data;
  }
  async startSimulation(sourceId: string, initialSpeedKmh: number, winter: boolean, initialHandle = 0): Promise<{ runId: string }> {
    return (await this.client.post('/simulation/' + sourceId + '/start', { initialSpeedKmh, winter, initialHandle })).data;
  }

  async simulationStep(id: string, handle: number, sequenceNumber: number): Promise<{ pointId: string; sequenceNumber: number; speed: number; distance: number; timestamp: number }> {
    return (await this.client.post('/simulation/' + id + '/step', { handle, sequenceNumber })).data;
  }

  async getTelemetry(tramRunId: string): Promise<import('../types').TramTelemetryPoint[]> {
    const response = await this.client.get<import('../types').TramTelemetryPoint[]>(`/tram-runs/${tramRunId}/telemetry`);
    return response.data;
  }

  async getTelemetryPoint(tramRunId: string, sequenceNumber: number): Promise<import('../types').TramTelemetryPoint> {
    const response = await this.client.get<import('../types').TramTelemetryPoint>(`/tram-runs/${tramRunId}/telemetry/${sequenceNumber}`);
    return response.data;
  }

  async createTelemetryPoint(tramRunId: string, data: import('../types').TramTelemetryPoint): Promise<import('../types').TramTelemetryPoint> {
    const response = await this.client.post<import('../types').TramTelemetryPoint>(`/tram-runs/${tramRunId}/telemetry`, data);
    return response.data;
  }

  async createTelemetryBatch(tramRunId: string, data: import('../types').TramTelemetryPoint[]): Promise<import('../types').TramTelemetryPoint[]> {
    const response = await this.client.post<import('../types').TramTelemetryPoint[]>(`/tram-runs/${tramRunId}/telemetry/batch`, data);
    return response.data;
  }

  // Model
  async predict(input: import('../types').ModelInput): Promise<import('../types').ModelOutput> {
    const response = await this.client.post<import('../types').ModelOutput>('/model/predict', input);
    return response.data;
  }

  async predictBatch(inputs: import('../types').ModelInput[]): Promise<import('../types').ModelOutput[]> {
    const response = await this.client.post<import('../types').ModelOutput[]>('/model/predict/batch', inputs);
    return response.data;
  }

  async calculateMetrics(request: import('../types').MetricsCalculationRequest): Promise<import('../types').MetricsSnapshot> {
    const response = await this.client.post<import('../types').MetricsSnapshot>('/model/metrics', request);
    return response.data;
  }

  async runInference(request: import('../types').InferenceRequest): Promise<import('../types').ModelInference> {
    const response = await this.client.post<import('../types').ModelInference>('/model/inference', request);
    return response.data;
  }

  async getInferencesByRun(tramRunId: string): Promise<import('../types').ModelInferenceListItem[]> {
    const response = await this.client.get<import('../types').ModelInferenceListItem[]>(`/model/inferences/run/${tramRunId}`);
    return response.data;
  }

  async getMyInferences(): Promise<import('../types').ModelInferenceListItem[]> {
    const response = await this.client.get<import('../types').ModelInferenceListItem[]>('/model/inferences/user/me');
    return response.data;
  }

  async getInference(id: string): Promise<import('../types').ModelInference> {
    const response = await this.client.get<import('../types').ModelInference>(`/model/inferences/${id}`);
    return response.data;
  }

  async userIntervention(id: string, data: import('../types').UserInterventionRequest): Promise<import('../types').ModelInference> {
    const response = await this.client.put<import('../types').ModelInference>(`/model/inferences/${id}/intervene`, data);
    return response.data;
  }

  async setActualValues(id: string, data: import('../types').ActualValuesRequest): Promise<import('../types').ModelInference> {
    const response = await this.client.put<import('../types').ModelInference>(`/model/inferences/${id}/actual`, data);
    return response.data;
  }

  async getModelStatus(): Promise<import('../types').ModelStatus> {
    const response = await this.client.get<import('../types').ModelStatus>('/model/status');
    return response.data;
  }
}

export const api = new ApiService();
