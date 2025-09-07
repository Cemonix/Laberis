import { AppLogger } from '@/core/logger/logger';
import { annotationService } from '@/services/project';
import type { Annotation, CreateAnnotationDto } from '@/core/workspace/annotation.types';

const logger = AppLogger.createServiceLogger('AnnotationDataManager');

/**
 * Result of annotation operations
 */
export type AnnotationOperationResult<T = Annotation> = {
    success: true;
    data: T;
} | {
    success: false;
    error: string;
};

/**
 * AnnotationDataManager handles CRUD operations for annotations.
 * This is separate from the AnnotationManager in /core/annotations/ which handles interaction and drawing.
 */
export class AnnotationDataManager {
    /**
     * Load all annotations for a specific asset
     */
    async loadAnnotationsForAsset(projectId: number, assetId: number): Promise<AnnotationOperationResult<Annotation[]>> {
        try {
            logger.info(`Loading annotations for asset ${assetId} in project ${projectId}`);
            const response = await annotationService.getAnnotationsForAsset(projectId, assetId);
            return { success: true, data: response.data };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load annotations';
            logger.error(`Failed to load annotations for asset ${assetId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Create a new annotation
     */
    async createAnnotation(projectId: number, annotation: Annotation): Promise<AnnotationOperationResult<Annotation>> {
        try {
            logger.info(`Creating annotation for asset ${annotation.assetId}`);

            // Convert Annotation to CreateAnnotationDto
            const createDto: CreateAnnotationDto = {
                annotationType: annotation.annotationType,
                data: annotation.data || (annotation.coordinates ? JSON.stringify(annotation.coordinates) : '{}'),
                taskId: annotation.taskId,
                assetId: annotation.assetId,
                labelId: annotation.labelId,
                isPrediction: annotation.isPrediction || false,
                confidenceScore: annotation.confidenceScore,
                isGroundTruth: annotation.isGroundTruth,
                version: annotation.version || 1,
                notes: annotation.notes,
                annotatorEmail: annotation.annotatorEmail,
                parentAnnotationId: annotation.parentAnnotationId
            };

            // Save annotation to backend
            const savedAnnotation = await annotationService.createAnnotation(projectId, createDto);
            
            logger.info(`Successfully created annotation with ID: ${savedAnnotation.annotationId}`);
            return { success: true, data: savedAnnotation };

        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to create annotation';
            logger.error("Failed to create annotation:", error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Update an existing annotation
     */
    async updateAnnotation(
        projectId: number, 
        annotationId: number, 
        updates: Partial<Annotation>
    ): Promise<AnnotationOperationResult<Annotation>> {
        try {
            logger.info(`Updating annotation ${annotationId}`);

            // Prepare update payload for backend
            const updatePayload: any = {};
            if (updates.annotationType) updatePayload.annotationType = updates.annotationType;
            if (updates.coordinates) updatePayload.data = JSON.stringify(updates.coordinates);
            if (updates.isPrediction !== undefined) updatePayload.isPrediction = updates.isPrediction;
            if (updates.confidenceScore !== undefined) updatePayload.confidenceScore = updates.confidenceScore;
            if (updates.isGroundTruth !== undefined) updatePayload.isGroundTruth = updates.isGroundTruth;
            if (updates.notes !== undefined) updatePayload.notes = updates.notes;
            if (updates.labelId !== undefined) updatePayload.labelId = updates.labelId;

            // Update annotation on backend
            const updatedAnnotation = await annotationService.updateAnnotation(projectId, annotationId, updatePayload);
            
            logger.info(`Successfully updated annotation with ID: ${annotationId}`);
            return { success: true, data: updatedAnnotation };

        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to update annotation';
            logger.error(`Failed to update annotation ${annotationId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Delete an annotation
     */
    async deleteAnnotation(projectId: number, annotationId: number): Promise<AnnotationOperationResult<void>> {
        try {
            logger.info(`Deleting annotation ${annotationId}`);

            // Delete annotation on backend
            await annotationService.deleteAnnotation(projectId, annotationId);
            
            logger.info(`Successfully deleted annotation with ID: ${annotationId}`);
            return { success: true, data: undefined };

        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to delete annotation';
            logger.error(`Failed to delete annotation ${annotationId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Find annotation in a list by client ID
     */
    findAnnotationByClientId(annotations: Annotation[], clientId: string): number {
        return annotations.findIndex((a: Annotation) => a.clientId === clientId);
    }

    /**
     * Find annotation in a list by coordinates and properties (fallback matching)
     */
    findAnnotationByProperties(
        annotations: Annotation[], 
        targetAnnotation: Annotation
    ): number {
        return annotations.findIndex((a: Annotation) => 
            !a.annotationId && // Only match unsaved annotations
            a.assetId === targetAnnotation.assetId && 
            a.labelId === targetAnnotation.labelId &&
            a.annotationType === targetAnnotation.annotationType &&
            JSON.stringify(a.coordinates) === JSON.stringify(targetAnnotation.coordinates)
        );
    }

    /**
     * Update annotation in list with preserved client ID
     */
    updateAnnotationInList(
        annotations: Annotation[], 
        index: number, 
        savedAnnotation: Annotation, 
        originalClientId?: string
    ): Annotation[] {
        const updatedAnnotations = [...annotations];
        updatedAnnotations[index] = { 
            ...savedAnnotation, 
            clientId: originalClientId || updatedAnnotations[index].clientId 
        };
        return updatedAnnotations;
    }

    /**
     * Remove annotation from list by client ID
     */
    removeAnnotationByClientId(annotations: Annotation[], clientId: string): Annotation[] {
        return annotations.filter((a: Annotation) => a.clientId !== clientId);
    }

    /**
     * Remove annotation from list by annotation ID
     */
    removeAnnotationById(annotations: Annotation[], annotationId: number): Annotation[] {
        return annotations.filter((a: Annotation) => a.annotationId !== annotationId);
    }

    /**
     * Add annotation to list
     */
    addAnnotationToList(annotations: Annotation[], annotation: Annotation): Annotation[] {
        return [...annotations, annotation];
    }

    /**
     * Check if annotations exist for asset (used for task completion validation)
     */
    hasAnnotationsForAsset(annotations: Annotation[], assetId: number): boolean {
        return annotations.some((annotation: Annotation) => annotation.assetId === assetId);
    }

    /**
     * Check if annotations exist for task (used for task completion validation)
     */
    hasAnnotationsForTask(annotations: Annotation[], taskId: number): boolean {
        return annotations.some((annotation: Annotation) => annotation.taskId === taskId);
    }

    /**
     * Get annotations for a specific asset
     */
    getAnnotationsForAsset(annotations: Annotation[], assetId: number): Annotation[] {
        return annotations.filter((annotation: Annotation) => annotation.assetId === assetId);
    }

    /**
     * Get annotations for a specific task
     */
    getAnnotationsForTask(annotations: Annotation[], taskId: number): Annotation[] {
        return annotations.filter((annotation: Annotation) => annotation.taskId === taskId);
    }
}

// Export singleton instance
export const annotationDataManager = new AnnotationDataManager();