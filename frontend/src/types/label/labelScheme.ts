import type { Label } from './label';

export interface LabelScheme {
    labelSchemeId: number;
    name: string;
    description?: string;
    projectId: number;
    isDefault?: boolean;
    labels: Label[];
}

export interface FormPayloadLabelScheme {
    name: string;
    description?: string;
    labels: Omit<Label, 'labelId' | 'labelSchemeId'>[];
}