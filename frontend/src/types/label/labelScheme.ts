import type { Label } from '@/types/label/label';

export interface LabelScheme {
    labelSchemeId: number;
    name: string;
    description?: string;
    labels: Label[];
    projectId: number;
    isDefault?: boolean;
}

export interface FormPayloadLabelScheme {
    name: string;
    description?: string;
    labels: Omit<Label, 'labelId' | 'labelSchemeId'>[];
}