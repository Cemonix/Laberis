/**
 * Label Scheme Service - Handles API operations for label schemes
 */

import apiClient from './apiClient';
import { AppLogger } from '@/utils/logger';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse } from '@/services/utils';
import type { LabelSchemeResponse } from '@/types/label/responses';
import type {
    CreateLabelSchemeRequest,
    UpdateLabelSchemeRequest
} from '@/types/label/requests';
import type { LabelScheme } from '@/types/label/labelScheme';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { QueryParams } from '@/types/api';

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
        query_params: QueryParams = {}
    ): Promise<PaginatedResponse<LabelScheme>> {
        logger.info('Fetching label schemes for project', { projectId, query: query_params });

        try {
            const response = await apiClient.get<PaginatedResponse<LabelSchemeResponse>>(
                `/projects/${projectId}/labelschemes`,
                { params: query_params }
            );

            if (!isValidPaginatedResponse(response)) {
                throw transformApiError(new Error('Invalid paginated response structure'), 
                    'Failed to fetch label schemes - Invalid response format');
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
            throw transformApiError(error, 'Failed to fetch label schemes');
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

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to fetch label scheme - Invalid response format');
            }
            
            const scheme = transformLabelSchemeResponse(response.data);

            logger.info('Successfully fetched label scheme', { projectId, schemeId, name: scheme.name });
            return scheme;
        } catch (error) {
            throw transformApiError(error, `Failed to fetch label scheme ${schemeId}`);
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

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to create label scheme - Invalid response format');
            }

            const scheme = transformLabelSchemeResponse(response.data);

            logger.info('Successfully created label scheme', {
                projectId,
                schemeId: scheme.labelSchemeId,
                name: scheme.name
            });

            return scheme;
        } catch (error) {
            throw transformApiError(error, 'Failed to create label scheme');
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

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to update label scheme - Invalid response format');
            }
            
            const scheme = transformLabelSchemeResponse(response.data);

            logger.info('Successfully updated label scheme', {
                projectId,
                schemeId,
                name: scheme.name
            });

            return scheme;
        } catch (error) {
            throw transformApiError(error, `Failed to update label scheme ${schemeId}`);
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
            throw transformApiError(error, `Failed to delete label scheme ${schemeId}`);
        }
    }
}

export const labelSchemeService = new LabelSchemeService();
