import { defineStore } from "pinia";
import { faArrowPointer, faDotCircle, faMinus, faWaveSquare, faSquare, faDrawPolygon } from '@fortawesome/free-solid-svg-icons';
import type { ImageDimensions } from "@/core/asset/asset.types";
import type { WorkspaceState } from "./workspaceStore.types";
import { Timer } from "@/core/timing";
import type { Point } from "@/core/geometry/geometry.types";
import { ToolName, type Tool } from "@/core/workspace/tools.types";
import type { Annotation, CreateAnnotationDto } from '@/core/workspace/annotation.types';
import type { LabelScheme, Label } from '@/services/project/labelScheme/label.types';
import { AssetManager, TaskManager, TaskNavigationManager } from '@/core/workspace';
import { 
    annotationService,
    labelSchemeService,
    labelService,
    taskService,
    workflowService,
    workflowStageService
} from '@/services/project';
import type { Asset } from '@/core/asset/asset.types';
import type { Task } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';
import { AppLogger } from '@/core/logger/logger';
import { WorkflowStageType } from '@/services/project/workflow/workflowStage.types';

const logger = AppLogger.createServiceLogger('WorkspaceStore');

const MIN_ZOOM = 0.1;
const MAX_ZOOM = 10.0;
const ZOOM_SENSITIVITY = 0.005;

// Core managers
const assetManager = new AssetManager();

// TODO: Refactor the store

export const useWorkspaceStore = defineStore("workspace", {
    state: (): WorkspaceState => ({
        currentProjectId: null,
        currentAssetId: null,
        currentAssetData: null,
        currentImageUrl: null,
        imageNaturalDimensions: null,
        canvasDisplayDimensions: null,
        timerInstance: new Timer(),
        elapsedTimeDisplay: "00:00:00",
        timerIntervalId: null,
        lastSavedWorkingTime: 0,
        viewOffset: { x: 0, y: 0 },
        zoomLevel: 1.0,
        activeTool: ToolName.CURSOR,
        availableTools: [
            { id: ToolName.CURSOR, name: 'Cursor', iconDefinition: faArrowPointer },
            { id: ToolName.POINT, name: 'Point', iconDefinition: faDotCircle },
            { id: ToolName.LINE, name: 'Line', iconDefinition: faMinus },
            { id: ToolName.BOUNDING_BOX, name: 'Bounding Box', iconDefinition: faSquare },
            { id: ToolName.POLYLINE, name: 'Polyline', iconDefinition: faWaveSquare },
            { id: ToolName.POLYGON, name: 'Polygon', iconDefinition: faDrawPolygon },
        ],
        annotations: [] as Annotation[],
        currentLabelId: null as number | null,
        currentLabelScheme: null as LabelScheme | null,
        availableLabelSchemes: [] as LabelScheme[],
        currentTaskId: null as number | null,
        currentTaskData: null as Task | null,
        currentWorkflowStageType: null as WorkflowStageType | null,
        availableTasks: [] as Task[],
        initialTaskId: null as number | null, // Store the initial task ID from URL query
        isLoading: false,
        error: null,
    }),

    getters: {
        getCurrentImageAspectRatio(): number | null {
            if (this.imageNaturalDimensions) {
                return (
                    this.imageNaturalDimensions.width /
                    this.imageNaturalDimensions.height
                );
            }
            return null;
        },
        getZoomConfig: () => ({
            minZoom: MIN_ZOOM,
            maxZoom: MAX_ZOOM,
            zoomSensitivity: ZOOM_SENSITIVITY,
        }),
        getActiveToolDetails(): Tool | undefined {
            return this.availableTools.find((tool: Tool) => tool.id === this.activeTool);
        },
        getAnnotations(state): Annotation[] {
            return state.annotations;
        },
        getSelectedLabelId(state): number | null {
            return state.currentLabelId;
        },
        getCurrentLabelScheme(state): LabelScheme | null {
            return state.currentLabelScheme;
        },
        getLabelById(state): (labelId: number) => Label | undefined {
            return (labelId: number) => {
                if (!state.currentLabelScheme || !state.currentLabelScheme.labels) {
                    return undefined;
                }
                return state.currentLabelScheme.labels.find((label: Label) => label.labelId === labelId);
            };
        },
        getCurrentAsset(state): Asset | null {
            return state.currentAssetData;
        },
        getLoadingState(state): boolean {
            return state.isLoading;
        },
        getError(state): string | null {
            return state.error;
        },
        getAvailableLabels(state): Label[] {
            return state.currentLabelScheme?.labels || [];
        },
        getAvailableLabelSchemes(state): LabelScheme[] {
            return state.availableLabelSchemes;
        },
        getCurrentTask(state): Task | null {
            return state.currentTaskData;
        },
        getAvailableTasks(state): Task[] {
            return state.availableTasks;
        },
        getCurrentTaskIndex(state): number {
            if (!state.currentTaskData || state.availableTasks.length === 0) {
                return -1;
            }
            return state.availableTasks.findIndex((task: Task) => task.id === state.currentTaskData?.id);
        },
        getTaskNavigationInfo(state): { current: number; total: number } {
            // Filter out tasks that cannot be opened for navigation
            // Note: Deferred tasks permission checking is handled in the navigation buttons themselves
            const accessibleTasks = state.availableTasks.filter((task: Task) => {
                // Vetoed tasks cannot be opened (they are view-only)
                if (task.status === TaskStatus.VETOED) {
                    return false;
                }
                
                // Completed and archived tasks cannot be opened for editing
                if (task.status && [TaskStatus.COMPLETED, TaskStatus.ARCHIVED].includes(task.status)) {
                    return false;
                }
                
                return true;
            });

            const currentIndex = state.currentTaskData && accessibleTasks.length > 0
                ? accessibleTasks.findIndex((task: Task) => task.id === state.currentTaskData?.id)
                : -1;
            
            return {
                current: currentIndex >= 0 ? currentIndex + 1 : 0,
                total: accessibleTasks.length
            };
        },
        isTaskCompleted(state): boolean {
            if (!state.currentTaskData) return false;
            
            // Task is NOT considered completed (i.e., should be editable) if it has CHANGES_REQUIRED status
            if (state.currentTaskData.status === TaskStatus.CHANGES_REQUIRED) {
                return false;
            }
            
            // Otherwise, check if task is actually completed
            return state.currentTaskData.status === TaskStatus.COMPLETED;
        },
        isAnnotationEditingDisabled(): boolean {
            // Annotation editing is disabled when task is completed (preview mode)
            return this.isTaskCompleted;
        },
        canNavigateToPrevious(state): boolean {
            if (!state.currentTaskData || state.availableTasks.length === 0) return false;
            const currentIndex = state.availableTasks.findIndex((task: Task) => task.id === state.currentTaskData?.id);
            return currentIndex > 0;
        },
        canNavigateToNext(state): boolean {
            if (!state.currentTaskData || state.availableTasks.length === 0) return false;
            const currentIndex = state.availableTasks.findIndex((task: Task) => task.id === state.currentTaskData?.id);
            return currentIndex < state.availableTasks.length - 1;
        },
        canCompleteCurrentTask(state): boolean {
            if (!state.currentTaskData || !state.currentAssetData) {
                return false;
            }

            // Check if task is already completed (but allow completion for CHANGES_REQUIRED tasks)
            if (state.currentTaskData.status === TaskStatus.COMPLETED) {
                return false;
            }

            // For revision and completion stages, check annotations for the asset (not just the current task)
            // For annotation stages, check annotations for the specific task
            if (state.currentWorkflowStageType === WorkflowStageType.REVISION || 
                state.currentWorkflowStageType === WorkflowStageType.COMPLETION) {
                // In revision/completion stages, check if there are annotations for this asset
                // (created in previous annotation stages)
                const assetAnnotations = state.annotations.filter((annotation: Annotation) => 
                    annotation.assetId === state.currentAssetData?.id
                );
                return assetAnnotations.length > 0;
            } else {
                // In annotation stages, check annotations for this specific task
                const taskAnnotations = state.annotations.filter((annotation: Annotation) => 
                    annotation.taskId === state.currentTaskData?.id
                );
                return taskAnnotations.length > 0;
            }
        },
        canVetoCurrentTask(state): boolean {
            // Check if there's a current task that exists and is not archived
            if (!state.currentTaskData || state.currentTaskData.archivedAt) {
                return false;
            }

            // Only show veto for review and completion stages (not annotation stages)
            if (state.currentWorkflowStageType === WorkflowStageType.REVISION || 
                state.currentWorkflowStageType === WorkflowStageType.COMPLETION) {
                // Permission check will be handled in the component
                return true;
            }

            return false;
        },

        // Task management core instances with access to store state
        taskManager(): TaskManager {
            const permissionsService = {
                canUpdateProject: async () => {
                    // Import permission check dynamically to avoid circular dependencies
                    try {
                        const { usePermissions } = await import('@/composables/usePermissions');
                        const { canUpdateProject } = usePermissions();
                        return canUpdateProject.value;
                    } catch {
                        return false;
                    }
                }
            };

            const timerService = {
                getElapsedTime: () => this.timerInstance.getElapsedTime(),
                isRunning: () => this.timerInstance.isRunning,
                start: () => this.timerInstance.start(),
                stop: () => this.timerInstance.stop(),
                pause: () => this.timerInstance.pause(),
                resume: () => this.timerInstance.start(), // Timer uses start() for resume
                reset: () => this.timerInstance.reset()
            };

            return new TaskManager(taskService, permissionsService, timerService);
        },

        taskNavigationManager(): TaskNavigationManager {
            const permissionsService = {
                canUpdateProject: async () => {
                    // Import permission check dynamically to avoid circular dependencies
                    try {
                        const { usePermissions } = await import('@/composables/usePermissions');
                        const { canUpdateProject } = usePermissions();
                        return canUpdateProject.value;
                    } catch {
                        return false;
                    }
                }
            };

            return new TaskNavigationManager(permissionsService);
        }
    },

    actions: {
        async loadAsset(projectId: string, assetId: string, taskId?: string) {
            this.currentProjectId = projectId;
            this.currentAssetId = assetId;
            this.isLoading = true;
            this.error = null;

            try {
                logger.info(`Loading asset ${assetId} for project ${projectId}`);

                // Use AssetManager to load the asset
                const assetResult = await assetManager.loadAsset(projectId, assetId);
                
                if (!assetResult.success) {
                    throw new Error(assetResult.error);
                }

                // Set asset data from AssetManager result
                this.currentAssetData = assetResult.asset;
                this.currentImageUrl = assetResult.imageUrl;
                this.imageNaturalDimensions = assetResult.naturalDimensions;

                logger.info(`Loaded asset data via AssetManager:`, assetResult.asset);

                // Convert IDs for other service calls (AssetManager already validated these)
                const numericProjectId = parseInt(projectId, 10);
                const numericAssetId = parseInt(assetId, 10);

                // Reset other workspace state
                this.viewOffset = { x: 0, y: 0 };
                this.zoomLevel = 1;
                this.annotations = [];
                this.currentLabelId = null;
                this.currentTaskId = null;
                this.currentTaskData = null;
                this.currentWorkflowStageType = null;
                this.availableTasks = [];
                this.initialTaskId = taskId ? parseInt(taskId, 10) : null;

                // Fetch annotations for this asset
                try {
                    const fetchedAnnotations = await annotationService.getAnnotationsForAsset(numericProjectId, numericAssetId);
                    this.setAnnotations(fetchedAnnotations.data);
                    logger.info(`Loaded ${fetchedAnnotations.data.length} annotations for asset ${assetId}`);
                } catch (annotationError) {
                    logger.error("Failed to fetch annotations:", annotationError);
                    this.setAnnotations([]); // Clear annotations on error
                }

                // Fetch tasks - either by specific task ID or find task for this asset
                try {
                    let currentTask: Task | null = null;
                    let workflowStageId: number | null = null;

                    if (this.initialTaskId) {
                        // If we have a specific task ID (from URL query), load that task first
                        try {
                            currentTask = await taskService.getTaskById(numericProjectId, this.initialTaskId);
                            workflowStageId = currentTask.workflowStageId;
                            logger.info(`Loaded specific task ${this.initialTaskId} for stage ${workflowStageId}`);
                        } catch (taskByIdError) {
                            logger.warn(`Failed to load task ${this.initialTaskId}, falling back to asset tasks:`, taskByIdError);
                        }
                    }

                    // If we don't have a current task yet, find task(s) for this asset
                    if (!currentTask) {
                        const assetTasks = await taskService.getTasksForAsset(numericProjectId, numericAssetId);
                        if (assetTasks.length > 0) {
                            currentTask = assetTasks[0];
                            workflowStageId = currentTask?.workflowStageId || null;
                            logger.info(`Found asset task ${currentTask.id} for asset ${assetId} in stage ${workflowStageId}`);
                        }
                    }

                    if (currentTask && workflowStageId) {
                        // Load all tasks for this workflow stage to enable navigation
                        const stageTasks = await taskService.getTasksForStage(numericProjectId, workflowStageId);
                        this.availableTasks = stageTasks.tasks;
                        this.currentTaskData = currentTask;
                        this.currentTaskId = currentTask.id;

                        // Fetch current workflow stage type for completion logic
                        try {
                            const stageData = await workflowStageService.getWorkflowStageById(
                                numericProjectId, 
                                currentTask.workflowId, 
                                workflowStageId
                            );
                            this.currentWorkflowStageType = stageData.stageType || null;
                            logger.info(`Loaded workflow stage type: ${this.currentWorkflowStageType} for stage ${workflowStageId}`);
                        } catch (stageError) {
                            logger.warn('Failed to fetch workflow stage type:', stageError);
                            this.currentWorkflowStageType = null;
                        }

                        const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === this.currentTaskData?.id);
                        logger.info(`Loaded ${this.availableTasks.length} tasks for stage ${workflowStageId}, current task is at index ${taskIndex}`);
                    } else {
                        logger.warn(`No tasks found for asset ${assetId}`);
                    }
                } catch (taskError) {
                    logger.error("Failed to fetch tasks:", taskError);
                    // Don't fail the entire load process if tasks fail
                }

                // Fetch label scheme from the workflow
                try {
                    if (this.currentTaskData && this.currentTaskData.workflowId) {
                        // Get workflow to access its assigned label scheme
                        const workflow = await workflowService.getWorkflowById(numericProjectId, this.currentTaskData.workflowId);
                        
                        if (workflow.labelSchemeId) {
                            // Get the specific label scheme assigned to this workflow
                            const labelScheme = await labelSchemeService.getLabelSchemeById(numericProjectId, workflow.labelSchemeId);
                            
                            // Fetch labels for the scheme
                            try {
                                const labelsResponse = await labelService.getLabelsForScheme(numericProjectId, labelScheme.labelSchemeId);
                                labelScheme.labels = labelsResponse.data;
                                
                                // Set this as the only available scheme (no switching allowed)
                                this.availableLabelSchemes = [labelScheme];
                                this.setCurrentLabelScheme(labelScheme);
                                logger.info(`Loaded workflow label scheme: ${labelScheme.name} with ${labelsResponse.data.length} labels`);
                            } catch (labelsError) {
                                logger.error("Failed to fetch labels for workflow scheme:", labelsError);
                                this.availableLabelSchemes = [labelScheme];
                                this.setCurrentLabelScheme(labelScheme); // Set scheme even without labels
                            }
                        } else {
                            logger.warn(`Workflow ${this.currentTaskData.workflowId} has no assigned label scheme`);
                            this.availableLabelSchemes = [];
                            this.setCurrentLabelScheme(null);
                        }
                    } else {
                        logger.warn('No current task with workflow information available for label scheme loading');
                        this.availableLabelSchemes = [];
                        this.setCurrentLabelScheme(null);
                    }
                } catch (labelSchemeError) {
                    logger.error("Failed to fetch workflow label scheme:", labelSchemeError);
                    this.availableLabelSchemes = [];
                    this.setCurrentLabelScheme(null);
                }

                // Start timer and set cursor tool
                this.startTimer();
                this.setActiveTool(ToolName.CURSOR);
                
                this.isLoading = false;
                logger.info(`Successfully loaded asset ${assetId} for project ${projectId}`);
            } catch (error) {
                this.isLoading = false;
                this.error = error instanceof Error ? error.message : 'Failed to load asset';
                logger.error(`Failed to load asset ${assetId} for project ${projectId}:`, error);
                
                // Reset state on error
                this.currentAssetData = null;
                this.currentImageUrl = null;
                this.imageNaturalDimensions = null;
                this.annotations = [];
                this.currentLabelScheme = null;
                this.availableLabelSchemes = [];
                this.currentLabelId = null;
                this.currentTaskId = null;
                this.currentTaskData = null;
                this.currentWorkflowStageType = null;
                this.availableTasks = [];
                this.initialTaskId = null;
            }
        },

        setImageNaturalDimensions(dimensions: ImageDimensions) {
            this.imageNaturalDimensions = dimensions;
        },

        setCanvasDisplayDimensions(dimensions: ImageDimensions) {
            this.canvasDisplayDimensions = dimensions;
        },

        setAnnotations(annotations: Annotation[]) {
            this.annotations = annotations;
        },

        /**
         * Adds a new annotation to the current list and saves it to the backend.
         */
        async addAnnotation(annotation: Annotation) {
            // Add annotation to the store immediately for optimistic updates
            this.annotations.push(annotation);

            try {
                // Convert Annotation to CreateAnnotationDto
                const createDto: CreateAnnotationDto = {
                    annotationType: annotation.annotationType,
                    data: annotation.data || (annotation.coordinates ? JSON.stringify(annotation.coordinates) : '{}'),
                    taskId: annotation.taskId,
                    assetId: annotation.assetId,
                    labelId: annotation.labelId,
                    isPrediction: annotation.isPrediction || false,
                    confidenceScore: annotation.confidenceScore,
                    isGroundTruth: annotation.isGroundTruth,
                    version: annotation.version || 1,
                    notes: annotation.notes,
                    annotatorEmail: annotation.annotatorEmail,
                    parentAnnotationId: annotation.parentAnnotationId
                };

                // Save annotation to backend
                const savedAnnotation = await annotationService.createAnnotation(parseInt(this.currentProjectId!), createDto);
                
                // Save working time when annotation is created (lazy update)
                await this._autoSaveWorkingTime();

                // Update the annotation in the store with the saved data (including ID)
                // First try to find by clientId
                let index = this.annotations.findIndex((a: Annotation) => a.clientId === annotation.clientId);

                if (index !== -1) {
                    // Preserve the clientId when updating
                    this.annotations[index] = { ...savedAnnotation, clientId: annotation.clientId };
                    logger.debug(`Updated annotation by clientId: ${annotation.clientId}`);
                } else {
                    // If clientId doesn't match, find by coordinates and other properties
                    index = this.annotations.findIndex((a: Annotation) => 
                        !a.annotationId && // Only match unsaved annotations
                        a.assetId === savedAnnotation.assetId && 
                        a.labelId === savedAnnotation.labelId &&
                        a.annotationType === savedAnnotation.annotationType &&
                        JSON.stringify(a.coordinates) === JSON.stringify(savedAnnotation.coordinates)
                    );
                    
                    if (index !== -1) {
                        // Preserve the original clientId
                        this.annotations[index] = { ...savedAnnotation, clientId: this.annotations[index].clientId };
                        logger.debug(`Updated annotation by coordinates match`);
                    } else {
                        logger.warn(`Could not find matching annotation to update after save`);
                        // Add as new annotation if we can't find a match
                        this.annotations.push({ ...savedAnnotation, clientId: annotation.clientId });
                    }
                }

                logger.info(`Successfully saved annotation with ID: ${savedAnnotation.annotationId}`);

            } catch (error) {
                logger.error("Failed to save annotation:", error);

                // Remove the annotation from the store on failure
                this.annotations = this.annotations.filter((a: Annotation) => a.clientId !== annotation.clientId);

                // Set error state
                this.error = error instanceof Error ? error.message : 'Failed to save annotation';
            }
        },

        /**
         * Updates an existing annotation
         */
        async updateAnnotation(annotationId: number, updates: Partial<Annotation>) {
            const index = this.annotations.findIndex((a: Annotation) => a.annotationId === annotationId);
            if (index === -1) {
                logger.error(`Annotation with ID ${annotationId} not found in store`);
                return;
            }

            // Store original annotation for rollback
            const originalAnnotation = { ...this.annotations[index] };
            
            // Optimistically update the annotation in the store
            this.annotations[index] = { ...this.annotations[index], ...updates };

            try {
                // Prepare update payload for backend
                const updatePayload: any = {};
                if (updates.annotationType) updatePayload.annotationType = updates.annotationType;
                if (updates.coordinates) updatePayload.data = JSON.stringify(updates.coordinates);
                if (updates.isPrediction !== undefined) updatePayload.isPrediction = updates.isPrediction;
                if (updates.confidenceScore !== undefined) updatePayload.confidenceScore = updates.confidenceScore;
                if (updates.isGroundTruth !== undefined) updatePayload.isGroundTruth = updates.isGroundTruth;
                if (updates.notes !== undefined) updatePayload.notes = updates.notes;
                if (updates.labelId !== undefined) updatePayload.labelId = updates.labelId;

                // Update annotation on backend
                const updatedAnnotation = await annotationService.updateAnnotation(parseInt(this.currentProjectId!), annotationId, updatePayload);
                
                // Save working time when annotation is updated (lazy update)
                await this._autoSaveWorkingTime();

                // Update the annotation in the store with the response from backend
                this.annotations[index] = updatedAnnotation;

                logger.info(`Successfully updated annotation with ID: ${annotationId}`);

            } catch (error) {
                logger.error(`Failed to update annotation ${annotationId}:`, error);

                // Rollback optimistic update
                this.annotations[index] = originalAnnotation;

                // Set error state
                this.error = error instanceof Error ? error.message : 'Failed to update annotation';
            }
        },

        /**
         * Deletes an annotation
         */
        async deleteAnnotation(annotationId: number) {
            const index = this.annotations.findIndex((a: Annotation) => a.annotationId === annotationId);
            if (index === -1) {
                logger.error(`Annotation with ID ${annotationId} not found in store`);
                return;
            }

            // Store annotation for rollback
            const deletedAnnotation = this.annotations[index];
            
            // Optimistically remove the annotation from the store
            this.annotations.splice(index, 1);

            try {
                // Delete annotation on backend
                await annotationService.deleteAnnotation(parseInt(this.currentProjectId!), annotationId);
                
                // Save working time when annotation is deleted (lazy update)
                await this._autoSaveWorkingTime();

                logger.info(`Successfully deleted annotation with ID: ${annotationId}`);

            } catch (error) {
                logger.error(`Failed to delete annotation ${annotationId}:`, error);

                // Rollback optimistic deletion
                this.annotations.splice(index, 0, deletedAnnotation);

                // Set error state
                this.error = error instanceof Error ? error.message : 'Failed to delete annotation';
            }
        },

        /**
         * Sets the currently selected label ID.
         */
        setCurrentLabelId(labelId: number | null) {
            this.currentLabelId = labelId;
            if (labelId !== null) {
                const label = this.getLabelById(labelId); // Using the getter within an action
                logger.info(`[Store] Selected Label ID: ${labelId} (${label?.name || 'Unknown'})`);
            } else {
                logger.info("[Store] Label selection cleared.");
            }
        },

        /**
         * Sets the label scheme for the current context.
         * @param scheme The label scheme to set, or null to clear it.
         */
        setCurrentLabelScheme(scheme: LabelScheme | null) {
            this.currentLabelScheme = scheme;
            if (scheme) {
                logger.info("[Store] Label Scheme set:", scheme.name);
            } else {
                logger.info("[Store] Label Scheme cleared.");
            }
        },
        
        /**
         * Sets the current task ID.
         * @param taskId The ID of the current task.
         */
        setCurrentTaskId(taskId: number | null) {
            this.currentTaskId = taskId;
            if (taskId !== null) {
                logger.info("[Store] Current Task ID set:", taskId);
            } else {
                logger.info("[Store] Current Task ID cleared.");
            }
        },

        startTimer() {
            this._clearInterval();

            // Store the saved working time for this session
            this.lastSavedWorkingTime = this.currentTaskData?.workingTimeMs || 0;
            
            
            // For completed/archived tasks, only display the saved time - don't run the timer
            if (!this._shouldTrackTime()) {
                this.timerInstance.reset();
                // Don't start the timer - just display the saved time
                this._updateElapsedTimeDisplay();
                logger.info('Task is completed/archived - displaying saved working time only');
                return;
            }
            
            // For active tasks, start the timer fresh for this session
            this.timerInstance.reset();
            this.timerInstance.start();

            this._updateElapsedTimeDisplay();

            // Update display every second
            this.timerIntervalId = window.setInterval(() => {
                this._updateElapsedTimeDisplay();
            }, 1000);
        },

        pauseTimer() {
            if (this.timerInstance.isRunning) {
                this.timerInstance.pause();
                this._clearInterval();
                this._updateElapsedTimeDisplay();
                
                // Save current working time when pausing
                this._autoSaveWorkingTime();
            }
        },

        stopAndResetTimer() {
            // Save working time before stopping
            this._autoSaveWorkingTime();
            
            this.timerInstance.stop();
            this.timerInstance.reset();
            this._clearInterval();
            this.lastSavedWorkingTime = 0;
            this.elapsedTimeDisplay = "00:00:00";
        },
        
        cleanupTimer() {
            // Save current working time before cleanup
            this._autoSaveWorkingTime();
            
            this._clearInterval();

            // Pause the timer instead of stopping it completely
            // so time is preserved when user returns to the same task
            if (this.timerInstance.isRunning) {
                this.timerInstance.pause();
            }
        },

        setViewOffset(offset: Point) {
            this.viewOffset = offset;
        },

        setZoomLevel(level: number) {
            this.zoomLevel = Math.max(MIN_ZOOM, Math.min(level, MAX_ZOOM));
        },
        
        setActiveTool(toolId: ToolName) {
            const toolExists = this.availableTools.some((tool: Tool) => tool.id === toolId);
            if (toolExists) {
                this.activeTool = toolId;
            }
        },

        clearError() {
            this.error = null;
        },

        setLoading(loading: boolean) {
            this.isLoading = loading;
        },

        _clearInterval() {
            if (this.timerIntervalId) {
                clearInterval(this.timerIntervalId);
                this.timerIntervalId = null;
            }
        },

        _updateElapsedTimeDisplay() {
            // For completed/archived tasks, only show the saved working time
            if (!this._shouldTrackTime()) {
                this.elapsedTimeDisplay = this.timerInstance.getFormattedElapsedTime(this.lastSavedWorkingTime);
                return;
            }
            
            // For active tasks, calculate total working time (saved time + current session time)
            const currentSessionTime = this.timerInstance.getElapsedTime();
            const totalWorkingTime = this.lastSavedWorkingTime + currentSessionTime;
            
            // Format the total time
            this.elapsedTimeDisplay = this.timerInstance.getFormattedElapsedTime(totalWorkingTime);
        },


        _shouldTrackTime(): boolean {
            // Only track time for active, non-completed tasks
            return !!(this.currentTaskData && 
                !this.currentTaskData.completedAt && 
                !this.currentTaskData.archivedAt);
        },

        async _autoSaveWorkingTime() {
            if (!this.currentTaskData || !this.currentProjectId || !this._shouldTrackTime()) {
                return;
            }

            try {
                const currentElapsedTime = this.timerInstance.getElapsedTime();
                const totalWorkingTime = this.lastSavedWorkingTime + currentElapsedTime;

                // Only save if there's been a meaningful change (at least 1 second)
                if (totalWorkingTime > this.currentTaskData.workingTimeMs + 1000) {
                    logger.info(`Auto-saving working time for task ${this.currentTaskData.id}: ${totalWorkingTime}ms`);
                    
                    const updatedTask = await taskService.updateWorkingTime(
                        parseInt(this.currentProjectId), 
                        this.currentTaskData.id, 
                        totalWorkingTime
                    );

                    if (updatedTask) {
                        // Update the current task data with the new working time
                        this.currentTaskData.workingTimeMs = totalWorkingTime;
                        
                        // Update in the available tasks list as well
                        const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === this.currentTaskData?.id);
                        if (taskIndex !== -1) {
                            this.availableTasks[taskIndex].workingTimeMs = totalWorkingTime;
                        }
                    }
                }
            } catch (error) {
                logger.warn('Failed to auto-save working time:', error);
                // Don't throw error - this is a background operation
            }
        },

        /**
         * Save working time before page unload using the task service
         */
        async saveWorkingTimeBeforeUnload() {
            if (!this.currentTaskData || !this.currentProjectId || !this._shouldTrackTime()) {
                return;
            }

            try {
                const currentElapsedTime = this.timerInstance.getElapsedTime();
                const totalWorkingTime = this.lastSavedWorkingTime + currentElapsedTime;

                // Only save if there's been a meaningful change (at least 1 second)
                if (totalWorkingTime > this.currentTaskData.workingTimeMs + 1000) {
                    const success = await taskService.saveWorkingTimeBeforeUnload(
                        parseInt(this.currentProjectId),
                        this.currentTaskData.id,
                        totalWorkingTime
                    );

                    if (success) {
                        // Update the current task data with the new working time
                        this.currentTaskData.workingTimeMs = totalWorkingTime;
                        
                        // Update in the available tasks list as well
                        const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === this.currentTaskData?.id);
                        if (taskIndex !== -1) {
                            this.availableTasks[taskIndex].workingTimeMs = totalWorkingTime;
                        }
                    }
                }
            } catch (error) {
                logger.warn('Failed to save working time before unload:', error);
                // Don't throw error - this is a background operation during page unload
            }
        },

        /**
         * Common helper to save working time and preserve it across task status changes
         * @param statusChangeOperation - Function that performs the status change and returns the updated task
         * @param operationName - Name of the operation for logging (e.g., "completion", "suspension")
         */
        async _saveWorkingTimeAndChangeStatus<T extends Task>(
            statusChangeOperation: () => Promise<T>,
            operationName: string
        ): Promise<T> {
            // Calculate the final working time before the status change
            const currentElapsedTime = this.timerInstance.getElapsedTime();
            const finalWorkingTime = this.lastSavedWorkingTime + currentElapsedTime;
            
            // Save working time before the status change
            await this._autoSaveWorkingTime();
            
            // Perform the status change operation
            const updatedTask = await statusChangeOperation();
            
            // Pause timer since task status has changed to a non-active state
            this.pauseTimer();
            
            // Update the current task data and ensure working time is preserved
            this.currentTaskData = updatedTask;
            
            // Ensure the working time is preserved in case the backend didn't return the latest value
            if (this.currentTaskData.workingTimeMs < finalWorkingTime) {
                logger.warn(`Working time mismatch after ${operationName}. Backend: ${this.currentTaskData.workingTimeMs}ms, Expected: ${finalWorkingTime}ms. Updating...`);
                try {
                    const correctedTask = await taskService.updateWorkingTime(
                        parseInt(this.currentProjectId!), 
                        this.currentTaskData.id, 
                        finalWorkingTime
                    );
                    this.currentTaskData = correctedTask;
                } catch (error) {
                    logger.error(`Failed to correct working time after ${operationName}:`, error);
                    // At least update the local state
                    this.currentTaskData.workingTimeMs = finalWorkingTime;
                }
            }
            
            return updatedTask;
        },

        /**
         * Check if a task can be opened by the current user
         */
        async canOpenTask(task: Task): Promise<boolean> {
            // Use TaskManager to check if the task can be opened
            return await this.taskManager.canOpenTask(task);
        },

        /**
         * Navigate to the previous task in the current workflow stage
         */
        async navigateToPreviousTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || this.availableTasks.length === 0) {
                logger.warn('Cannot navigate to previous task: no current task or available tasks');
                return null;
            }

            // Use TaskNavigationManager to find previous task
            const result = await this.taskNavigationManager.navigateToPrevious(
                this.currentTaskData,
                this.availableTasks,
                this.currentProjectId!
            );

            if (!result.success) {
                logger.error('Failed to navigate to previous task:', result.error);
                return null;
            }

            return result.navigation;
        },

        /**
         * Navigate to the next task in the current workflow stage
         */
        async navigateToNextTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || this.availableTasks.length === 0) {
                logger.warn('Cannot navigate to next task: no current task or available tasks');
                return null;
            }

            // Use TaskNavigationManager to find next task
            const result = await this.taskNavigationManager.navigateToNext(
                this.currentTaskData,
                this.availableTasks,
                this.currentProjectId!
            );

            if (!result.success) {
                logger.error('Failed to navigate to next task:', result.error);
                return null;
            }

            return result.navigation;
        },

        /**
         * Complete current task and automatically move to next workflow stage using pipeline system
         */
        async completeAndMoveToNextTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot complete task: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);
                
                // Use TaskManager to complete the task
                const result = await this.taskManager.completeTask(numericProjectId, this.currentTaskData.id);
                
                if (!result.success) {
                    this.error = result.error || 'Failed to complete task';
                    return false;
                }
                
                // Update current task data
                if (result.task) {
                    this.currentTaskData = result.task;
                    
                    // Update the task in the available tasks list
                    const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === result.task!.id);
                    if (taskIndex !== -1) {
                        this.availableTasks[taskIndex] = result.task;
                    }
                }

                return true;
            } catch (error) {
                logger.error('Failed to complete task via TaskManager:', error);
                this.error = error instanceof Error ? error.message : 'Failed to complete task';
                return false;
            }
        },

        /**
         * Suspend the current task
         */
        async suspendCurrentTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot suspend task: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);

                // Use TaskManager to suspend the task with working time preservation
                const result = await this.taskManager.suspendTask(
                    numericProjectId, 
                    this.currentTaskData.id, 
                    this.lastSavedWorkingTime
                );
                
                if (!result.success) {
                    this.error = result.error || 'Failed to suspend task';
                    return false;
                }
                
                // Update current task data
                if (result.task) {
                    this.currentTaskData = result.task;
                    
                    // Update the task in the available tasks list
                    const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === result.task!.id);
                    if (taskIndex !== -1) {
                        this.availableTasks[taskIndex] = result.task;
                    }
                }
                
                return true;
            } catch (error) {
                logger.error('Failed to suspend task via TaskManager:', error);
                this.error = error instanceof Error ? error.message : 'Failed to suspend task';
                return false;
            }
        },

        /**
         * Defer the current task (skip for now)
         */
        async deferCurrentTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot defer task: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);
                
                // Use TaskManager to defer the task with working time preservation
                const result = await this.taskManager.deferTask(
                    numericProjectId, 
                    this.currentTaskData.id, 
                    this.lastSavedWorkingTime
                );
                
                if (!result.success) {
                    this.error = result.error || 'Failed to defer task';
                    return false;
                }
                
                // Update current task data
                if (result.task) {
                    this.currentTaskData = result.task;
                    
                    // Update the task in the available tasks list
                    const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === result.task!.id);
                    if (taskIndex !== -1) {
                        this.availableTasks[taskIndex] = result.task;
                    }
                }
                
                return true;
            } catch (error) {
                logger.error('Failed to defer task via TaskManager:', error);
                this.error = error instanceof Error ? error.message : 'Failed to defer task';
                return false;
            }
        },

        /**
         * Return the current task for rework (reviewers and managers)
         */
        async returnCurrentTaskForRework(reason?: string): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot return task for rework: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);
                
                // Use TaskManager to veto the task
                const result = await this.taskManager.vetoTask(
                    numericProjectId, 
                    this.currentTaskData.id, 
                    reason
                );
                
                if (!result.success) {
                    this.error = result.error || 'Failed to return task for rework';
                    return false;
                }
                
                // Update current task data
                if (result.task) {
                    this.currentTaskData = result.task;
                    
                    // Update the task in the available tasks list
                    const taskIndex = this.availableTasks.findIndex((task: Task) => task.id === result.task!.id);
                    if (taskIndex !== -1) {
                        this.availableTasks[taskIndex] = result.task;
                    }
                }

                return true;
            } catch (error) {
                logger.error('Failed to return task for rework via TaskManager:', error);
                this.error = error instanceof Error ? error.message : 'Failed to return task for rework';
                return false;
            }
        },

        /**
         * Get the next available task for seamless transitions
         */
        async getNextAvailableTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || this.availableTasks.length === 0) {
                logger.warn('Cannot get next task: no current task or available tasks');
                return null;
            }

            // Use TaskNavigationManager to find next available task
            const result = await this.taskNavigationManager.getNextAvailableTask(
                this.currentTaskData,
                this.availableTasks,
                this.currentProjectId!
            );

            if (!result.success) {
                logger.error('Failed to get next available task:', result.error);
                return null;
            }

            return result.navigation;
        },

        /**
         * Assign a task to the current user and set its status to IN_PROGRESS
         * This is used when auto-transitioning to the next task after completion
         */
        async assignAndStartNextTask(projectId: number, taskId: number): Promise<void> {
            try {
                // First assign the task to current user
                await taskService.assignTaskToCurrentUser(projectId, taskId);
                
                // Then change status to IN_PROGRESS if not already
                await taskService.changeTaskStatus(projectId, taskId, {
                    targetStatus: TaskStatus.IN_PROGRESS
                });
                
                logger.info(`Successfully assigned and started task ${taskId} for project ${projectId}`);
            } catch (error) {
                logger.error(`Failed to assign and start task ${taskId}:`, error);
                throw error;
            }
        },

    },
});
