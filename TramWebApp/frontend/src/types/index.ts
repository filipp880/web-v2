// src/types/index.ts

export interface TramRun {
  plannedRoute?: [number, number][];
  id: string;
  name: string;
  description: string;
  createdAt: string;
  updatedAt: string | null;
  createdByUserId: string;
  isActive: boolean;
  telemetryPointsCount: number;
  inferencesCount: number;
}

export interface TramRunListItem {
  id: string;
  name: string;
  description: string;
  createdAt: string;
  isActive: boolean;
  telemetryPointsCount: number;
  inferencesCount: number;
}

export interface CreateTramRunDto {
  name: string;
  description: string;
}

export interface UpdateTramRunDto {
  name: string;
  description: string;
}

export interface TramTelemetryPoint {
  distanceMeters?: number | null;
  id: string;
  tramRunId: string;
  sequenceNumber: number;
  timestamp: number;
  controllerHandlePosition: number;
  frontBogieSpeed: number;
  rearBogieSpeed: number;
  gpsLatitude: number | null;
  gpsLongitude: number | null;
  gpsSpeed: number | null;
}

export interface ModelInput {
  controllerHandlePosition: number;
  frontBogieSpeed: number;
  rearBogieSpeed: number;
  timestamp: number;
  gpsLatitude?: number;
  gpsLongitude?: number;
  gpsSpeed?: number;
}

export interface ModelOutput {
  predictedSpeed: number;
  predictedPosition: number;
  timestamp: number;
  confidence: number;
}

export interface MetricsSnapshot {
  currentSpeed: number;
  currentPosition: number;
  predictedSpeed: number;
  predictedPosition: number;
  actualSpeed: number;
  actualPosition: number;
  speedError: number;
  positionError: number;
  controllerPosition: number;
  frontBogieSpeed: number;
  rearBogieSpeed: number;
  isUserIntervention: boolean;
  timestamp: string;
  sequenceNumber: number;
}

export interface ModelInference {
  id: string;
  tramRunId: string;
  userId: string | null;
  inferenceTime: string;
  inputControllerPosition: number;
  inputFrontSpeed: number;
  inputRearSpeed: number;
  predictedSpeed: number;
  predictedPosition: number;
  actualSpeed: number | null;
  actualPosition: number | null;
  userAdjustedSpeed: number | null;
  userAdjustedPosition: number | null;
  isUserIntervention: boolean;
  errorSpeed: number | null;
  errorPosition: number | null;
}

export interface ModelInferenceListItem {
  id: string;
  tramRunId: string;
  inferenceTime: string;
  predictedSpeed: number;
  predictedPosition: number;
  isUserIntervention: boolean;
  errorSpeed: number | null;
  errorPosition: number | null;
}

export interface InferenceRequest {
  scenarioInputs?: ModelInput[];
  tramRunId: string;
  sequenceNumber: number;
  input: ModelInput;
  userAdjustedSpeed?: number;
  userAdjustedPosition?: number;
}

export interface User {
  id: string;
  username: string;
  email: string;
  role: 'Anonymous' | 'User' | 'Admin';
  createdAt: string;
  lastLoginAt: string | null;
  isActive: boolean;
}

export interface UserProfile {
  id: string;
  username: string;
  email: string;
  role: 'Anonymous' | 'User' | 'Admin';
  createdAt: string;
  lastLoginAt: string | null;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  user: User;
}

export interface ModelStatus {
  isLoaded: boolean;
  version: string;
}

export interface UserInterventionRequest {
  adjustedSpeed: number;
  adjustedPosition: number;
}

export interface ActualValuesRequest {
  actualSpeed: number;
  actualPosition: number;
}

export interface MetricsCalculationRequest {
  input: ModelInput;
  output: ModelOutput;
  actualSpeed?: number;
  actualPosition?: number;
}
