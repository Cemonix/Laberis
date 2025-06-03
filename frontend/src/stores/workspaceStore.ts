import { defineStore } from "pinia";
import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { WorkspaceState } from "@/types/workspace/workspaceState";
import { Timer } from "@/utils/timer";
import type { Point } from "@/types/common/point";
import { ToolName, type Tool } from "@/types/workspace/tools";

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
            { id: ToolName.CURSOR, name: 'Cursor', icon: 'fa-regular fa-arrow-pointer' },
        ],
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
        }
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
            // this.annotations = []; // Clear annotations for the new asset

            this.startTimer();
            this.resetZoomAndView();
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

        resetZoomAndView() {
            this.zoomLevel = 1.0;
            this.viewOffset = { x: 0, y: 0 };
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
