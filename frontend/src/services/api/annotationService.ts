import type { Annotation } from '@/types/workspace/annotation';
import apiClient from './apiClient';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { ApiError } from '@/types/api/error';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('AnnotationService');

/**
 * Fetches annotations for a given asset from the backend with pagination.
 * @param assetId The ID of the asset whose annotations are to be fetched.
 * @param page The page number for pagination.
 * @param pageSize The number of items per page.
 * @returns A Promise that resolves with a PaginatedResponse of annotations.
 */
export async function fetchAnnotations(
    assetId: number | string,
    page: number = 1,
    pageSize: number = 10
): Promise<PaginatedResponse<Annotation>> {
    logger.info(`Fetching annotations for asset ${assetId}, page: ${page}, pageSize: ${pageSize}...`);
    try {
        const response = await apiClient.get<PaginatedResponse<Annotation>>(`/assets/${assetId}/annotations`, {
            params: { page, pageSize },
        });

        if (response && response.data && Array.isArray(response.data.data)) {
            logger.info(`Fetched annotations successfully for asset ${assetId}!`, response.data);
            return response.data;
        } else {
            logger.error(`Invalid response structure for asset ${assetId}.`, response);
            throw new Error('Invalid response structure from API for annotations.');
        }
    } catch (error) {
        const apiError = error as ApiError;
        logger.error(`Failed to fetch annotations for asset ${assetId}.`, apiError.response?.data || apiError.message);
        throw apiError;
    }
}

/**
 * Saves an annotation to the backend.
 * @param annotation The annotation object to save.
 * @returns A Promise that resolves with the saved annotation, including backend-generated fields like `annotationId`, `createdAt`, `updatedAt`.
 */
export async function saveAnnotation(annotation: Omit<Annotation, 'annotationId' | 'createdAt' | 'updatedAt'>): Promise<Annotation> {
    logger.info(`Saving annotation to backend...`, annotation);
    try {
        const response = await apiClient.post<Annotation>('/annotations', annotation);
        
        if (response && response.data && response.data.annotationId) {
            logger.info(`Annotation saved successfully!`, response.data);
            return response.data;
        } else {
            logger.error(`Invalid response structure after saving annotation.`, response);
            throw new Error('Invalid response structure from API after saving annotation.');
        }
    } catch (error) {
        const apiError = error as ApiError;
        logger.error(`Failed to save annotation.`, apiError.response?.data || apiError.message);
        throw apiError;
    }
}

/**
 * Updates an existing annotation on the backend.
 * @param annotationId The ID of the annotation to update.
 * @param annotationData The partial data to update the annotation with.
 * @returns A Promise that resolves with the updated annotation.
 */
export async function updateAnnotation(
    annotationId: number,
    annotationData: Partial<Omit<Annotation, 'annotationId' | 'assetId' | 'taskId' | 'createdAt' | 'updatedAt'>>
): Promise<Annotation> {
    logger.info(`Updating annotation ${annotationId}...`, annotationData);
    try {
        const response = await apiClient.put<Annotation>(`/annotations/${annotationId}`, annotationData);
        if (response && response.data && response.data.annotationId) {
            logger.info(`Annotation ${annotationId} updated successfully!`, response.data);
            return response.data;
        } else {
            logger.error(`Invalid response structure after updating annotation ${annotationId}.`, response);
            throw new Error('Invalid response structure from API after updating annotation.');
        }
    } catch (error) {
        const apiError = error as ApiError;
        logger.error(`Failed to update annotation ${annotationId}.`, apiError.response?.data || apiError.message);
        throw apiError;
    }
}

/**
 * Deletes an annotation from the backend.
 * @param annotationId The ID of the annotation to delete.
 * @returns A Promise that resolves when the annotation is successfully deleted.
 */
export async function deleteAnnotation(annotationId: number): Promise<void> {
    logger.info(`Deleting annotation ${annotationId}...`);
    try {
        const response = await apiClient.delete(`/annotations/${annotationId}`);
        if (response.status === 204 || response.status === 200) {
            logger.info(`Annotation ${annotationId} deleted successfully!`);
        } else {
            logger.error(`Unexpected status code after deleting annotation ${annotationId}.`, response);
            throw new Error('Unexpected response from API after deleting annotation.');
        }
    } catch (error) {
        const apiError = error as ApiError;
        logger.error(`Failed to delete annotation ${annotationId}.`, apiError.response?.data || apiError.message);
        throw apiError;
    }
}