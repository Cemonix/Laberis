import type { WorkflowStagePipeline } from './index';

export interface Connection {
    from: WorkflowStagePipeline;
    to: WorkflowStagePipeline;
    x1: number;
    y1: number;
    x2: number;
    y2: number;
}

export interface PipelineLayout {
    canvasWidth: number;
    canvasHeight: number;
    stageWidth: number;
    stageHeight: number;
    horizontalSpacing: number;
    verticalSpacing: number;
}

export interface StagePosition {
    x: number;
    y: number;
    width: number;
    height: number;
}
