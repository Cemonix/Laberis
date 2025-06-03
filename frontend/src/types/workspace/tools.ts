export enum ToolName {
    CURSOR = 'cursor',
    // BOUNDING_BOX = 'bbox',
    // POLYGON = 'polygon',
    // POINT = 'point',
}

export interface Tool {
    id: ToolName;
    name: string;
    icon?: string;
}