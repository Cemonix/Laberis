export interface LabelSchemeResponse {
    id: number;
    name: string;
    description?: string;
    projectId: number;
    isDefault: boolean;
    isActive: boolean;
    createdAt: string;
    deletedAt?: string;
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
