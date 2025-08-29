export interface Label {
    labelId: number;
    name: string;
    color: string;
    description?: string;
    labelSchemeId: number;
    createdAt: string;
}

export interface LabelScheme {
    labelSchemeId: number;
    name: string;
    description?: string;
    projectId: number;
    isDefault?: boolean;
    isActive: boolean;
    createdAt: string;
    deletedAt?: string;
    labels?: Label[];
}

export interface FormPayloadLabelScheme {
    name: string;
    description?: string;
    labels?: {
        name: string;
        color: string;
        description?: string;
    }[];
}

export interface LabelSchemeDeletionImpact {
    labelSchemeId: number;
    labelSchemeName: string;
    totalLabelsCount: number;
    totalAnnotationsCount: number;
    labelImpacts: LabelImpact[];
}

export interface LabelImpact {
    labelId: number;
    labelName: string;
    labelColor?: string;
    annotationsCount: number;
}

export interface CreateLabelSchemeRequest {
    name: string;
    description?: string;
    labels?: Omit<Label, 'labelId' | 'labelSchemeId'>[];
}

export interface UpdateLabelSchemeRequest {
    name?: string;
    description?: string;
}

export interface CreateLabelRequest {
    name: string;
    color: string;
    description?: string;
    metadata?: any;
}

export interface UpdateLabelRequest {
    name?: string;
    color?: string;
    description?: string;
    metadata?: any;
}

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