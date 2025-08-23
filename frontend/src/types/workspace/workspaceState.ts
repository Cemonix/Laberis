import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { Timer } from "@/utils/timer";
import type { Point } from "@/types/common/point";
import type { Tool, ToolName } from "@/types/workspace/tools";
import type { Annotation } from '@/types/workspace/annotation';
import type { LabelScheme } from '@/types/label/labelScheme';
import type { Asset } from '@/types/asset/asset';
import type { Task } from '@/types/task';
import type { WorkflowStageType } from '@/types/workflow/workflowstage';

export interface WorkspaceState {
    currentProjectId: string | null;
    currentAssetId: string | null;
    currentAssetData: Asset | null;
    currentImageUrl: string | null;
    imageNaturalDimensions: ImageDimensions | null;
    canvasDisplayDimensions: ImageDimensions | null;
    timerInstance: Timer;
    elapsedTimeDisplay: string;
    timerIntervalId: number | null;
    lastSavedWorkingTime: number;
    viewOffset: Point
    zoomLevel: number;
    activeTool: ToolName;
    availableTools: Tool[];
    annotations: Annotation[];
    currentLabelId: number | null;
    currentLabelScheme: LabelScheme | null;
    availableLabelSchemes: LabelScheme[];
    currentTaskId: number | null;
    currentTaskData: Task | null;
    currentWorkflowStageType: WorkflowStageType | null;
    availableTasks: Task[];
    initialTaskId: number | null;
    isLoading: boolean;
    error: string | null;
}
