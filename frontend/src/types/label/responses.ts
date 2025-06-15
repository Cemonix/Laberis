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

export interface PaginatedLabelSchemesResponse {
    data: LabelSchemeResponse[];
    pageSize: number;
    currentPage: number;
    totalPages: number;
}

export interface PaginatedLabelsResponse {
    data: LabelResponse[];
    pageSize: number;
    currentPage: number;
    totalPages: number;
}

/**
 * Utility type to transform backend response to frontend types
 */
export type LabelSchemeFromResponse = {
    labelSchemeId: LabelSchemeResponse['id'];
    name: LabelSchemeResponse['name'];
    description: LabelSchemeResponse['description'];
    projectId: LabelSchemeResponse['projectId'];
    isDefault: LabelSchemeResponse['isDefault'];
    labels: LabelFromResponse[];
};

/**
 * Utility type to transform backend label response to frontend types
 */
export type LabelFromResponse = {
    labelId: LabelResponse['id'];
    name: LabelResponse['name'];
    color: LabelResponse['color'];
    description: LabelResponse['description'];
    labelSchemeId: LabelResponse['labelSchemeId'];
    metadata: LabelResponse['metadata'];
};
