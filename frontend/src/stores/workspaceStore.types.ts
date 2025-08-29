import type { ImageDimensions } from "@/core/asset/asset.types";
import type { Timer } from "@/core/timing";
import type { Point } from "@/core/geometry/geometry.types";
import type { Tool, ToolName } from "@/core/workspace/tools.types";
import type { Annotation } from '@/core/workspace/annotation.types';
import type { LabelScheme } from '@/services/project/labelScheme/label.types';
import type { Asset } from '@/core/asset/asset.types';
import type { Task } from '@/services/project/task/task.types';
import type { WorkflowStageType } from '@/services/project/workflow/workflowStage.types';

/**
 * Current flat workspace store state interface (legacy structure)
 * This matches the actual current implementation in workspaceStore.ts
 */
export interface WorkspaceState {
    // Asset-related properties
    currentProjectId: string | null;
    currentAssetId: string | null;
    currentAssetData: Asset | null;
    currentImageUrl: string | null;
    imageNaturalDimensions: ImageDimensions | null;
    canvasDisplayDimensions: ImageDimensions | null;
    
    // Timer-related properties
    timerInstance: Timer;
    elapsedTimeDisplay: string;
    timerIntervalId: number | null;
    lastSavedWorkingTime: number;
    
    // UI-related properties
    viewOffset: Point;
    zoomLevel: number;
    activeTool: ToolName;
    availableTools: Tool[];
    isLoading: boolean;
    error: string | null;
    
    // Annotation-related properties
    annotations: Annotation[];
    currentLabelId: number | null;
    currentLabelScheme: LabelScheme | null;
    availableLabelSchemes: LabelScheme[];
    
    // Task-related properties
    currentTaskId: number | null;
    currentTaskData: Task | null;
    currentWorkflowStageType: WorkflowStageType | null;
    availableTasks: Task[];
    initialTaskId: number | null;
}

/**
 * Asset-related state in the workspace store
 */
export interface WorkspaceAssetState {
    currentProjectId: string | null;
    currentAssetId: string | null;
    currentAssetData: Asset | null;
    currentImageUrl: string | null;
    imageNaturalDimensions: ImageDimensions | null;
    canvasDisplayDimensions: ImageDimensions | null;
}

/**
 * Task-related state in the workspace store
 */
export interface WorkspaceTaskState {
    currentTaskId: number | null;
    currentTaskData: Task | null;
    currentWorkflowStageType: WorkflowStageType | null;
    availableTasks: Task[];
    initialTaskId: number | null;
}

/**
 * Timer-related state in the workspace store
 */
export interface WorkspaceTimerState {
    timerInstance: Timer;
    elapsedTimeDisplay: string;
    timerIntervalId: number | null;
    lastSavedWorkingTime: number;
}

/**
 * Annotation-related state in the workspace store
 */
export interface WorkspaceAnnotationState {
    annotations: Annotation[];
    currentLabelId: number | null;
    currentLabelScheme: LabelScheme | null;
    availableLabelSchemes: LabelScheme[];
}

/**
 * UI-related state in the workspace store
 */
export interface WorkspaceUIState {
    viewOffset: Point;
    zoomLevel: number;
    activeTool: ToolName;
    availableTools: Tool[];
    isLoading: boolean;
    error: string | null;
}

/**
 * Main workspace store state interface
 * Organized by domain concerns instead of flat structure
 */
export interface WorkspaceStoreState {
    asset: WorkspaceAssetState;
    task: WorkspaceTaskState;
    timer: WorkspaceTimerState;
    annotation: WorkspaceAnnotationState;
    ui: WorkspaceUIState;
}

/**
 * Parameters for loading a workspace
 */
export interface WorkspaceLoadParams {
    projectId: string;
    assetId: string;
    taskId?: string;
}

/**
 * Parameters for initializing a workspace
 */
export interface WorkspaceInitParams {
    projectId: string;
    assetId: string;
    taskId?: string;
    loadAnnotations?: boolean;
    loadTasks?: boolean;
    loadLabelSchemes?: boolean;
}

/**
 * Complete workspace loading result (includes asset, annotations, tasks, etc.)
 */
export type WorkspaceLoadResult = {
    success: true;
    asset: Asset;
    imageUrl: string | null;
    naturalDimensions: ImageDimensions | null;
    annotations: Annotation[];
    labelScheme: LabelScheme | null;
    tasks: Task[];
} | {
    success: false;
    error: string;
}

/**
 * Result of task operations (complete, suspend, defer, etc.)
 */
export type TaskResult = {
    success: true;
    task: Task;
} | {
    success: false;
    error: string;
}

/**
 * Navigation information for task navigation
 */
export interface NavigationInfo {
    projectId: string;
    assetId: string;
    taskId: string;
}

/**
 * Result of navigation operations
 */
export type NavigationResult = {
    success: true;
    nextTask: NavigationInfo;
} | {
    success: false;
    error: string;
}


/**
 * Result of workspace initialization
 */
export type WorkspaceInitResult = {
    success: true;
    assetData: Asset;
    taskData: Task | null;
    annotations: Annotation[];
    labelScheme: LabelScheme | null;
    availableTasks: Task[];
} | {
    success: false;
    error: string;
}

/**
 * Configuration for workspace operations
 */
export interface WorkspaceConfig {
    autoSaveInterval: number;
    maxZoom: number;
    minZoom: number;
    zoomSensitivity: number;
}

/**
 * Workspace operation context
 */
export interface WorkspaceOperationContext {
    projectId: string;
    assetId?: string;
    taskId?: number;
    userId?: string;
}

/**
 * Workspace events that can be emitted
 */
export enum WorkspaceEvent {
    ASSET_LOADED = 'asset_loaded',
    TASK_CHANGED = 'task_changed',
    ANNOTATION_CREATED = 'annotation_created',
    ANNOTATION_UPDATED = 'annotation_updated',
    ANNOTATION_DELETED = 'annotation_deleted',
    TIMER_STARTED = 'timer_started',
    TIMER_PAUSED = 'timer_paused',
    TIMER_STOPPED = 'timer_stopped',
    ERROR_OCCURRED = 'error_occurred'
}

/**
 * Event payload for workspace events
 */
export interface WorkspaceEventPayload {
    event: WorkspaceEvent;
    data: any;
    timestamp: number;
    context: WorkspaceOperationContext;
}