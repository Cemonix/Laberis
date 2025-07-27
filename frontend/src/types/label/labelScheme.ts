import type { Label } from './label';

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