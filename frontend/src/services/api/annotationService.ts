import apiClient from './apiClient';
import type { ApiError, PaginatedResponse } from '@/types/api';
import type { 
    Annotation, 
    AnnotationDto, 
    CreateAnnotationDto, 
    UpdateAnnotationDto, 
    AnnotationListParams 
} from '@/types/annotation';
import { ApiResponseError, ServerError, NetworkError } from '@/types/common/errors';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('AnnotationService');

class AnnotationService {
    private readonly baseUrl = '/annotations';

    /**
     * Transforms backend AnnotationDto to frontend Annotation
     */
    private transformAnnotation(dto: AnnotationDto): Annotation {
        let coordinates;
        try {
            coordinates = JSON.parse(dto.data);
        } catch (error) {
            logger.error('Failed to parse annotation coordinates:', dto.data);
            throw new ApiResponseError('Invalid annotation coordinates format');
        }

        return {
            annotationId: dto.id,
            annotationType: dto.annotationType as any, // Type assertion for now
            coordinates,
            labelId: dto.labelId,
            assetId: dto.assetId,
            taskId: dto.taskId,
            notes: dto.notes,
            confidenceScore: dto.confidenceScore,
            parentAnnotationId: dto.parentAnnotationId,
            isGroundTruth: dto.isGroundTruth,
            isPrediction: dto.isPrediction,
            version: dto.version,
            createdAt: dto.createdAt,
            updatedAt: dto.updatedAt,
            annotatorUserId: dto.annotatorUserId,
        };
    }

    /**
     * Transforms frontend Annotation to backend CreateAnnotationDto format
     */
    private transformAnnotationForSave(annotation: Omit<Annotation, 'annotationId' | 'createdAt' | 'updatedAt'>): CreateAnnotationDto {
        return {
            annotationType: annotation.annotationType,
            data: JSON.stringify(annotation.coordinates),
            isPrediction: annotation.isPrediction || false,
            confidenceScore: annotation.confidenceScore,
            isGroundTruth: annotation.isGroundTruth || false,
            version: annotation.version || 1,
            notes: annotation.notes,
            taskId: annotation.taskId,
            assetId: annotation.assetId,
            labelId: annotation.labelId,
            annotatorUserId: annotation.annotatorUserId || '',
            parentAnnotationId: annotation.parentAnnotationId,
        };
    }

    /**
     * Gets annotations for a specific asset with optional filtering and pagination
     * @param assetId The asset ID
     * @param options Query options for filtering, sorting, and pagination
     * @returns Promise resolving to paginated annotation list
     */
    async getAnnotationsForAsset(
        assetId: number,
        options: AnnotationListParams = {}
    ): Promise<PaginatedResponse<Annotation>> {
        logger.info(`Fetching annotations for asset ${assetId}`, options);

        try {
            const response = await apiClient.get<PaginatedResponse<AnnotationDto>>(
                `${this.baseUrl}/asset/${assetId}`,
                { params: options }
            );

            if (response && response.data && Array.isArray(response.data.data)) {
                logger.info(`Fetched ${response.data.data.length} annotations for asset ${assetId}`, response.data);
                
                // Transform backend DTOs to frontend types
                const transformedAnnotations = response.data.data.map(dto => this.transformAnnotation(dto));
                
                return {
                    ...response.data,
                    data: transformedAnnotations
                };
            } else {
                logger.error(`Invalid response structure for annotations for asset ${assetId}`, response);
                throw new ApiResponseError('Invalid response structure from API for annotations.');
            }
        } catch (error) {
            if (error instanceof ApiResponseError) {
                throw error;
            }

            const apiError = error as ApiError;
            logger.error(`Failed to fetch annotations for asset ${assetId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred while fetching annotations',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown error while fetching annotations', apiError);
        }
    }

    /**
     * Creates a new annotation
     * @param annotation The annotation data to create
     * @returns Promise resolving to the created annotation
     */
    async createAnnotation(annotation: Omit<Annotation, 'annotationId' | 'createdAt' | 'updatedAt'>): Promise<Annotation> {
        logger.info(`Creating annotation`, annotation);

        try {
            const backendPayload = this.transformAnnotationForSave(annotation);
            const response = await apiClient.post<AnnotationDto>(this.baseUrl, backendPayload);
            
            if (response && response.data && response.data.id) {
                logger.info(`Annotation created successfully!`, response.data);
                return this.transformAnnotation(response.data);
            } else {
                logger.error(`Invalid response structure after creating annotation`, response);
                throw new ApiResponseError('Invalid response structure from API after creating annotation.');
            }
        } catch (error) {
            if (error instanceof ApiResponseError) {
                throw error;
            }

            const apiError = error as ApiError;
            logger.error(`Failed to create annotation`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred while creating annotation',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown error while creating annotation', apiError);
        }
    }

    /**
     * Updates an existing annotation
     * @param annotationId The annotation ID
     * @param annotationData The updated annotation data
     * @returns Promise resolving to the updated annotation
     */
    async updateAnnotation(
        annotationId: number,
        annotationData: UpdateAnnotationDto
    ): Promise<Annotation> {
        logger.info(`Updating annotation ${annotationId}`, annotationData);

        try {
            const response = await apiClient.put<AnnotationDto>(`${this.baseUrl}/${annotationId}`, annotationData);
            
            if (response && response.data && response.data.id) {
                logger.info(`Annotation ${annotationId} updated successfully!`, response.data);
                return this.transformAnnotation(response.data);
            } else {
                logger.error(`Invalid response structure after updating annotation ${annotationId}`, response);
                throw new ApiResponseError('Invalid response structure from API after updating annotation.');
            }
        } catch (error) {
            if (error instanceof ApiResponseError) {
                throw error;
            }

            const apiError = error as ApiError;
            logger.error(`Failed to update annotation ${annotationId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred while updating annotation',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown error while updating annotation', apiError);
        }
    }

    /**
     * Deletes an annotation by ID
     * @param annotationId The annotation ID to delete
     * @returns Promise resolving when the annotation is deleted
     */
    async deleteAnnotation(annotationId: number): Promise<void> {
        logger.info(`Deleting annotation ${annotationId}`);

        try {
            const response = await apiClient.delete(`${this.baseUrl}/${annotationId}`);
            
            if (response.status === 204 || response.status === 200) {
                logger.info(`Annotation ${annotationId} deleted successfully!`);
            } else {
                logger.error(`Unexpected status code after deleting annotation ${annotationId}`, response);
                throw new ApiResponseError('Unexpected response from API after deleting annotation.');
            }
        } catch (error) {
            if (error instanceof ApiResponseError) {
                throw error;
            }

            const apiError = error as ApiError;
            logger.error(`Failed to delete annotation ${annotationId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred while deleting annotation',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown error while deleting annotation', apiError);
        }
    }
}

export default new AnnotationService();