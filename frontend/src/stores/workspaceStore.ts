import { defineStore } from "pinia";
import { faArrowPointer, faDotCircle, faMinus, faWaveSquare, faSquare, faDrawPolygon } from '@fortawesome/free-solid-svg-icons';
import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { WorkspaceState } from "@/types/workspace/workspaceState";
import { Timer } from "@/utils/timer";
import type { Point } from "@/types/common/point";
import { ToolName, type Tool } from "@/types/workspace/tools";
import type { Annotation, CreateAnnotationDto } from '@/types/workspace/annotation';
import type { LabelScheme } from '@/types/label/labelScheme';
import type { Label } from '@/types/label/label';
import { 
    annotationService,
    assetService, 
    labelSchemeService,
    labelService,
    taskService,
    workflowService,
    workflowStageService
} from '@/services/api/projects';
import { taskStatusService } from '@/services/taskStatusService';
import type { Asset } from '@/types/asset/asset';
import type { Task } from '@/types/task';
import { TaskStatus } from '@/types/task';
import { AppLogger } from '@/utils/logger';
import { WorkflowStageType } from '@/types/workflow/workflowstage';

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
        isTaskCompleted(state): boolean {
            return state.currentTaskData?.completedAt !== null && state.currentTaskData?.completedAt !== undefined;
        },
        isAnnotationEditingDisabled(): boolean {
            // Annotation editing is disabled when task is completed (preview mode)
            return this.isTaskCompleted;
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
        canCompleteCurrentTask(state): boolean {
            if (!state.currentTaskData || !state.currentAssetData) {
                return false;
            }

            // Check if task is already completed
            if (state.currentTaskData.completedAt) {
                return false;
            }

            // For revision and completion stages, check annotations for the asset (not just the current task)
            // For annotation stages, check annotations for the specific task
            if (state.currentWorkflowStageType === WorkflowStageType.REVISION || 
                state.currentWorkflowStageType === WorkflowStageType.COMPLETION) {
                // In revision/completion stages, check if there are annotations for this asset
                // (created in previous annotation stages)
                const assetAnnotations = state.annotations.filter(annotation => 
                    annotation.assetId === state.currentAssetData?.id
                );
                return assetAnnotations.length > 0;
            } else {
                // In annotation stages, check annotations for this specific task
                const taskAnnotations = state.annotations.filter(annotation => 
                    annotation.taskId === state.currentTaskData?.id
                );
                return taskAnnotations.length > 0;
            }
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
                            workflowStageId = currentTask?.currentWorkflowStageId || null;
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
                        
                        // Auto-assign task to current user if not already assigned (makes it IN_PROGRESS)
                        try {
                            // If task is suspended, resume it first before assignment
                            if (currentTask.status === 'SUSPENDED') {
                                logger.info(`Task ${currentTask.id} is suspended, resuming it before assignment`);
                                const resumedTask = await taskStatusService.resumeTask(numericProjectId, currentTask, false);
                                
                                // Update current task data with resumed task
                                currentTask = resumedTask;
                                this.currentTaskData = resumedTask;
                                
                                // Update the task in the available tasks list
                                const taskIndex = this.availableTasks.findIndex(task => task.id === resumedTask.id);
                                if (taskIndex !== -1) {
                                    this.availableTasks[taskIndex] = resumedTask;
                                }
                                
                                logger.info(`Successfully resumed suspended task ${currentTask.id}`);
                            }
                            
                            if (!currentTask.assignedToEmail) {
                                const { useAuthStore } = await import('@/stores/authStore');
                                const authStore = useAuthStore();
                                
                                if (authStore.currentUser?.email) {
                                    logger.info(`Auto-assigning task ${currentTask.id} to current user ${authStore.currentUser.email}`);
                                    const assignedTask = await taskService.assignTaskToCurrentUser(numericProjectId, currentTask.id);
                                    
                                    // Update the current task data with the assigned task
                                    this.currentTaskData = assignedTask;
                                    
                                    // Update the task in the available tasks list
                                    const taskIndex = this.availableTasks.findIndex(task => task.id === assignedTask.id);
                                    if (taskIndex !== -1) {
                                        this.availableTasks[taskIndex] = assignedTask;
                                    }
                                    
                                    logger.info(`Successfully auto-assigned task ${currentTask.id} to ${authStore.currentUser.email} - status should now be IN_PROGRESS`);
                                } else {
                                    logger.warn('Cannot auto-assign task: no current user email available');
                                }
                            } else {
                                logger.info(`Task ${currentTask.id} is already assigned to ${currentTask.assignedToEmail}`);
                            }
                        } catch (assignError) {
                            logger.warn('Failed to auto-assign task to current user:', assignError);
                            // Don't fail the load process if assignment fails
                        }
                        
                        const taskIndex = this.availableTasks.findIndex(task => task.id === this.currentTaskData?.id);
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
                    isGroundTruth: annotation.isGroundTruth || false,
                    version: annotation.version || 1,
                    notes: annotation.notes,
                    annotatorEmail: annotation.annotatorEmail,
                    parentAnnotationId: annotation.parentAnnotationId
                };

                // Save annotation to backend
                const savedAnnotation = await annotationService.createAnnotation(parseInt(this.currentProjectId!), createDto);

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
                const updatedAnnotation = await annotationService.updateAnnotation(parseInt(this.currentProjectId!), annotationId, updatePayload);

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
                await annotationService.deleteAnnotation(parseInt(this.currentProjectId!), annotationId);

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
         * Switches to a different label scheme by ID
         * @param schemeId The ID of the label scheme to switch to
         */
        async switchLabelScheme(schemeId: number) {
            try {
                // Find the scheme in available schemes
                const scheme = this.availableLabelSchemes.find(s => s.labelSchemeId === schemeId);
                if (!scheme) {
                    logger.error(`Label scheme with ID ${schemeId} not found in available schemes`);
                    return;
                }

                // If the scheme doesn't have labels loaded, fetch them
                if (!scheme.labels && this.currentProjectId) {
                    const numericProjectId = parseInt(this.currentProjectId);
                    try {
                        const labelsResponse = await labelService.getLabelsForScheme(numericProjectId, schemeId);
                        scheme.labels = labelsResponse.data;
                        logger.info(`Loaded ${labelsResponse.data.length} labels for scheme: ${scheme.name}`);
                    } catch (labelsError) {
                        logger.error("Failed to fetch labels for scheme:", labelsError);
                        // Continue anyway, set scheme without labels
                    }
                }

                // Clear current label selection when switching schemes
                this.setCurrentLabelId(null);
                
                // Set the new scheme
                this.setCurrentLabelScheme(scheme);
                logger.info(`Switched to label scheme: ${scheme.name}`);
            } catch (error) {
                logger.error("Failed to switch label scheme:", error);
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
         * Check if a task can be opened by the current user
         */
        async canOpenTask(task: Task): Promise<boolean> {
            // Import permission check dynamically to avoid circular dependencies
            const { usePermissions } = await import('@/composables/usePermissions');
            const { canUpdateProject } = usePermissions();
            
            // Deferred tasks can only be opened by managers
            if (task.status === TaskStatus.DEFERRED && !canUpdateProject.value) {
                return false;
            }
            
            // Completed and archived tasks cannot be opened (suspended tasks can be resumed)
            if (task.status && [TaskStatus.COMPLETED, TaskStatus.ARCHIVED].includes(task.status)) {
                return false;
            }
            
            return true;
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
            
            // Look for the previous available task that can be opened
            for (let i = currentIndex - 1; i >= 0; i--) {
                const previousTask = this.availableTasks[i];
                const canOpen = await this.canOpenTask(previousTask);
                
                if (canOpen) {
                    logger.info(`Navigating to previous task: ${previousTask.id} (asset ${previousTask.assetId})`);
                    return {
                        projectId: this.currentProjectId!,
                        assetId: previousTask.assetId.toString(),
                        taskId: previousTask.id.toString()
                    };
                } else {
                    logger.debug(`Skipping previous task ${previousTask.id} - status: ${previousTask.status}`);
                }
            }
            
            logger.info('No accessible previous task found');
            return null;
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
            
            // Look for the next available task that can be opened
            for (let i = currentIndex + 1; i < this.availableTasks.length; i++) {
                const nextTask = this.availableTasks[i];
                const canOpen = await this.canOpenTask(nextTask);
                
                if (canOpen) {
                    logger.info(`Navigating to next task: ${nextTask.id} (asset ${nextTask.assetId})`);
                    return {
                        projectId: this.currentProjectId!,
                        assetId: nextTask.assetId.toString(),
                        taskId: nextTask.id.toString()
                    };
                } else {
                    logger.debug(`Skipping next task ${nextTask.id} - status: ${nextTask.status}`);
                }
            }
            
            logger.info('No accessible next task found');
            return null;
        },

        /**
         * Complete the current task (annotation workflow)
         */
        async completeCurrentTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot complete task: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);
                
                // Check if user has manager permissions for completion stage tasks
                const { usePermissions } = await import('@/composables/usePermissions');
                const { canUpdateProject } = usePermissions();
                const isManager = canUpdateProject.value;
                
                // Use the smart task status service that understands workflow context
                const updatedTask = await taskStatusService.completeTask(numericProjectId, this.currentTaskData, isManager);
                
                // Update the current task data
                this.currentTaskData = updatedTask;
                
                // Update the task in the available tasks list
                const taskIndex = this.availableTasks.findIndex(task => task.id === updatedTask.id);
                if (taskIndex !== -1) {
                    this.availableTasks[taskIndex] = updatedTask;
                }

                logger.info(`Successfully completed task ${this.currentTaskData.id}`);
                return true;
            } catch (error) {
                logger.error('Failed to complete task:', error);
                this.error = error instanceof Error ? error.message : 'Failed to complete task';
                return false;
            }
        },

        /**
         * Complete current task and automatically move to next workflow stage
         */
        async completeAndMoveToNextTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot complete task: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);
                
                // Check if user has manager permissions for completion stage tasks
                const { usePermissions } = await import('@/composables/usePermissions');
                const { canUpdateProject } = usePermissions();
                const isManager = canUpdateProject.value;
                
                // Use the smart task status service (same as other complete methods now)
                const updatedTask = await taskStatusService.completeTask(numericProjectId, this.currentTaskData, isManager);
                
                // Update the current task data
                this.currentTaskData = updatedTask;
                
                // Update the task in the available tasks list
                const taskIndex = this.availableTasks.findIndex(task => task.id === updatedTask.id);
                if (taskIndex !== -1) {
                    this.availableTasks[taskIndex] = updatedTask;
                }

                logger.info(`Successfully completed and moved task ${this.currentTaskData.id} to next stage`);
                return true;
            } catch (error) {
                logger.error('Failed to complete and move task:', error);
                this.error = error instanceof Error ? error.message : 'Failed to complete and move task';
                return false;
            }
        },

        /**
         * Mark a completed task as uncomplete
         */
        async uncompleteCurrentTask(): Promise<boolean> {
            if (!this.currentTaskData || !this.currentProjectId) {
                logger.error('Cannot uncomplete task: no current task or project');
                return false;
            }

            try {
                const numericProjectId = parseInt(this.currentProjectId);
                
                // Check if user has manager permissions for completion stage tasks
                const { usePermissions } = await import('@/composables/usePermissions');
                const { canUpdateProject } = usePermissions();
                const isManager = canUpdateProject.value;
                
                // Use the smart task status service
                const updatedTask = await taskStatusService.uncompleteTask(numericProjectId, this.currentTaskData, isManager);
                
                // Update the current task data
                this.currentTaskData = updatedTask;
                
                // Update the task in the available tasks list
                const taskIndex = this.availableTasks.findIndex(task => task.id === updatedTask.id);
                if (taskIndex !== -1) {
                    this.availableTasks[taskIndex] = updatedTask;
                }

                logger.info(`Successfully uncompleted task ${this.currentTaskData.id}`);
                return true;
            } catch (error) {
                logger.error('Failed to uncomplete task:', error);
                this.error = error instanceof Error ? error.message : 'Failed to uncomplete task';
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
                
                // Check if user has manager permissions for completion stage tasks
                const { usePermissions } = await import('@/composables/usePermissions');
                const { canUpdateProject } = usePermissions();
                const isManager = canUpdateProject.value;
                
                // Use the smart task status service
                const suspendedTask = await taskStatusService.suspendTask(numericProjectId, this.currentTaskData, isManager);
                
                // Update the current task data
                this.currentTaskData = suspendedTask;
                
                // Update the task in the available tasks list
                const taskIndex = this.availableTasks.findIndex(task => task.id === suspendedTask.id);
                if (taskIndex !== -1) {
                    this.availableTasks[taskIndex] = suspendedTask;
                }
                
                logger.info(`Successfully suspended task ${this.currentTaskData.id}`);
                return true;
            } catch (error) {
                logger.error('Failed to suspend task:', error);
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
                
                // Check if user has manager permissions for completion stage tasks
                const { usePermissions } = await import('@/composables/usePermissions');
                const { canUpdateProject } = usePermissions();
                const isManager = canUpdateProject.value;
                
                // Call the backend API to set task status to deferred
                const deferredTask = await taskStatusService.deferTask(numericProjectId, this.currentTaskData, isManager);
                
                // Update the current task data
                this.currentTaskData = deferredTask;
                
                // Update the task in the available tasks list
                const taskIndex = this.availableTasks.findIndex(task => task.id === deferredTask.id);
                if (taskIndex !== -1) {
                    this.availableTasks[taskIndex] = deferredTask;
                }
                
                logger.info(`Successfully deferred task ${this.currentTaskData.id}`);
                return true;
            } catch (error) {
                logger.error('Failed to defer task:', error);
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
                
                // Use the specialized return for rework endpoint
                const returnedTask = await taskStatusService.returnTaskForRework(numericProjectId, this.currentTaskData, reason);
                
                // Update the current task data
                this.currentTaskData = returnedTask;
                
                // Update the task in the available tasks list
                const taskIndex = this.availableTasks.findIndex(task => task.id === returnedTask.id);
                if (taskIndex !== -1) {
                    this.availableTasks[taskIndex] = returnedTask;
                }

                logger.info(`Successfully returned task ${this.currentTaskData.id} for rework`, { reason });
                return true;
            } catch (error) {
                logger.error('Failed to return task for rework:', error);
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

            const currentIndex = this.availableTasks.findIndex(task => task.id === this.currentTaskData?.id);

            // Look for next uncompleted task
            for (let i = currentIndex + 1; i < this.availableTasks.length; i++) {
                const task = this.availableTasks[i];
                if (!task.completedAt) {
                    return {
                        projectId: this.currentProjectId!,
                        assetId: task.assetId.toString(),
                        taskId: task.id.toString()
                    };
                }
            }

            // If no task found after current, wrap around to beginning
            for (let i = 0; i < currentIndex; i++) {
                const task = this.availableTasks[i];
                if (!task.completedAt) {
                    return {
                        projectId: this.currentProjectId!,
                        assetId: task.assetId.toString(),
                        taskId: task.id.toString()
                    };
                }
            }

            logger.info('No more uncompleted tasks available');
            return null;
        },


    },
});
