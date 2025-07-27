/**
 * Label Service - Handles API operations for labels within label schemes
 */

import { BaseProjectService } from '../base';
import type { LabelResponse } from '@/types/label/responses';
import type {
    CreateLabelRequest,
    UpdateLabelRequest,
} from '@/types/label/requests';
import type { Label } from '@/types/label/label';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { QueryParams } from '@/types/api';

/**
 * Transforms a backend LabelResponse object to a frontend Label object.
 */
function transformLabelResponse(response: LabelResponse): Label {
    return {
        labelId: response.id,
        name: response.name,
        color: response.color,
        description: response.description,
        labelSchemeId: response.labelSchemeId,
        createdAt: response.createdAt,
    };
}

/**
 * Service class for managing labels within label schemes.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class LabelService extends BaseProjectService {
    constructor() {
        super('LabelService');
    }

    /**
     * Retrieves all labels for a specific label scheme with optional filtering and pagination.
     */
    async getLabelsForScheme(
        projectId: number,
        schemeId: number,
        queryParams: QueryParams = {}
    ): Promise<PaginatedResponse<Label>> {
        this.logger.info('Fetching labels for scheme', { projectId, schemeId, query: queryParams });

        const url = this.buildProjectResourceUrl(projectId, 'labelschemes/{schemeId}/labels', { schemeId });
        const response = await this.getPaginated<LabelResponse>(url, queryParams);

        const labels = response.data.map(transformLabelResponse);

        this.logger.info('Successfully fetched labels', {
            projectId,
            schemeId,
            count: labels.length,
            currentPage: response.currentPage,
            totalPages: response.totalPages
        });

        return {
            data: labels,
            currentPage: response.currentPage,
            pageSize: response.pageSize,
            totalPages: response.totalPages,
            totalItems: response.totalItems
        };
    }

    /**
     * Retrieves a single label by its ID.
     */
    async getLabelById(
        projectId: number,
        schemeId: number,
        labelId: number
    ): Promise<Label> {
        this.logger.info('Fetching label by ID', { projectId, schemeId, labelId });

        const url = this.buildProjectResourceUrl(
            projectId, 
            'labelschemes/{schemeId}/labels/{labelId}',
            { schemeId, labelId }
        );
        
        const response = await this.get<LabelResponse>(url);
        const label = transformLabelResponse(response);

        this.logger.info('Successfully fetched label', {
            projectId,
            schemeId,
            labelId,
            name: label.name
        });

        return label;
    }

    /**
     * Creates a new label within a label scheme.
     */
    async createLabel(
        projectId: number,
        schemeId: number,
        data: CreateLabelRequest
    ): Promise<Label> {
        this.logger.info('Creating new label', { projectId, schemeId, name: data.name });

        const url = this.buildProjectResourceUrl(projectId, 'labelschemes/{schemeId}/labels', { schemeId });
        const response = await this.post<CreateLabelRequest, LabelResponse>(url, data);
        const label = transformLabelResponse(response);

        this.logger.info('Successfully created label', {
            projectId,
            schemeId,
            labelId: label.labelId,
            name: label.name
        });

        return label;
    }

    /**
     * Updates an existing label.
     */
    async updateLabel(
        projectId: number,
        schemeId: number,
        labelId: number,
        data: UpdateLabelRequest
    ): Promise<Label> {
        this.logger.info('Updating label', { projectId, schemeId, labelId, updates: data });

        const url = this.buildProjectResourceUrl(
            projectId,
            'labelschemes/{schemeId}/labels/{labelId}',
            { schemeId, labelId }
        );
        
        const response = await this.put<UpdateLabelRequest, LabelResponse>(url, data);
        const label = transformLabelResponse(response);

        this.logger.info('Successfully updated label', {
            projectId,
            schemeId,
            labelId,
            name: label.name
        });

        return label;
    }

    /**
     * Deletes a label from a label scheme.
     */
    async deleteLabel(
        projectId: number,
        schemeId: number,
        labelId: number
    ): Promise<void> {
        this.logger.info('Deleting label', { projectId, schemeId, labelId });

        const url = this.buildProjectResourceUrl(
            projectId,
            'labelschemes/{schemeId}/labels/{labelId}',
            { schemeId, labelId }
        );
        
        await this.delete(url);
        
        this.logger.info('Successfully deleted label', { projectId, schemeId, labelId });
    }
}

export const labelService = new LabelService();