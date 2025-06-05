import type { IconDefinition } from "@fortawesome/fontawesome-svg-core";

export enum ToolName {
    CURSOR = 'cursor',
    POINT = 'point',
    LINE = 'line',
    BOUNDING_BOX = 'bbox',
    POLYGON = 'polygon',
}

export interface Tool {
    id: ToolName;
    name: string;
    iconDefinition: IconDefinition
}