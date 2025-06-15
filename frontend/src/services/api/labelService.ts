/**
 * Label Service - Handles API operations for labels within label schemes
 */

import apiClient from './apiClient';
import { loggerInstance } from '@/utils/logger';
import type { 
    LabelResponse, 
    PaginatedLabelsResponse
} from '@/types/label/responses';
import type { 
    CreateLabelRequest, 
    UpdateLabelRequest,
    GetLabelsQuery 
} from '@/types/label/requests';
import type { Label } from '@/types/label/label';

// Create service-specific logger
const logger = loggerInstance.createServiceLogger('LabelService');

/**
 * Transform backend LabelResponse to frontend Label
 */
function transformLabelResponse(response: LabelResponse): Label {
    return {
        labelId: response.id,
        name: response.name,
        color: response.color,
        description: response.description,
        labelSchemeId: response.labelSchemeId,
        metadata: response.metadata
    };
}

/**
 * Service class for managing labels within label schemes
 */
class LabelService {
    /**
     * Retrieves all labels for a specific label scheme with optional filtering and pagination
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @param query Optional query parameters for filtering, sorting, and pagination
     * @returns Promise resolving to labels with pagination metadata
     */
    async getLabelsForScheme(
        projectId: number,
        schemeId: number, 
        query: GetLabelsQuery = {}
    ): Promise<{ labels: Label[]; pagination?: any }> {
        logger.info('Fetching labels for scheme', { projectId, schemeId, query });
        
        try {
            const response = await apiClient.get<PaginatedLabelsResponse>(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                { params: query }
            );

            // Handle empty or invalid response
            if (!response?.data) {
                logger.warn('Empty response received', { projectId, schemeId, query });
                return {
                    labels: [],
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
                logger.warn('Invalid data format received', { projectId, schemeId, query });
                return {
                    labels: [],
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
                logger.info('No labels found for scheme', { projectId, schemeId });
                return {
                    labels: [],
                    pagination: {
                        pageSize: query.pageSize || 25,
                        currentPage: query.pageNumber || 1,
                        totalPages: 0,
                        hasNextPage: false,
                        hasPreviousPage: false
                    }
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
                labels,
                pagination: {
                    pageSize: response.data.pageSize,
                    currentPage: response.data.currentPage,
                    totalPages: response.data.totalPages,
                    hasNextPage: response.data.currentPage < response.data.totalPages,
                    hasPreviousPage: response.data.currentPage > 1
                }
            };
        } catch (error) {
            logger.error('Failed to fetch labels for scheme', error);
            throw error;
        }
    }

    /**
     * Retrieves a single label by its ID
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @param labelId The unique identifier of the label
     * @returns Promise resolving to the label data
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
            
            const label = transformLabelResponse(response.data);
            
            logger.info('Successfully fetched label', { 
                projectId, 
                schemeId, 
                labelId, 
                name: label.name 
            });
            
            return label;
        } catch (error) {
            logger.error('Failed to fetch label', error);
            throw error;
        }
    }

    /**
     * Creates a new label within a label scheme
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @param data The label data for creation
     * @returns Promise resolving to the created label
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
            
            const label = transformLabelResponse(response.data);
            
            logger.info('Successfully created label', { 
                projectId, 
                schemeId,
                labelId: label.labelId, 
                name: label.name 
            });
            
            return label;
        } catch (error) {
            logger.error('Failed to create label', error);
            throw error;
        }
    }

    /**
     * Updates an existing label
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @param labelId The unique identifier of the label to update
     * @param data The updated label data
     * @returns Promise resolving to the updated label
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
            
            const label = transformLabelResponse(response.data);
            
            logger.info('Successfully updated label', { 
                projectId, 
                schemeId,
                labelId, 
                name: label.name 
            });
            
            return label;
        } catch (error) {
            logger.error('Failed to update label', error);
            throw error;
        }
    }

    /**
     * Deletes a label from a label scheme
     * @param projectId The unique identifier of the project
     * @param schemeId The unique identifier of the label scheme
     * @param labelId The unique identifier of the label to delete
     * @returns Promise that resolves when the label is deleted
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
            logger.error('Failed to delete label', error);
            throw error;
        }
    }
}

// Export singleton instance
export const labelService = new LabelService();
