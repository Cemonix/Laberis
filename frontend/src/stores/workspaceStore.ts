import { defineStore } from "pinia";
import { faArrowPointer, faDotCircle, faMinus, faWaveSquare, faSquare, faDrawPolygon } from '@fortawesome/free-solid-svg-icons';
import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { WorkspaceState } from "@/types/workspace/workspaceState";
import { Timer } from "@/utils/timer";
import type { Point } from "@/types/common/point";
import { ToolName, type Tool } from "@/types/workspace/tools";
import type { Annotation } from '@/types/workspace/annotation';
import type { LabelScheme } from '@/types/label/labelScheme';
import type { Label } from '@/types/label/label';
import annotationService from '@/services/api/annotationService';
import assetService from '@/services/api/assetService';
import { labelSchemeService } from '@/services/api/labelSchemeService';
import { labelService } from '@/services/api/labelService';
import { taskService } from '@/services/api/taskService';
import type { Asset } from '@/types/asset/asset';
import type { Task } from '@/types/task';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('WorkspaceStore');

const MIN_ZOOM = 0.1;
const MAX_ZOOM = 10.0;
const ZOOM_SENSITIVITY = 0.005;


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
        viewOffset: { x: 0, y: 0 },
        zoomLevel: 1.0,
        activeTool: ToolName.CURSOR,
        availableTools: [
            { id: ToolName.CURSOR, name: 'Cursor', iconDefinition: faArrowPointer },
            { id: ToolName.POINT, name: 'Point', iconDefinition: faDotCircle },
            { id: ToolName.BOUNDING_BOX, name: 'Bounding Box', iconDefinition: faSquare },
            { id: ToolName.LINE, name: 'Line', iconDefinition: faMinus },
            { id: ToolName.POLYLINE, name: 'Polyline', iconDefinition: faWaveSquare },
            { id: ToolName.POLYGON, name: 'Polygon', iconDefinition: faDrawPolygon },
        ],
        annotations: [] as Annotation[],
        currentLabelId: null as number | null,
        currentLabelScheme: null as LabelScheme | null,
        currentTaskId: null as number | null,
        currentTaskData: null as Task | null,
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
            return this.availableTools.find(tool => tool.id === this.activeTool);
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
                return state.currentLabelScheme.labels.find(label => label.labelId === labelId);
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
            return state.availableTasks.findIndex(task => task.id === state.currentTaskData?.id);
        },
        getTaskNavigationInfo(state): { current: number; total: number } {
            const currentIndex = state.currentTaskData && state.availableTasks.length > 0 
                ? state.availableTasks.findIndex(task => task.id === state.currentTaskData?.id)
                : -1;
            return {
                current: currentIndex >= 0 ? currentIndex + 1 : 0,
                total: state.availableTasks.length
            };
        },
        canNavigateToPrevious(state): boolean {
            if (!state.currentTaskData || state.availableTasks.length === 0) return false;
            const currentIndex = state.availableTasks.findIndex(task => task.id === state.currentTaskData?.id);
            return currentIndex > 0;
        },
        canNavigateToNext(state): boolean {
            if (!state.currentTaskData || state.availableTasks.length === 0) return false;
            const currentIndex = state.availableTasks.findIndex(task => task.id === state.currentTaskData?.id);
            return currentIndex < state.availableTasks.length - 1;
        },
    },

    actions: {
        async loadAsset(projectId: string, assetId: string, taskId?: string) {
            this.currentProjectId = projectId;
            this.currentAssetId = assetId;
            this.isLoading = true;
            this.error = null;

            try {
                logger.info(`Loading asset ${assetId} for project ${projectId}`);

                // Convert string IDs to numbers for API calls
                const numericProjectId = parseInt(projectId, 10);
                const numericAssetId = parseInt(assetId, 10);

                if (isNaN(numericProjectId) || isNaN(numericAssetId)) {
                    throw new Error('Invalid project ID or asset ID');
                }

                // Fetch asset data
                const assetData = await assetService.getAssetById(numericProjectId, numericAssetId);
                this.currentAssetData = assetData;
                this.currentImageUrl = assetData.imageUrl || null;

                logger.info(`Loaded asset data:`, assetData);

                // Reset workspace state
                this.imageNaturalDimensions = null;
                this.viewOffset = { x: 0, y: 0 };
                this.zoomLevel = 1;
                this.annotations = [];
                this.currentLabelId = null;
                this.currentTaskId = null;
                this.currentTaskData = null;
                this.availableTasks = [];
                this.initialTaskId = taskId ? parseInt(taskId, 10) : null;

                // Fetch annotations for this asset
                try {
                    const fetchedAnnotations = await annotationService.getAnnotationsForAsset(numericAssetId);
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
                            workflowStageId = currentTask.currentWorkflowStageId;
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
                            workflowStageId = currentTask.currentWorkflowStageId;
                            logger.info(`Found asset task ${currentTask.id} for asset ${assetId} in stage ${workflowStageId}`);
                        }
                    }

                    if (currentTask && workflowStageId) {
                        // Load all tasks for this workflow stage to enable navigation
                        const stageTasks = await taskService.getTasksForStage(numericProjectId, workflowStageId);
                        this.availableTasks = stageTasks.tasks;
                        this.currentTaskData = currentTask;
                        this.currentTaskId = currentTask.id;
                        
                        const taskIndex = this.availableTasks.findIndex(task => task.id === currentTask.id);
                        logger.info(`Loaded ${this.availableTasks.length} tasks for stage ${workflowStageId}, current task is at index ${taskIndex}`);
                    } else {
                        logger.warn(`No tasks found for asset ${assetId}`);
                    }
                } catch (taskError) {
                    logger.error("Failed to fetch tasks:", taskError);
                    // Don't fail the entire load process if tasks fail
                }

                // Fetch label schemes for this project
                try {
                    const labelSchemes = await labelSchemeService.getLabelSchemesForProject(numericProjectId);
                    if (labelSchemes.data.length > 0) {
                        // Use the first available label scheme, or look for a default one
                        const selectedScheme = labelSchemes.data.find(scheme => scheme.isDefault) || labelSchemes.data[0];
                        
                        // Fetch labels for the selected scheme
                        try {
                            const labelsResponse = await labelService.getLabelsForScheme(numericProjectId, selectedScheme.labelSchemeId);
                            selectedScheme.labels = labelsResponse.data;
                            this.setCurrentLabelScheme(selectedScheme);
                            logger.info(`Loaded label scheme: ${selectedScheme.name} with ${labelsResponse.data.length} labels`);
                        } catch (labelsError) {
                            logger.error("Failed to fetch labels for scheme:", labelsError);
                            this.setCurrentLabelScheme(selectedScheme); // Set scheme even without labels
                        }
                    } else {
                        logger.warn(`No label schemes found for project ${projectId}`);
                        this.setCurrentLabelScheme(null);
                    }
                } catch (labelSchemeError) {
                    logger.error("Failed to fetch label schemes:", labelSchemeError);
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
                this.currentLabelId = null;
                this.currentTaskId = null;
                this.currentTaskData = null;
                this.availableTasks = [];
                this.initialTaskId = null;
            }
        },

        setImageNaturalDimensions(dimensions: ImageDimensions) {
            this.imageNaturalDimensions = dimensions;
            console.log("[Store] Set image natural dimensions:", dimensions);
        },

        setCanvasDisplayDimensions(dimensions: ImageDimensions) {
            this.canvasDisplayDimensions = dimensions;
            console.log("[Store] Set canvas display dimensions:", dimensions);
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
                // Save annotation to backend
                const savedAnnotation = await annotationService.createAnnotation(annotation);

                // Update the annotation in the store with the saved data (including ID)
                // First try to find by clientId
                let index = this.annotations.findIndex(a => a.clientId === annotation.clientId);
                
                if (index !== -1) {
                    // Preserve the clientId when updating
                    this.annotations[index] = { ...savedAnnotation, clientId: annotation.clientId };
                    logger.debug(`Updated annotation by clientId: ${annotation.clientId}`);
                } else {
                    // If clientId doesn't match, find by coordinates and other properties
                    index = this.annotations.findIndex(a => 
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
                this.annotations = this.annotations.filter(a => a.clientId !== annotation.clientId);

                // Set error state
                this.error = error instanceof Error ? error.message : 'Failed to save annotation';
            }
        },

        /**
         * Updates an existing annotation
         */
        async updateAnnotation(annotationId: number, updates: Partial<Annotation>) {
            const index = this.annotations.findIndex(a => a.annotationId === annotationId);
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
                const updatedAnnotation = await annotationService.updateAnnotation(annotationId, updatePayload);

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
            const index = this.annotations.findIndex(a => a.annotationId === annotationId);
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
                await annotationService.deleteAnnotation(annotationId);

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
                console.log(`[Store] Selected Label ID: ${labelId} (${label?.name || 'Unknown'})`);
            } else {
                console.log("[Store] Label selection cleared.");
            }
        },

        /**
         * Sets the label scheme for the current context.
         * @param scheme The label scheme to set, or null to clear it.
         */
        setCurrentLabelScheme(scheme: LabelScheme | null) {
            this.currentLabelScheme = scheme;
            if (scheme) {
                console.log("[Store] Label Scheme set:", scheme.name);
            } else {
                console.log("[Store] Label Scheme cleared.");
            }
        },

        /**
         * Sets the current task ID.
         * @param taskId The ID of the current task.
         */
        setCurrentTaskId(taskId: number | null) {
            this.currentTaskId = taskId;
            if (taskId !== null) {
                console.log("[Store] Current Task ID set:", taskId);
            } else {
                console.log("[Store] Current Task ID cleared.");
            }
        },

        startTimer() {
            this._clearInterval();

            this.timerInstance.start();
            this._updateElapsedTimeDisplay();

            this.timerIntervalId = window.setInterval(() => {
                this._updateElapsedTimeDisplay();
            }, 1000);
        },

        pauseTimer() {
            if (this.timerInstance.isRunning) {
                this.timerInstance.pause();
                this._clearInterval();
                this._updateElapsedTimeDisplay();
            }
        },

        stopAndResetTimer() {
            this.timerInstance.stop();
            this.timerInstance.reset();
            this._clearInterval();
            this.elapsedTimeDisplay = "00:00:00";
        },
        
        cleanupTimer() {
            this._clearInterval();

            // TODO:
            // We might not want to stop/reset the timer itself here
            // if the user might return to the same asset.
            // loadAsset will handle resetting for a *new* asset.
            // For now, we will stop it.
            this.timerInstance.stop();
        },

        setViewOffset(offset: Point) {
            this.viewOffset = offset;
        },

        setZoomLevel(level: number) {
            this.zoomLevel = Math.max(MIN_ZOOM, Math.min(level, MAX_ZOOM));
        },
        
        setActiveTool(toolId: ToolName) {
            const toolExists = this.availableTools.some(tool => tool.id === toolId);
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
            this.elapsedTimeDisplay = this.timerInstance.getFormattedElapsedTime();
        },

        /**
         * Navigate to the previous task in the current workflow stage
         */
        async navigateToPreviousTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || this.availableTasks.length === 0) {
                logger.warn('Cannot navigate to previous task: no current task or available tasks');
                return null;
            }

            const currentIndex = this.availableTasks.findIndex(task => task.id === this.currentTaskData?.id);
            if (currentIndex <= 0) {
                logger.info('Already at first task, cannot go to previous');
                return null; // Already at first task
            }

            const previousTask = this.availableTasks[currentIndex - 1];
            logger.info(`Navigating to previous task: ${previousTask.id} (asset ${previousTask.assetId})`);
            
            return {
                projectId: this.currentProjectId!,
                assetId: previousTask.assetId.toString(),
                taskId: previousTask.id.toString()
            };
        },

        /**
         * Navigate to the next task in the current workflow stage
         */
        async navigateToNextTask(): Promise<{ projectId: string; assetId: string; taskId: string } | null> {
            if (!this.currentTaskData || this.availableTasks.length === 0) {
                logger.warn('Cannot navigate to next task: no current task or available tasks');
                return null;
            }

            const currentIndex = this.availableTasks.findIndex(task => task.id === this.currentTaskData?.id);
            if (currentIndex >= this.availableTasks.length - 1) {
                logger.info('Already at last task, cannot go to next');
                return null; // Already at last task
            }

            const nextTask = this.availableTasks[currentIndex + 1];
            logger.info(`Navigating to next task: ${nextTask.id} (asset ${nextTask.assetId})`);
            
            return {
                projectId: this.currentProjectId!,
                assetId: nextTask.assetId.toString(),
                taskId: nextTask.id.toString()
            };
        },

    },
});
