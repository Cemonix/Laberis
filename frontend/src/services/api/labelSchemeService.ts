/**
 * Label Scheme Service - Handles API operations for label schemes
 */

import apiClient from './apiClient';
import { AppLogger } from '@/utils/logger';
import type { LabelSchemeResponse } from '@/types/label/responses';
import type {
    CreateLabelSchemeRequest,
    UpdateLabelSchemeRequest
} from '@/types/label/requests';
import type { LabelScheme } from '@/types/label/labelScheme';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { BaseListParams } from '@/types/api';

const logger = AppLogger.createServiceLogger('LabelSchemeService');

/**
 * Transforms a backend LabelSchemeResponse object to a frontend LabelScheme object.
 * Assumes LabelSchemeResponse mirrors the backend LabelSchemeDto.
 * @param response - The backend label scheme response object.
 * @returns The transformed frontend label scheme object.
 */
function transformLabelSchemeResponse(response: LabelSchemeResponse): LabelScheme {
    return {
        labelSchemeId: response.id,
        name: response.name,
        description: response.description,
        projectId: response.projectId,
        isDefault: response.isDefault,
        createdAt: response.createdAt,
    };
}

/**
 * Service class for managing label schemes.
 */
class LabelSchemeService {
    /**
     * Retrieves all label schemes for a specific project with optional filtering and pagination.
     * @param projectId - The unique identifier of the project.
     * @param query_params - Optional query parameters for filtering, sorting, and pagination.
     * @returns A promise resolving to a paginated response of label schemes.
     */
    async getLabelSchemesForProject(
        projectId: number,
        query_params: BaseListParams = {}
    ): Promise<PaginatedResponse<LabelScheme>> {
        const { pageNumber = 1, pageSize = 25 } = query_params;

        logger.info('Fetching label schemes for project', { projectId, query: query_params });

        try {
            const response = await apiClient.get<PaginatedResponse<LabelSchemeResponse>>(
                `/projects/${projectId}/labelschemes`,
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
                logger.warn('Invalid paginated response structure received for label schemes', {
                    projectId,
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

            const schemes = response.data.data.map(transformLabelSchemeResponse);

            logger.info('Successfully fetched label schemes', {
                projectId,
                count: schemes.length,
                currentPage: response.data.currentPage,
                totalPages: response.data.totalPages
            });

            return {
                data: schemes,
                currentPage: response.data.currentPage,
                pageSize: response.data.pageSize,
                totalPages: response.data.totalPages,
                totalItems: response.data.totalItems
            };
        } catch (error) {
            logger.error('Failed to fetch label schemes', { error, projectId, query: query_params });
            throw error;
        }
    }

    /**
     * Retrieves a single label scheme by its ID.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @returns A promise resolving to the label scheme data.
     */
    async getLabelSchemeById(projectId: number, schemeId: number): Promise<LabelScheme> {
        logger.info('Fetching label scheme by ID', { projectId, schemeId });

        try {
            const response = await apiClient.get<LabelSchemeResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}`
            );

            if (!response?.data) {
                logger.warn('Empty response data received for getLabelSchemeById', { projectId, schemeId });
                throw new Error('Invalid data received for label scheme');
            }
            
            const scheme = transformLabelSchemeResponse(response.data);

            logger.info('Successfully fetched label scheme', { projectId, schemeId, name: scheme.name });
            return scheme;
        } catch (error) {
            logger.error('Failed to fetch label scheme by ID', { error, projectId, schemeId });
            throw error;
        }
    }

    /**
     * Creates a new label scheme within a project.
     * @param projectId - The unique identifier of the project.
     * @param data - The label scheme data for creation.
     * @returns A promise resolving to the created label scheme.
     */
    async createLabelScheme(
        projectId: number,
        data: CreateLabelSchemeRequest
    ): Promise<LabelScheme> {
        logger.info('Creating new label scheme', { projectId, name: data.name });

        try {
            const response = await apiClient.post<LabelSchemeResponse>(
                `/projects/${projectId}/labelschemes`,
                data
            );

            if (!response?.data) {
                logger.warn('Empty response data received for createLabelScheme', { projectId, name: data.name });
                throw new Error('Invalid data received after creating label scheme');
            }

            const scheme = transformLabelSchemeResponse(response.data);

            logger.info('Successfully created label scheme', {
                projectId,
                schemeId: scheme.labelSchemeId,
                name: scheme.name
            });

            return scheme;
        } catch (error) {
            logger.error('Failed to create label scheme', { error, projectId, requestData: data });
            throw error;
        }
    }

    /**
     * Updates an existing label scheme.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme.
     * @param data - The updated label scheme data.
     * @returns A promise resolving to the updated label scheme.
     */
    async updateLabelScheme(
        projectId: number,
        schemeId: number,
        data: UpdateLabelSchemeRequest
    ): Promise<LabelScheme> {
        logger.info('Updating label scheme', { projectId, schemeId, updates: data });

        try {
            const response = await apiClient.put<LabelSchemeResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}`,
                data
            );

            if (!response?.data) {
                logger.warn('Empty response data received for updateLabelScheme', { projectId, schemeId });
                throw new Error('Invalid data received after updating label scheme');
            }
            
            const scheme = transformLabelSchemeResponse(response.data);

            logger.info('Successfully updated label scheme', {
                projectId,
                schemeId,
                name: scheme.name
            });

            return scheme;
        } catch (error) {
            logger.error('Failed to update label scheme', { error, projectId, schemeId, requestData: data });
            throw error;
        }
    }

    /**
     * Deletes a label scheme from a project.
     * @param projectId - The unique identifier of the project.
     * @param schemeId - The unique identifier of the label scheme to delete.
     * @returns A promise that resolves when the label scheme is deleted.
     */
    async deleteLabelScheme(projectId: number, schemeId: number): Promise<void> {
        logger.info('Deleting label scheme', { projectId, schemeId });

        try {
            await apiClient.delete(`/projects/${projectId}/labelschemes/${schemeId}`);
            logger.info('Successfully deleted label scheme', { projectId, schemeId });
        } catch (error) {
            logger.error('Failed to delete label scheme', { error, projectId, schemeId });
            throw error;
        }
    }
}

export const labelSchemeService = new LabelSchemeService();
