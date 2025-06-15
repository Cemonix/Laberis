/**
 * Label Scheme Service - Handles API operations for label schemes
 */

import apiClient from './apiClient';
import { loggerInstance } from '@/utils/logger';
import type { 
    LabelSchemeResponse, 
    PaginatedLabelSchemesResponse
} from '@/types/label/responses';
import type { 
    CreateLabelSchemeRequest, 
    UpdateLabelSchemeRequest,
    GetLabelSchemesQuery 
} from '@/types/label/requests';
import type { LabelScheme } from '@/types/label/labelScheme';

// Create service-specific logger
const logger = loggerInstance.createServiceLogger('LabelSchemeService');

/**
 * Transform backend LabelSchemeResponse to frontend LabelScheme
 */
function transformLabelSchemeResponse(response: LabelSchemeResponse): LabelScheme {
    return {
        labelSchemeId: response.id,
        name: response.name,
        description: response.description,
        projectId: response.projectId,
        isDefault: response.isDefault,
        labels: response.labels.map(label => ({
            labelId: label.id,
            name: label.name,
            color: label.color,
            description: label.description,
            labelSchemeId: label.labelSchemeId,
            metadata: label.metadata
        }))
    };
}

/**
 * Service class for managing label schemes
 */
class LabelSchemeService {
    /**
     * Retrieves all label schemes for a specific project with optional filtering and pagination
     * @param projectId The unique identifier of the project
     * @param query Optional query parameters for filtering, sorting, and pagination
     * @returns Promise resolving to label schemes with pagination metadata
     */
    async getLabelSchemesForProject(
        projectId: number, 
        query: GetLabelSchemesQuery = {}
    ): Promise<{ schemes: LabelScheme[]; pagination?: any }> {
        logger.info('Fetching label schemes for project', { projectId, query });
        
        try {
            const response = await apiClient.get<PaginatedLabelSchemesResponse>(
                `/projects/${projectId}/labelschemes`,
                { params: query }
            );

            // Handle empty or invalid response
            if (!response?.data) {
                logger.warn('Empty response received', { projectId, query });
                return {
                    schemes: [],
                    pagination: {
                        pageSize: query.pageSize || 25,
                        currentPage: query.pageNumber || 1,
                        totalPages: 0,
                        hasNextPage: false,
                        hasPreviousPage: false
                    }
                };
            }
            else if (!Array.isArray(response.data.data)) {
                logger.warn('Invalid data format received', { projectId, query });
                return {
                    schemes: [],
                    pagination: {
                        pageSize: query.pageSize || 25,
                        currentPage: query.pageNumber || 1,
                        totalPages: 0,
                        hasNextPage: false,
                        hasPreviousPage: false
                    }
                };
            }
            else if (response.data.data.length === 0) {
                logger.info('No label schemes found', { projectId, query });
                return {
                    schemes: [],
                    pagination: {
                        pageSize: query.pageSize || 25,
                        currentPage: query.pageNumber || 1,
                        totalPages: 0,
                        hasNextPage: false,
                        hasPreviousPage: false
                    }
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
                schemes,
                pagination: {
                    pageSize: response.data.pageSize,
                    currentPage: response.data.currentPage,
                    totalPages: response.data.totalPages,
                    hasNextPage: response.data.currentPage < response.data.totalPages,
                    hasPreviousPage: response.data.currentPage > 1
                }
            };
        } catch (error) {
            logger.error('Failed to fetch label schemes', error);
            throw error;
        }
    }

    /**
     * Retrieves a single label scheme by its ID
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @returns Promise resolving to the label scheme data
     */
    async getLabelSchemeById(projectId: number, schemeId: number): Promise<LabelScheme> {
        logger.info('Fetching label scheme by ID', { projectId, schemeId });
        
        try {
            const response = await apiClient.get<LabelSchemeResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}`
            );
            
            const scheme = transformLabelSchemeResponse(response.data);
            
            logger.info('Successfully fetched label scheme', { projectId, schemeId, name: scheme.name });
            return scheme;
        } catch (error) {
            logger.error('Failed to fetch label scheme', error);
            throw error;
        }
    }

    /**
     * Creates a new label scheme within a project
     * @param projectId The unique identifier of the project
     * @param data The label scheme data for creation
     * @returns Promise resolving to the created label scheme
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
            
            const scheme = transformLabelSchemeResponse(response.data);
            
            logger.info('Successfully created label scheme', { 
                projectId, 
                schemeId: scheme.labelSchemeId, 
                name: scheme.name 
            });
            
            return scheme;
        } catch (error) {
            logger.error('Failed to create label scheme', error);
            throw error;
        }
    }

    /**
     * Updates an existing label scheme
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @param data The updated label scheme data
     * @returns Promise resolving to the updated label scheme
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
            
            const scheme = transformLabelSchemeResponse(response.data);
            
            logger.info('Successfully updated label scheme', { 
                projectId, 
                schemeId, 
                name: scheme.name 
            });
            
            return scheme;
        } catch (error) {
            logger.error('Failed to update label scheme', error);
            throw error;
        }
    }

    /**
     * Deletes a label scheme from a project
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme to delete
     * @returns Promise that resolves when the label scheme is deleted
     */
    async deleteLabelScheme(projectId: number, schemeId: number): Promise<void> {
        logger.info('Deleting label scheme', { projectId, schemeId });
        
        try {
            await apiClient.delete(`/projects/${projectId}/labelschemes/${schemeId}`);
            
            logger.info('Successfully deleted label scheme', { projectId, schemeId });
        } catch (error) {
            logger.error('Failed to delete label scheme', error);
            throw error;
        }
    }
}

// Export singleton instance
export const labelSchemeService = new LabelSchemeService();
