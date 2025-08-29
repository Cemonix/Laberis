import type { IconDefinition } from "@fortawesome/fontawesome-svg-core";

export enum ToolName {
    CURSOR = 'cursor',
    POINT = 'point',
    LINE = 'line',
    POLYLINE = 'polyline',
    POLYGON = 'polygon',
    BOUNDING_BOX = 'bbox',
}

export interface Tool {
    id: ToolName;
    name: string;
    iconDefinition: IconDefinition
}