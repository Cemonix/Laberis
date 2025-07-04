export interface LabelSchemeResponse {
    id: number;
    name: string;
    description?: string;
    projectId: number;
    isDefault: boolean;
    createdAt: string;
    updatedAt: string;
    labels: LabelResponse[];
}

export interface LabelResponse {
    id: number;
    name: string;
    color: string;
    description?: string;
    labelSchemeId: number;
    metadata?: any;
    createdAt: string;
    updatedAt: string;
}
