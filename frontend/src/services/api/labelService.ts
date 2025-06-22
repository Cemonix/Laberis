/**
 * Label Service - Handles API operations for labels within label schemes
 */

import apiClient from './apiClient';
import { AppLogger } from '@/utils/logger';
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
        const { pageNumber = 1, pageSize = 25 } = query_params;
        logger.info('Fetching labels for scheme', { projectId, schemeId, query: query_params });

        try {
            const response = await apiClient.get<PaginatedResponse<LabelResponse>>(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                { params: query_params }
            );

            if (
                !response?.data ||
                typeof response.data.data === 'undefined' ||
                !Array.isArray(response.data.data) ||
                typeof response.data.currentPage !== 'number' ||
                typeof response.data.pageSize !== 'number' ||
                typeof response.data.totalPages !== 'number'
            ) {
                logger.warn('Invalid paginated response structure received for labels', {
                    projectId,
                    schemeId,
                    query: query_params,
                    responseData: response?.data
                });
                return {
                    data: [],
                    currentPage: pageNumber,
                    pageSize: pageSize,
                    totalPages: 0,
                    totalItems: 0
                };
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
            logger.error('Failed to fetch labels for scheme', { error, projectId, schemeId, query: query_params });
            throw error;
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

            if (!response?.data) {
                logger.warn('Empty response data received for getLabelById', { projectId, schemeId, labelId });
                throw new Error('Invalid data received for label');
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
            logger.error('Failed to fetch label by ID', { error, projectId, schemeId, labelId });
            throw error;
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

            if (!response?.data) {
                logger.warn('Empty response data received for createLabel', { projectId, schemeId, name: data.name });
                throw new Error('Invalid data received after creating label');
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
            logger.error('Failed to create label', { error, projectId, schemeId, requestData: data });
            throw error;
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

            if (!response?.data) {
                logger.warn('Empty response data received for updateLabel', { projectId, schemeId, labelId });
                throw new Error('Invalid data received after updating label');
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
            logger.error('Failed to update label', { error, projectId, schemeId, labelId, requestData: data });
            throw error;
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
            logger.error('Failed to delete label', { error, projectId, schemeId, labelId });
            throw error;
        }
    }
}

export const labelService = new LabelService();
