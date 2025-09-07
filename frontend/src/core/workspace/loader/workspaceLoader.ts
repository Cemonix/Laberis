import { AppLogger } from '@/core/logger/logger';
import { AssetManager } from '@/core/workspace';
import { taskManager } from '../task/taskManager';
import { annotationDataManager } from '../data/annotationDataManager';
import { 
    labelSchemeService,
    labelService,
    workflowService,
    workflowStageService
} from '@/services/project';

import type { Asset } from '@/core/asset/asset.types';
import type { ImageDimensions } from "@/core/asset/asset.types";
import type { Task } from '@/services/project/task/task.types';
import type { Annotation } from '@/core/workspace/annotation.types';
import type { LabelScheme } from '@/services/project/labelScheme/label.types';
import type { WorkflowStageType } from '@/services/project/workflow/workflowStage.types';
import type { TaskNavigationContext } from '@/stores/workspaceStore.types';

const logger = AppLogger.createServiceLogger('WorkspaceLoader');

// Core managers
const assetManager = new AssetManager();

/**
 * Parameters for loading workspace data
 */
export interface WorkspaceLoadParams {
    projectId: string;
    assetId: string;
    taskId?: string;
}

/**
 * Asset loading result
 */
export interface AssetLoadResult {
    asset: Asset;
    imageUrl: string | null;
    naturalDimensions: ImageDimensions | null;
}

/**
 * Task loading result  
 */
export interface TaskLoadResult {
    currentTask: Task | null;
    availableTasks: Task[];
    workflowStageType: WorkflowStageType | null;
    navigationContext: TaskNavigationContext | null;
}

/**
 * Label scheme loading result
 */
export interface LabelSchemeLoadResult {
    labelScheme: LabelScheme | null;
    availableSchemes: LabelScheme[];
}

/**
 * Complete workspace loading result
 */
export interface WorkspaceLoadResult {
    success: boolean;
    error?: string;
    asset?: AssetLoadResult;
    annotations?: Annotation[];
    tasks?: TaskLoadResult;
    labelScheme?: LabelSchemeLoadResult;
}

/**
 * WorkspaceLoader orchestrates the loading of all workspace-related data.
 * It coordinates asset loading, annotation fetching, task management, and label scheme loading.
 */
export class WorkspaceLoader {
    /**
     * Load asset data using AssetManager
     */
    async loadAssetData(projectId: string, assetId: string): Promise<AssetLoadResult | null> {
        try {
            logger.info(`Loading asset ${assetId} for project ${projectId}`);

            // Use AssetManager to load the asset
            const assetResult = await assetManager.loadAsset(projectId, assetId);
            
            if (!assetResult.success) {
                throw new Error(assetResult.error);
            }

            logger.info(`Successfully loaded asset data:`, assetResult.asset);
            return {
                asset: assetResult.asset,
                imageUrl: assetResult.imageUrl,
                naturalDimensions: assetResult.naturalDimensions
            };
        } catch (error) {
            logger.error(`Failed to load asset ${assetId}:`, error);
            return null;
        }
    }

    /**
     * Load annotations for the specified asset
     */
    async loadAnnotationsForAsset(projectId: number, assetId: number): Promise<Annotation[]> {
        try {
            logger.info(`Loading annotations for asset ${assetId}`);
            const result = await annotationDataManager.loadAnnotationsForAsset(projectId, assetId);
            
            if (!result.success) {
                logger.error("Failed to fetch annotations:", result.error);
                return [];
            }
            
            logger.info(`Loaded ${result.data.length} annotations for asset ${assetId}`);
            return result.data;
        } catch (error) {
            logger.error("Failed to fetch annotations:", error);
            return [];
        }
    }

    /**
     * Load tasks for the specified asset and determine current task
     */
    async loadTasksForAsset(
        projectId: number, 
        assetId: number, 
        initialTaskId?: number
    ): Promise<TaskLoadResult> {
        const result: TaskLoadResult = {
            currentTask: null,
            availableTasks: [],
            workflowStageType: null,
            navigationContext: null
        };

        try {
            let currentTask: Task | null = null;
            let workflowStageId: number | null = null;

            // Load specific task if provided
            if (initialTaskId) {
                try {
                    const taskResult = await taskManager.loadTask(projectId, initialTaskId);
                    if (taskResult.success) {
                        currentTask = taskResult.data;
                        workflowStageId = currentTask.workflowStageId;
                        logger.info(`Loaded specific task ${initialTaskId} for stage ${workflowStageId}`);
                    } else {
                        logger.warn(`Failed to load task ${initialTaskId}, falling back to asset tasks:`, taskResult.error);
                    }
                } catch (taskByIdError) {
                    logger.warn(`Failed to load task ${initialTaskId}, falling back to asset tasks:`, taskByIdError);
                }
            }

            // If no current task yet, find task(s) for this asset
            if (!currentTask) {
                const assetTasksResult = await taskManager.loadTasksForAsset(projectId, assetId);
                if (assetTasksResult.success && assetTasksResult.data.length > 0) {
                    currentTask = assetTasksResult.data[0];
                    workflowStageId = currentTask?.workflowStageId || null;
                    logger.info(`Found asset task ${currentTask.id} for asset ${assetId} in stage ${workflowStageId}`);
                }
            }

            if (currentTask && workflowStageId) {
                // Load user's assigned tasks for this workflow stage
                const assignedTasksResult = await taskManager.loadMyTasksForStage(projectId, workflowStageId);
                let tasksForNavigation = assignedTasksResult.success ? assignedTasksResult.data : [];
                
                // If current task is not in the assigned tasks list, add it
                const currentTaskInList = tasksForNavigation.find(task => task.id === currentTask.id);
                if (!currentTaskInList) {
                    tasksForNavigation = [...tasksForNavigation, currentTask];
                    logger.info(`Added current task ${currentTask.id} to navigation context`);
                }
                
                result.currentTask = currentTask;
                result.availableTasks = tasksForNavigation;

                // Load navigation context
                const navigationResult = await taskManager.getNavigationContext(
                    projectId, 
                    currentTask.id,
                    currentTask.workflowStageId
                );
                
                if (navigationResult.success) {
                    result.navigationContext = navigationResult.data;
                    logger.info(`Loaded navigation context: ${navigationResult.data.currentPosition}/${navigationResult.data.totalTasks}`);
                }

                // Load workflow stage type
                try {
                    const stageData = await workflowStageService.getWorkflowStageById(
                        projectId, 
                        currentTask.workflowId, 
                        workflowStageId
                    );
                    result.workflowStageType = stageData.stageType || null;
                    logger.info(`Loaded workflow stage type: ${result.workflowStageType} for stage ${workflowStageId}`);
                } catch (stageError) {
                    logger.warn('Failed to fetch workflow stage type:', stageError);
                }

                logger.info(`Loaded ${result.availableTasks.length} tasks for stage ${workflowStageId}`);
            } else {
                logger.warn(`No tasks found for asset ${assetId}`);
            }
        } catch (taskError) {
            logger.error("Failed to fetch tasks:", taskError);
        }

        return result;
    }

    /**
     * Load label scheme for the current workflow
     */
    async loadLabelScheme(
        projectId: number, 
        currentTask: Task | null
    ): Promise<LabelSchemeLoadResult> {
        const result: LabelSchemeLoadResult = {
            labelScheme: null,
            availableSchemes: []
        };

        try {
            if (currentTask && currentTask.workflowId) {
                // Get workflow to access its assigned label scheme
                const workflow = await workflowService.getWorkflowById(projectId, currentTask.workflowId);
                
                if (workflow.labelSchemeId) {
                    // Get the specific label scheme assigned to this workflow
                    const labelScheme = await labelSchemeService.getLabelSchemeById(projectId, workflow.labelSchemeId);
                    
                    // Fetch labels for the scheme
                    try {
                        const labelsResponse = await labelService.getLabelsForScheme(projectId, labelScheme.labelSchemeId);
                        labelScheme.labels = labelsResponse.data;
                        
                        // Set this as the only available scheme (no switching allowed)
                        result.availableSchemes = [labelScheme];
                        result.labelScheme = labelScheme;
                        logger.info(`Loaded workflow label scheme: ${labelScheme.name} with ${labelsResponse.data.length} labels`);
                    } catch (labelsError) {
                        logger.error("Failed to fetch labels for workflow scheme:", labelsError);
                        result.availableSchemes = [labelScheme];
                        result.labelScheme = labelScheme; // Set scheme even without labels
                    }
                } else {
                    logger.warn(`Workflow ${currentTask.workflowId} has no assigned label scheme`);
                }
            } else {
                logger.warn('No current task with workflow information available for label scheme loading');
            }
        } catch (labelSchemeError) {
            logger.error("Failed to fetch workflow label scheme:", labelSchemeError);
        }

        return result;
    }

    /**
     * Load all workspace data for the given parameters
     */
    async loadWorkspaceData(params: WorkspaceLoadParams): Promise<WorkspaceLoadResult> {
        const { projectId, assetId, taskId } = params;
        const numericProjectId = parseInt(projectId, 10);
        const numericAssetId = parseInt(assetId, 10);
        const numericTaskId = taskId ? parseInt(taskId, 10) : undefined;

        logger.info(`Loading workspace data for project ${projectId}, asset ${assetId}, task ${taskId || 'auto'}`);

        try {
            // Load asset data
            const assetData = await this.loadAssetData(projectId, assetId);
            if (!assetData) {
                return { success: false, error: 'Failed to load asset data' };
            }

            // Load annotations for this asset
            const annotations = await this.loadAnnotationsForAsset(numericProjectId, numericAssetId);

            // Load tasks for this asset
            const tasksData = await this.loadTasksForAsset(numericProjectId, numericAssetId, numericTaskId);

            // Load label scheme from the workflow
            const labelSchemeData = await this.loadLabelScheme(numericProjectId, tasksData.currentTask);

            logger.info(`Successfully loaded all workspace data for asset ${assetId}`);
            return {
                success: true,
                asset: assetData,
                annotations,
                tasks: tasksData,
                labelScheme: labelSchemeData
            };

        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load workspace data';
            logger.error(`Failed to load workspace data for asset ${assetId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Reset workspace state (helper for clearing data)
     */
    getInitialWorkspaceState() {
        return {
            asset: {
                currentProjectId: null,
                currentAssetId: null,
                currentAssetData: null,
                currentImageUrl: null,
                imageNaturalDimensions: null,
                canvasDisplayDimensions: null
            },
            task: {
                currentTaskId: null,
                currentTaskData: null,
                currentWorkflowStageType: null,
                availableTasks: [],
                initialTaskId: null
            },
            annotation: {
                annotations: [],
                currentLabelId: null,
                currentLabelScheme: null,
                availableLabelSchemes: []
            },
            ui: {
                viewOffset: { x: 0, y: 0 },
                zoomLevel: 1.0,
                isLoading: false,
                error: null
            },
            timer: {
                elapsedTimeDisplay: "00:00:00",
                timerIntervalId: null,
                lastSavedWorkingTime: 0
            },
            navigationContext: null
        };
    }
}

// Export singleton instance
export const workspaceLoader = new WorkspaceLoader();