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
import { fetchAnnotations, saveAnnotation } from '@/services/api/annotationService';

// TODO: Replace with actual API calls or more sophisticated logic
const SAMPLE_LABEL_SCHEME: LabelScheme = {
    labelSchemeId: 1,
    name: "Default Scheme",
    projectId: 1,
    labels: [
        { labelId: 1, name: "Person", color: "#FF0000", labelSchemeId: 1, description: "A human being" },
        { labelId: 2, name: "Car", color: "#00FF00", labelSchemeId: 1, description: "A vehicle" },
        { labelId: 3, name: "Tree", color: "#0000FF", labelSchemeId: 1, description: "A woody plant" },
    ],
};

const MIN_ZOOM = 0.1;
const MAX_ZOOM = 10.0;
const ZOOM_SENSITIVITY = 0.005;


export const useWorkspaceStore = defineStore("workspace", {
    state: (): WorkspaceState => ({
        currentProjectId: null,
        currentAssetId: null,
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
        currentLabelScheme: SAMPLE_LABEL_SCHEME as LabelScheme | null,
        currentTaskId: null as number | null,
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
                if (!state.currentLabelScheme) {
                    return undefined;
                }
                return state.currentLabelScheme.labels.find(label => label.labelId === labelId);
            };
        },
    },

    actions: {
        async loadAsset(projectId: string, assetId: string) {
            this.currentProjectId = projectId;
            this.currentAssetId = assetId;

            // TODO: Replace with actual API call or more sophisticated logic
            this.currentImageUrl = `https://picsum.photos/800/600?random=${Math.random()}`;
            console.log(
                `[Store] Loaded asset: P:<span class="math-inline">\{projectId\}, A\:</span>{assetId}, URL: ${this.currentImageUrl}`
            );

            this.imageNaturalDimensions = null;
            this.annotations = [];
            this.currentLabelId = null;

            try {
                const fetchedAnnotations = await fetchAnnotations(assetId);
                this.setAnnotations(fetchedAnnotations);
            } catch (error) {
                console.error("Failed to fetch annotations:", error);
                this.setAnnotations([]); // Clear annotations on error
            }

            this.startTimer();
            this.setActiveTool(ToolName.CURSOR);
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
         * Adds a new annotation to the current list.
         * Later, this will also handle POSTing to the backend.
         */
        async addAnnotation(annotation: Annotation) {
            this.annotations.push(annotation);

            try {
                const savedAnnotation = await saveAnnotation(annotation);

                const index = this.annotations.findIndex(a => a.clientId === savedAnnotation.clientId);
                if (index !== -1) {
                    this.annotations[index] = savedAnnotation;
                }

            } catch (error) {
                console.error("Failed to save annotation:", error);

                this.annotations = this.annotations.filter(a => a.clientId !== annotation.clientId);

                // TODO: Show a user-friendly error notification
                alert("Could not save annotation. Please try again.");
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
