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
import type { Asset } from '@/types/asset/asset';
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
    },

    actions: {
        async loadAsset(projectId: string, assetId: string) {
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

                // Fetch annotations for this asset
                try {
                    const fetchedAnnotations = await annotationService.getAnnotationsForAsset(numericAssetId);
                    this.setAnnotations(fetchedAnnotations.data);
                    logger.info(`Loaded ${fetchedAnnotations.data.length} annotations for asset ${assetId}`);
                } catch (annotationError) {
                    logger.error("Failed to fetch annotations:", annotationError);
                    this.setAnnotations([]); // Clear annotations on error
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
                const index = this.annotations.findIndex(a => a.clientId === savedAnnotation.clientId);
                if (index !== -1) {
                    this.annotations[index] = savedAnnotation;
                } else {
                    // If clientId doesn't match, find by coordinates or other unique properties
                    const foundIndex = this.annotations.findIndex(a => 
                        a.assetId === savedAnnotation.assetId && 
                        a.labelId === savedAnnotation.labelId &&
                        JSON.stringify(a.coordinates) === JSON.stringify(savedAnnotation.coordinates)
                    );
                    if (foundIndex !== -1) {
                        this.annotations[foundIndex] = savedAnnotation;
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
    },
});
