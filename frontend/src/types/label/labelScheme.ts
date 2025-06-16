import type { Label } from './label';

export interface LabelScheme {
    labelSchemeId: number;
    name: string;
    description?: string;
    projectId: number;
    isDefault?: boolean;
    createdAt: string;
    labels?: Label[];
}

export interface FormPayloadLabelScheme {
    name: string;
    description?: string;
    labels: {
        name: string;
        color: string;
        description?: string;
    }[];
}