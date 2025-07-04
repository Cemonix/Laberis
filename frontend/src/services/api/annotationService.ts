import apiClient from './apiClient';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse } from '@/services/utils';
import type { PaginatedResponse, QueryParams } from '@/types/api';
import type { 
    Annotation, 
    AnnotationDto, 
    CreateAnnotationDto, 
    UpdateAnnotationDto
} from '@/types/annotation';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('AnnotationService');

// TODO: Refactor this service

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
            throw transformApiError(new Error('Invalid annotation coordinates format'), 
                'Failed to transform annotation - Invalid coordinates format');
        }

        return {
            annotationId: dto.id,
            annotationType: dto.annotationType as any, // TODO: Type assertion for now
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
            annotatorEmail: dto.annotatorEmail,
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
            annotatorEmail: annotation.annotatorEmail || '',
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
        options: QueryParams = {}
    ): Promise<PaginatedResponse<Annotation>> {
        logger.info(`Fetching annotations for asset ${assetId}`, options);

        try {
            const response = await apiClient.get<PaginatedResponse<AnnotationDto>>(
                `${this.baseUrl}/asset/${assetId}`,
                { params: options }
            );

            if (!isValidPaginatedResponse(response)) {
                throw transformApiError(new Error('Invalid paginated response structure'), 
                    'Failed to fetch annotations - Invalid response format');
            }

            logger.info(`Fetched ${response.data.data.length} annotations for asset ${assetId}`, response.data);
            
            // Transform backend DTOs to frontend types
            const transformedAnnotations = response.data.data.map(dto => this.transformAnnotation(dto));
            
            return {
                ...response.data,
                data: transformedAnnotations
            };
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch annotations');
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
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to create annotation - Invalid response format');
            }

            logger.info(`Annotation created successfully!`, response.data);
            return this.transformAnnotation(response.data);
        } catch (error) {
            throw transformApiError(error, 'Failed to create annotation');
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
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to update annotation - Invalid response format');
            }

            logger.info(`Annotation ${annotationId} updated successfully!`, response.data);
            return this.transformAnnotation(response.data);
        } catch (error) {
            throw transformApiError(error, `Failed to update annotation ${annotationId}`);
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
            await apiClient.delete(`${this.baseUrl}/${annotationId}`);
            logger.info(`Annotation ${annotationId} deleted successfully!`);
        } catch (error) {
            throw transformApiError(error, `Failed to delete annotation ${annotationId}`);
        }
    }
}

export default new AnnotationService();