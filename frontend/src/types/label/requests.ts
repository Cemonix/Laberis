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

export interface GetLabelSchemesQuery {
    filterOn?: string;
    filterQuery?: string;
    sortBy?: string;
    isAscending?: boolean;
    pageNumber?: number;
    pageSize?: number;
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

export interface GetLabelsQuery {
    filterOn?: string;
    filterQuery?: string;
    sortBy?: string;
    isAscending?: boolean;
    pageNumber?: number;
    pageSize?: number;
}
