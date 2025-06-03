import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { Timer } from "@/utils/timer";
import type { Point } from "@/types/common/point";
import type { Tool, ToolName } from "@/types/workspace/tools";

export interface WorkspaceState {
    currentProjectId: string | null;
    currentAssetId: string | null;
    currentImageUrl: string | null;
    imageNaturalDimensions: ImageDimensions | null;
    canvasDisplayDimensions: ImageDimensions | null;
    timerInstance: Timer;
    elapsedTimeDisplay: string;
    timerIntervalId: number | null;
    viewOffset: Point
    zoomLevel: number;
    activeTool: ToolName;
    availableTools: Tool[];
}
