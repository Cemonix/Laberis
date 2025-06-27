/**
 * Label Service - Handles API operations for labels within label schemes
 */

import apiClient from './apiClient';
import { AppLogger } from '@/utils/logger';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse } from '@/services/utils';
import type { LabelResponse } from '@/types/label/responses';
import type {
    CreateLabelRequest,
    UpdateLabelRequest,
} from '@/types/label/requests';
import type { Label } from '@/types/label/label';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { QueryParams } from '@/types/api';

const logger = AppLogger.createServiceLogger('LabelService');

/**
 * Transforms a backend LabelResponse object to a frontend Label object.
 * Assumes LabelResponse mirrors the backend LabelDto.
 * @param response - The backend label response object.
 * @returns The transformed frontend label object.
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
 */
class LabelService {
    /**
     * Retrieves all labels for a specific label scheme with optional filtering and pagination.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @param query_params - Optional query parameters for filtering, sorting, and pagination.
     * @returns A promise resolving to a paginated response of labels.
     */
    async getLabelsForScheme(
        projectId: number,
        schemeId: number,
        query_params: QueryParams = {}
    ): Promise<PaginatedResponse<Label>> {
        logger.info('Fetching labels for scheme', { projectId, schemeId, query: query_params });

        try {
            const response = await apiClient.get<PaginatedResponse<LabelResponse>>(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                { params: query_params }
            );

            if (!isValidPaginatedResponse(response)) {
                throw transformApiError(new Error('Invalid paginated response structure'), 
                    'Failed to fetch labels - Invalid response format');
            }

            const labels = response.data.data.map(transformLabelResponse);

            logger.info('Successfully fetched labels', {
                projectId,
                schemeId,
                count: labels.length,
                currentPage: response.data.currentPage,
                totalPages: response.data.totalPages
            });

            return {
                data: labels,
                currentPage: response.data.currentPage,
                pageSize: response.data.pageSize,
                totalPages: response.data.totalPages,
                totalItems: response.data.totalItems
            };
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch labels');
        }
    }

    /**
     * Retrieves a single label by its ID.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @param labelId - The unique identifier of the label.
     * @returns A promise resolving to the label data.
     */
    async getLabelById(
        projectId: number,
        schemeId: number,
        labelId: number
    ): Promise<Label> {
        logger.info('Fetching label by ID', { projectId, schemeId, labelId });

        try {
            const response = await apiClient.get<LabelResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}/labels/${labelId}`
            );

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to fetch label - Invalid response format');
            }

            const label = transformLabelResponse(response.data);

            logger.info('Successfully fetched label', {
                projectId,
                schemeId,
                labelId,
                name: label.name
            });

            return label;
        } catch (error) {
            throw transformApiError(error, `Failed to fetch label ${labelId}`);
        }
    }

    /**
     * Creates a new label within a label scheme.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @param data - The label data for creation.
     * @returns A promise resolving to the created label.
     */
    async createLabel(
        projectId: number,
        schemeId: number,
        data: CreateLabelRequest
    ): Promise<Label> {
        logger.info('Creating new label', { projectId, schemeId, name: data.name });

        try {
            const response = await apiClient.post<LabelResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                data
            );

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to create label - Invalid response format');
            }

            const label = transformLabelResponse(response.data);

            logger.info('Successfully created label', {
                projectId,
                schemeId,
                labelId: label.labelId,
                name: label.name
            });

            return label;
        } catch (error) {
            throw transformApiError(error, 'Failed to create label');
        }
    }

    /**
     * Updates an existing label.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @param labelId - The unique identifier of the label to update.
     * @param data - The updated label data.
     * @returns A promise resolving to the updated label.
     */
    async updateLabel(
        projectId: number,
        schemeId: number,
        labelId: number,
        data: UpdateLabelRequest
    ): Promise<Label> {
        logger.info('Updating label', { projectId, schemeId, labelId, updates: data });

        try {
            const response = await apiClient.put<LabelResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}/labels/${labelId}`,
                data
            );

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to update label - Invalid response format');
            }

            const label = transformLabelResponse(response.data);

            logger.info('Successfully updated label', {
                projectId,
                schemeId,
                labelId,
                name: label.name
            });

            return label;
        } catch (error) {
            throw transformApiError(error, `Failed to update label ${labelId}`);
        }
    }

    /**
     * Deletes a label from a label scheme.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @param labelId - The unique identifier of the label to delete.
     * @returns A promise that resolves when the label is deleted.
     */
    async deleteLabel(
        projectId: number,
        schemeId: number,
        labelId: number
    ): Promise<void> {
        logger.info('Deleting label', { projectId, schemeId, labelId });

        try {
            await apiClient.delete(
                `/projects/${projectId}/labelschemes/${schemeId}/labels/${labelId}`
            );
            logger.info('Successfully deleted label', { projectId, schemeId, labelId });
        } catch (error) {
            throw transformApiError(error, `Failed to delete label ${labelId}`);
        }
    }
}

export const labelService = new LabelService();
