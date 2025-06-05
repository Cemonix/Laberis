export interface Label {
    labelId: number;
    name: string;
    color: string;
    description?: string;
    labelSchemeId: number;
    metadata?: any;
}

export interface LabelScheme {
    labelSchemeId: number;
    name: string;
    description?: string;
    labels: Label[];
    projectId: number;
    isDefault?: boolean;
}