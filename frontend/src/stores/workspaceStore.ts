import { defineStore } from "pinia";
import type { ImageDimensions } from "@/core/asset/asset.types";
import type { WorkspaceState, TaskNavigationContext } from "./workspaceStore.types";
import type { Point } from "@/core/geometry/geometry.types";
import { ToolName, type Tool } from "@/core/workspace/tools.types";
import type { Annotation } from '@/core/workspace/annotation.types';
import type { LabelScheme, Label } from '@/services/project/labelScheme/label.types';
import { TimeTracker } from '@/core/timeTracking';
import { workspaceLoader } from '@/core/workspace/loader/workspaceLoader';
import { taskManager } from '@/core/workspace/task/taskManager';
import { annotationDataManager } from '@/core/workspace/data/annotationDataManager';
import { viewportManager } from '@/core/workspace/ui/viewportManager';
import { toolManager } from '@/core/workspace/ui/toolManager';
import { taskService } from '@/services/project';
import type { Task } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';
import { AppLogger } from '@/core/logger/logger';
import { WorkflowStageType } from '@/services/project/workflow/workflowStage.types';

const logger = AppLogger.createServiceLogger('WorkspaceStore');


// Core managers
const timeTracker = new TimeTracker();


export const useWorkspaceStore = defineStore("workspace", {
    state: (): WorkspaceState => ({
        currentProjectId: null,
        currentAssetId: null,
        currentAssetData: null,
        currentImageUrl: null,
        imageNaturalDimensions: null,
        canvasDisplayDimensions: null,
        elapsedTimeDisplay: "00:00:00",
        timerIntervalId: null,
        lastSavedWorkingTime: 0,
        viewOffset: { x: 0, y: 0 },
        zoomLevel: 1.0,
        activeTool: ToolName.CURSOR,
        availableTools: toolManager.getAvailableTools(),
        annotations: [] as Annotation[],
        currentLabelId: null as number | null,
        currentLabelScheme: null as LabelScheme | null,
        availableLabelSchemes: [] as LabelScheme[],
        currentTaskId: null as number | null,
        currentTaskData: null as Task | null,
        currentWorkflowStageType: null as WorkflowStageType | null,
        initialTaskId: null as number | null, // Store the initial task ID from URL query
        navigationContext: null as TaskNavigationContext | null,
        isLoading: false,
        error: null,
    }),

    getters: {
        getCurrentImageAspectRatio(): number | null {
            return viewportManager.calculateImageAspectRatio(this.imageNaturalDimensions);
        },
        getZoomConfig() {
            return viewportManager.getZoomConfig();
        },
        getActiveToolDetails(): Tool | undefined {
            return toolManager.getToolById(this.activeTool);
        },
        getLabelById(state): (labelId: number) => Label | undefined {
            return (labelId: number) => {
                if (!state.currentLabelScheme || !state.currentLabelScheme.labels) {
                    return undefined;
                }
                return state.currentLabelScheme.labels.find((label: Label) => label.labelId === labelId);
            };
        },
        // Simple state getters - for backward compatibility with components
        getAnnotations(state): Annotation[] {
            return state.annotations;
        },
        getSelectedLabelId(state): number | null {
            return state.currentLabelId;
        },
        getCurrentAsset(state) {
            return state.currentAssetData;
        },
        getLoadingState(state): boolean {
            return state.isLoading;
        },
        getError(state): string | null {
            return state.error;
        },
        getCurrentTask(state): Task | null {
            return state.currentTaskData;
        },
        getCurrentLabelScheme(state): LabelScheme | null {
            return state.currentLabelScheme;
        },
        getAvailableLabels(state): Label[] {
            return state.currentLabelScheme?.labels || [];
        },
        getTaskNavigationInfo(): { current: number; total: number } {
            // Simple navigation info from server context
            if (this.navigationContext) {
                return {
                    current: this.navigationContext.currentPosition,
                    total: this.navigationContext.totalTasks
                };
            }
            
            return { current: 0, total: 0 };
        },
        isTaskCompleted(state): boolean {
            return taskManager.isTaskCompleted(state.currentTaskData);
        },
        isAnnotationEditingDisabled(): boolean {
            // Annotation editing is disabled when task is completed (preview mode)
            return this.isTaskCompleted;
        },
        canNavigateToPrevious(): boolean {
            return this.navigationContext?.hasPrevious ?? false;
        },
        canNavigateToNext(): boolean {
            return this.navigationContext?.hasNext ?? false;
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
                return annotationDataManager.hasAnnotationsForAsset(state.annotations, state.currentAssetData.id);
            } else {
                // In annotation stages, check annotations for this specific task
                return annotationDataManager.hasAnnotationsForTask(state.annotations, state.currentTaskData.id);
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

    },

    actions: {
        /**
         * Initialize workspace with clean state
         */
        initializeWorkspaceState(projectId: string, assetId: string, taskId?: string) {
            this.currentProjectId = projectId;
            this.currentAssetId = assetId;
            this.isLoading = true;
            this.error = null;
            
            // Reset UI state to defaults
            const initialViewport = viewportManager.getInitialViewportState();
            this.viewOffset = initialViewport.viewOffset;
            this.zoomLevel = initialViewport.zoomLevel;
            
            // Clear data state
            this.annotations = [];
            this.currentLabelId = null;
            this.currentTaskId = null;
            this.currentTaskData = null;
            this.currentWorkflowStageType = null;
            this.initialTaskId = taskId ? parseInt(taskId, 10) : null;
            this.navigationContext = null;
            
            // Clear asset state
            this.currentAssetData = null;
            this.currentImageUrl = null;
            this.imageNaturalDimensions = null;
            this.currentLabelScheme = null;
            this.availableLabelSchemes = [];
        },

        /**
         * Load asset data using WorkspaceLoader
         */
        async loadAssetData(projectId: string, assetId: string): Promise<boolean> {
            try {
                const assetData = await workspaceLoader.loadAssetData(projectId, assetId);
                
                if (!assetData) {
                    this.error = 'Failed to load asset data';
                    return false;
                }

                // Set asset data
                this.currentAssetData = assetData.asset;
                this.currentImageUrl = assetData.imageUrl;
                this.imageNaturalDimensions = assetData.naturalDimensions;

                logger.info(`Successfully loaded asset data for ${assetId}`);
                return true;
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to load asset data';
                logger.error(`Failed to load asset data for ${assetId}:`, error);
                this.error = errorMessage;
                return false;
            }
        },

        /**
         * Load annotations for current asset
         */
        async loadAnnotations(): Promise<void> {
            if (!this.currentProjectId || !this.currentAssetId) return;
            
            const numericProjectId = parseInt(this.currentProjectId, 10);
            const numericAssetId = parseInt(this.currentAssetId, 10);

            const annotations = await workspaceLoader.loadAnnotationsForAsset(numericProjectId, numericAssetId);
            this.setAnnotations(annotations);
            logger.info(`Loaded ${annotations.length} annotations for asset ${this.currentAssetId}`);
        },

        /**
         * Load tasks and navigation context for current asset
         */
        async loadTasks(): Promise<void> {
            if (!this.currentProjectId || !this.currentAssetId) return;
            
            const numericProjectId = parseInt(this.currentProjectId, 10);
            const numericAssetId = parseInt(this.currentAssetId, 10);

            try {
                const tasksData = await workspaceLoader.loadTasksForAsset(
                    numericProjectId, 
                    numericAssetId, 
                    this.initialTaskId || undefined
                );

                // Update task state
                this.currentTaskData = tasksData.currentTask;
                this.currentTaskId = tasksData.currentTask?.id || null;
                this.currentWorkflowStageType = tasksData.workflowStageType;
                this.navigationContext = tasksData.navigationContext;

                if (tasksData.currentTask) {
                    logger.info(`Loaded task ${tasksData.currentTask.id}`);
                }
            } catch (error) {
                // Check if this is a task access restriction error from workspaceLoader
                logger.error('Failed to load tasks:', error);
                throw error; // Re-throw the error so it can be caught by loadAsset
            }
        },

        /**
         * Load label scheme for current workflow
         */
        async loadLabelScheme(): Promise<void> {
            if (!this.currentProjectId || !this.currentTaskData) return;
            
            const numericProjectId = parseInt(this.currentProjectId, 10);

            const labelSchemeData = await workspaceLoader.loadLabelScheme(numericProjectId, this.currentTaskData);

            // Update label scheme state
            this.currentLabelScheme = labelSchemeData.labelScheme;
            this.availableLabelSchemes = labelSchemeData.availableSchemes;

            if (labelSchemeData.labelScheme) {
                logger.info(`Loaded label scheme: ${labelSchemeData.labelScheme.name}`);
            }
        },

        /**
         * Complete workspace initialization
         */
        finalizeWorkspaceLoad(): void {
            // Start timer and set cursor tool
            this.startTimer();
            this.setActiveTool(toolManager.getDefaultTool());
            this.isLoading = false;
        },

        /**
         * Handle workspace load error
         */
        handleWorkspaceLoadError(error: unknown, assetId: string): void {
            this.isLoading = false;
            this.error = error instanceof Error ? error.message : 'Failed to load asset';
            logger.error(`Failed to load asset ${assetId}:`, error);
            
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
            this.initialTaskId = null;
            this.navigationContext = null;
        },

        /**
         * Main entry point for loading workspace data
         * Orchestrates the loading of asset, annotations, tasks, and labels
         */
        async loadAsset(projectId: string, assetId: string, taskId?: string) {
            logger.info(`Loading asset ${assetId} for project ${projectId}`);

            try {
                // Step 1: Initialize clean state
                this.initializeWorkspaceState(projectId, assetId, taskId);

                // Step 2: Load asset data
                const assetLoaded = await this.loadAssetData(projectId, assetId);
                if (!assetLoaded) {
                    this.handleWorkspaceLoadError(new Error('Failed to load asset data'), assetId);
                    return;
                }

                // Step 3: Load all workspace data in parallel
                const results = await Promise.allSettled([
                    this.loadAnnotations(),
                    this.loadTasks()
                ]);

                // Check if task loading failed due to access restrictions
                const taskLoadResult = results[1];
                if (taskLoadResult.status === 'rejected') {
                    const error = taskLoadResult.reason;
                    // Check if this is a task access restriction error that should redirect to alternative task
                    if (error?.shouldRedirectToTask) {
                        const redirectError = new Error(error.error || 'Task assigned to another user');
                        (redirectError as any).redirectToTask = error.shouldRedirectToTask;
                        (redirectError as any).alternativeTask = error.alternativeTask;
                        (redirectError as any).toastMessage = error.toastMessage || 'This task was assigned to another user. We found you an alternative task.';
                        throw redirectError;
                    }
                    
                    // Check if this is a task access restriction error that should redirect to task view
                    if (error?.shouldRedirectToTaskView) {
                        const redirectError = new Error(error.error || 'Task access denied');
                        (redirectError as any).redirectToTaskView = true;
                        (redirectError as any).noTasksAvailable = error.noTasksAvailable === true;
                        (redirectError as any).toastMessage = error.toastMessage || (
                            error.noTasksAvailable ? 'No tasks available to work on right now.' : 'You cannot access this task.'
                        );
                        throw redirectError;
                    }
                    // For other task loading errors, log but continue
                    logger.warn('Task loading failed, but continuing with workspace load:', error);
                }

                // Step 4: Load label scheme (depends on tasks being loaded first)
                await this.loadLabelScheme();

                // Step 5: Finalize workspace
                this.finalizeWorkspaceLoad();
                
                logger.info(`Successfully loaded asset ${assetId} for project ${projectId}`);
            } catch (error) {
                // Check if this is a redirect error and re-throw it
                if ((error as any)?.redirectToTaskView || (error as any)?.redirectToTask) {
                    throw error;
                }
                this.handleWorkspaceLoadError(error, assetId);
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
            if (!this.currentProjectId) {
                this.error = 'No current project';
                return;
            }

            // Add annotation to the store immediately for optimistic updates
            this.annotations = annotationDataManager.addAnnotationToList(this.annotations, annotation);

            try {
                // Save annotation to backend using annotationDataManager
                const result = await annotationDataManager.createAnnotation(
                    parseInt(this.currentProjectId), 
                    annotation
                );

                if (!result.success) {
                    throw new Error(result.error);
                }

                const savedAnnotation = result.data;

                // Update the annotation in the store with the saved data
                let index = annotationDataManager.findAnnotationByClientId(this.annotations, annotation.clientId!);

                if (index !== -1) {
                    // Update by clientId
                    this.annotations = annotationDataManager.updateAnnotationInList(
                        this.annotations, 
                        index, 
                        savedAnnotation, 
                        annotation.clientId
                    );
                    logger.debug(`Updated annotation by clientId: ${annotation.clientId}`);
                } else {
                    // Try finding by properties as fallback
                    index = annotationDataManager.findAnnotationByProperties(this.annotations, annotation);
                    
                    if (index !== -1) {
                        this.annotations = annotationDataManager.updateAnnotationInList(
                            this.annotations, 
                            index, 
                            savedAnnotation, 
                            this.annotations[index].clientId
                        );
                        logger.debug(`Updated annotation by properties match`);
                    } else {
                        logger.warn(`Could not find matching annotation to update after save`);
                        // Add as new annotation if we can't find a match
                        this.annotations = annotationDataManager.addAnnotationToList(
                            this.annotations, 
                            { ...savedAnnotation, clientId: annotation.clientId }
                        );
                    }
                }

                logger.info(`Successfully saved annotation with ID: ${savedAnnotation.annotationId}`);

            } catch (error) {
                logger.error("Failed to save annotation:", error);

                // Remove the annotation from the store on failure
                this.annotations = annotationDataManager.removeAnnotationByClientId(
                    this.annotations, 
                    annotation.clientId!
                );

                // Set error state
                this.error = error instanceof Error ? error.message : 'Failed to save annotation';
            }
        },

        /**
         * Updates an existing annotation
         */
        async updateAnnotation(annotationId: number, updates: Partial<Annotation>) {
            if (!this.currentProjectId) {
                this.error = 'No current project';
                return;
            }

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
                // Update annotation on backend using annotationDataManager
                const result = await annotationDataManager.updateAnnotation(
                    parseInt(this.currentProjectId), 
                    annotationId, 
                    updates
                );

                if (!result.success) {
                    throw new Error(result.error);
                }

                // Update the annotation in the store with the response from backend
                this.annotations[index] = result.data;

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
            if (!this.currentProjectId) {
                this.error = 'No current project';
                return;
            }

            const index = this.annotations.findIndex((a: Annotation) => a.annotationId === annotationId);
            if (index === -1) {
                logger.error(`Annotation with ID ${annotationId} not found in store`);
                return;
            }

            // Store annotation for rollback
            const deletedAnnotation = this.annotations[index];
            
            // Optimistically remove the annotation from the store
            this.annotations = annotationDataManager.removeAnnotationById(this.annotations, annotationId);

            try {
                // Delete annotation on backend using annotationDataManager
                const result = await annotationDataManager.deleteAnnotation(
                    parseInt(this.currentProjectId), 
                    annotationId
                );

                if (!result.success) {
                    throw new Error(result.error);
                }

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
                // Don't start the timer - just display the saved time
                this._updateElapsedTimeDisplay();
                logger.info('Task is completed/archived - displaying saved working time only');
                return;
            }
            
            // For active tasks, use the persistent time tracker
            if (this.currentTaskData) {
                timeTracker.startTracking(
                    this.currentTaskData.id,
                    this.currentTaskData.workingTimeMs || 0,
                    async (taskId: number, totalTimeMs: number) => {
                        // Sync callback - save to server
                        if (this.currentProjectId) {
                            try {
                                await taskService.updateWorkingTime(
                                    parseInt(this.currentProjectId),
                                    taskId,
                                    totalTimeMs
                                );
                                // Update local state
                                if (this.currentTaskData && this.currentTaskData.id === taskId) {
                                    this.currentTaskData.workingTimeMs = totalTimeMs;
                                }
                            } catch (error) {
                                logger.warn('Failed to sync working time:', error);
                                throw error; // Let the time tracker handle the failure
                            }
                        }
                    }
                );

                // Update display every second
                this.timerIntervalId = window.setInterval(() => {
                    this._updateElapsedTimeDisplay();
                }, 1000);
            }
        },

        pauseTimer() {
            this._clearInterval();
        },

        stopAndResetTimer() {
            this._clearInterval();
            timeTracker.stopTracking();
            this.lastSavedWorkingTime = 0;
            this.elapsedTimeDisplay = "00:00:00";
        },
        
        cleanupTimer() {
            this._clearInterval();
            // Stop the persistent time tracker (which will save state to localStorage)
            timeTracker.stopTracking();
        },

        setViewOffset(offset: Point) {
            this.viewOffset = viewportManager.clampOffset(offset);
        },

        setZoomLevel(level: number) {
            this.zoomLevel = viewportManager.clampZoom(level);
        },
        
        setActiveTool(toolId: ToolName) {
            this.activeTool = toolManager.validateAndSetTool(toolId);
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
                this.elapsedTimeDisplay = timeTracker.getFormattedElapsedTime(this.lastSavedWorkingTime);
                return;
            }
            
            // For active tasks, use the time tracker
            if (this.currentTaskData) {
                this.elapsedTimeDisplay = timeTracker.getFormattedElapsedTime();
            }
        },


        _shouldTrackTime(): boolean {
            return taskManager.shouldTrackTime(this.currentTaskData);
        },


        /**
         * Save working time before page unload using the task service
         */
        async saveWorkingTimeBeforeUnload() {
            if (!this.currentTaskData || !this.currentProjectId || !this._shouldTrackTime()) {
                return;
            }

            try {
                // Use the persistent time tracker's save method which is more reliable
                await timeTracker.saveForUnload();
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
            // Get current working time from persistent tracker
            const finalWorkingTime = timeTracker.getCurrentTime();
            
            
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
            return taskManager.canOpenTask(task);
        },

        /**
         * Navigate to the previous task in the current workflow stage
         */
        async navigateToPreviousTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.warn('Cannot navigate to previous task: no current task or project');
                return null;
            }

            const result = await taskManager.navigateToPreviousTask(
                parseInt(this.currentProjectId), 
                this.currentTaskData.id,
                this.currentTaskData.workflowStageId
            );

            if (!result.success) {
                logger.error('Failed to navigate to previous task:', result.error);
                return null;
            }

            const navigation = result.data;

            if (navigation.taskId && navigation.assetId) {
                logger.info('Successfully navigated to previous task:', navigation);
                // Update navigation context
                this.navigationContext = {
                    hasNext: navigation.hasNext,
                    hasPrevious: navigation.hasPrevious,
                    currentPosition: navigation.currentPosition,
                    totalTasks: navigation.totalTasks,
                    message: navigation.message,
                    availableTasks: navigation.availableTasks,
                    completedTasks: navigation.completedTasks,
                    assignedTasks: navigation.assignedTasks
                };
                
                return {
                    projectId: this.currentProjectId,
                    assetId: navigation.assetId.toString(),
                    taskId: navigation.taskId.toString()
                };
            }

            logger.info('No previous task available');
            return null;
        },

        /**
         * Navigate to the next task in the current workflow stage
         */
        async navigateToNextTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.warn('Cannot navigate to next task: no current task or project');
                return null;
            }

            const result = await taskManager.navigateToNextTask(
                parseInt(this.currentProjectId), 
                this.currentTaskData.id,
                this.currentTaskData.workflowStageId
            );

            if (!result.success) {
                logger.error('Failed to navigate to next task:', result.error);
                return null;
            }

            const navigation = result.data;

            if (navigation.taskId && navigation.assetId) {
                logger.info('Successfully navigated to next task:', navigation);
                // Update navigation context
                this.navigationContext = {
                    hasNext: navigation.hasNext,
                    hasPrevious: navigation.hasPrevious,
                    currentPosition: navigation.currentPosition,
                    totalTasks: navigation.totalTasks,
                    message: navigation.message,
                    availableTasks: navigation.availableTasks,
                    completedTasks: navigation.completedTasks,
                    assignedTasks: navigation.assignedTasks
                };
                
                return {
                    projectId: this.currentProjectId,
                    assetId: navigation.assetId.toString(),
                    taskId: navigation.taskId.toString()
                };
            }

            logger.info('No next task available');
            return null;
        },

        /**
         * Complete current task and automatically move to next workflow stage using pipeline system
         */
        async completeAndMoveToNextTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot complete task: no current task or project');
                return false;
            }

            const result = await taskManager.completeTaskPipeline(
                parseInt(this.currentProjectId), 
                this.currentTaskData.id
            );

            if (!result.success) {
                this.error = result.error;
                return false;
            }

            const pipelineResult = result.data;
            
            if (!pipelineResult.isSuccess) {
                this.error = pipelineResult.errorMessage || 'Failed to complete task';
                return false;
            }
            
            // Update current task data
            if (pipelineResult.updatedTask) {
                this.currentTaskData = pipelineResult.updatedTask;
            }

            return true;
        },

        /**
         * Suspend the current task
         */
        async suspendCurrentTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot suspend task: no current task or project');
                return false;
            }

            const numericProjectId = parseInt(this.currentProjectId);

            // Update working time before suspending
            await taskManager.updateWorkingTime(numericProjectId, this.currentTaskData.id, this.lastSavedWorkingTime);
            
            // Suspend the task using taskManager
            const result = await taskManager.suspendTask(numericProjectId, this.currentTaskData.id);
            
            if (!result.success) {
                this.error = result.error;
                return false;
            }
            
            // Update current task data
            this.currentTaskData = result.data;
            
            return true;
        },

        /**
         * Defer the current task (skip for now)
         */
        async deferCurrentTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot defer task: no current task or project');
                return false;
            }

            const numericProjectId = parseInt(this.currentProjectId);
            
            // Update working time before deferring
            await taskManager.updateWorkingTime(numericProjectId, this.currentTaskData.id, this.lastSavedWorkingTime);
            
            // Defer the task using taskManager
            const result = await taskManager.deferTask(numericProjectId, this.currentTaskData.id);
            
            if (!result.success) {
                this.error = result.error;
                return false;
            }
            
            // Update current task data
            this.currentTaskData = result.data;
            
            return true;
        },

        /**
         * Return the current task for rework (reviewers and managers)
         */
        async returnCurrentTaskForRework(reason?: string): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot return task for rework: no current task or project');
                return false;
            }

            const result = await taskManager.vetoTaskPipeline(
                parseInt(this.currentProjectId), 
                this.currentTaskData.id, 
                reason
            );
            
            if (!result.success) {
                this.error = result.error;
                return false;
            }

            const pipelineResult = result.data;
            
            if (!pipelineResult.isSuccess) {
                this.error = pipelineResult.errorMessage || 'Failed to return task for rework';
                return false;
            }
            
            // Update current task data
            if (pipelineResult.updatedTask) {
                this.currentTaskData = pipelineResult.updatedTask;
            }

            return true;
        },

        /**
         * Get the next available task for seamless transitions
         */
        async getNextAvailableTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            // Simply delegate to navigateToNextTask - they do the same thing
            return this.navigateToNextTask();
        },

        /**
         * Assign a task to the current user and set its status to IN_PROGRESS
         * This is used when auto-transitioning to the next task after completion
         */
        async assignAndStartNextTask(projectId: number, taskId: number): Promise<void> {
            const result = await taskManager.assignAndStartTask(projectId, taskId);
            
            if (!result.success) {
                throw new Error(result.error);
            }
        },

        /**
         * Clean up the store and persistent time tracker
         */
        destroy() {
            this.cleanupTimer();
            timeTracker.destroy();
        },

    },
});
