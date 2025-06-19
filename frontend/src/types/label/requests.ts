import type { Label } from './label';

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