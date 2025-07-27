import { BaseProjectService } from '../base';
import type { PaginatedResponse, QueryParams } from '@/types/api';
import { 
    type Annotation, 
    AnnotationType,
    type CreateAnnotationDto, 
    type UpdateAnnotationDto,
    type AnnotationDto
} from '@/types/workspace/annotation';

/**
 * Service class for managing annotations within projects.
 */
class AnnotationService extends BaseProjectService {
    constructor() {
        super('AnnotationService');
    }

    /**
     * Validates and converts backend annotation type to frontend enum
     */
    private validateAnnotationType(backendType: string): AnnotationType {
        // Map backend string values to frontend enum
        const typeMapping: Record<string, AnnotationType> = {
            'POINT': AnnotationType.POINT,
            'LINE': AnnotationType.LINE,
            'BOUNDING_BOX': AnnotationType.BOUNDING_BOX,
            'POLYLINE': AnnotationType.POLYLINE,
            'POLYGON': AnnotationType.POLYGON,
            'TEXT': AnnotationType.TEXT,
        };
        
        const mappedType = typeMapping[backendType];
        if (!mappedType) {
            this.logger.warn(
                `Unknown annotation type from backend: ${backendType}. ` +
                `Expected one of: ${Object.keys(typeMapping).join(', ')}. ` +
                `Defaulting to POINT. Please check the backend data source for inconsistencies.`
            );
            return AnnotationType.POINT;
        }

        return mappedType;
    }

    /**
     * Converts AnnotationDto from backend to frontend Annotation object
     */
    private transformAnnotationDto(dto: AnnotationDto): Annotation {
        // Parse coordinates from data field
        let coordinates;
        try {
            if (dto.data) {
                coordinates = JSON.parse(dto.data);
            }
        } catch (error) {
            this.logger.warn(`Failed to parse annotation coordinates for annotation ${dto.id}:`, dto.data);
            coordinates = null;
        }

        return {
            annotationId: dto.id,
            assetId: dto.assetId,
            labelId: dto.labelId,
            taskId: dto.taskId,
            annotationType: this.validateAnnotationType(dto.annotationType),
            data: dto.data,
            coordinates: coordinates,
            notes: dto.notes,
            confidenceScore: dto.confidenceScore,
            isGroundTruth: dto.isGroundTruth,
            isPrediction: dto.isPrediction,
            version: dto.version,
            parentAnnotationId: dto.parentAnnotationId,
            createdAt: dto.createdAt,
            updatedAt: dto.updatedAt,
            annotatorEmail: dto.annotatorEmail
        };
    }

    /**
     * Gets all annotations for a specific asset within a project
     */
    async getAnnotationsForAsset(
        projectId: number,
        assetId: number,
        params: QueryParams = {}
    ): Promise<PaginatedResponse<Annotation>> {
        this.logger.info(`Fetching annotations for asset ${assetId} in project ${projectId}`, params);

        const url = this.buildProjectUrl(projectId, `annotations/asset/${assetId}`);
        const response = await this.getPaginated<AnnotationDto>(url, params);

        // Transform DTOs to frontend Annotation objects
        const annotations = response.data.map(dto => this.transformAnnotationDto(dto));

        this.logger.info(`Fetched ${annotations.length} annotations for asset ${assetId}`);

        return {
            ...response,
            data: annotations
        };
    }

    /**
     * Gets all annotations for a specific task within a project
     */
    async getAnnotationsForTask(
        projectId: number,
        taskId: number,
        params: QueryParams = {}
    ): Promise<PaginatedResponse<Annotation>> {
        this.logger.info(`Fetching annotations for task ${taskId} in project ${projectId}`, params);

        const url = this.buildProjectUrl(projectId, `annotations/task/${taskId}`);
        const response = await this.getPaginated<AnnotationDto>(url, params);

        const annotations = response.data.map(dto => this.transformAnnotationDto(dto));

        this.logger.info(`Fetched ${annotations.length} annotations for task ${taskId}`);

        return {
            ...response,
            data: annotations
        };
    }

    /**
     * Gets a single annotation by ID within a project
     */
    async getAnnotationById(projectId: number, annotationId: number): Promise<Annotation> {
        this.logger.info(`Fetching annotation ${annotationId} in project ${projectId}`);

        const url = this.buildProjectUrl(projectId, `annotations/${annotationId}`);
        const response = await this.get<AnnotationDto>(url);
        const annotation = this.transformAnnotationDto(response);

        this.logger.info(`Fetched annotation ${annotationId} successfully`);
        return annotation;
    }

    /**
     * Creates a new annotation within a project
     */
    async createAnnotation(projectId: number, annotationData: CreateAnnotationDto): Promise<Annotation> {
        this.logger.info(`Creating new annotation in project ${projectId}`, {
            assetId: annotationData.assetId,
            taskId: annotationData.taskId,
            annotationType: annotationData.annotationType,
            labelId: annotationData.labelId
        });

        const url = this.buildProjectUrl(projectId, 'annotations');
        
        const response = await this.post<CreateAnnotationDto, AnnotationDto>(url, annotationData);
        const annotation = this.transformAnnotationDto(response);

        this.logger.info(`Created annotation ${annotation.annotationId} for asset ${annotation.assetId} in project ${projectId}`);
        return annotation;
    }

    /**
     * Updates an existing annotation within a project
     */
    async updateAnnotation(
        projectId: number, 
        annotationId: number, 
        annotationData: UpdateAnnotationDto
    ): Promise<Annotation> {
        this.logger.info(`Updating annotation ${annotationId} in project ${projectId}`, annotationData);

        const url = this.buildProjectUrl(projectId, `annotations/${annotationId}`);
        const response = await this.put<UpdateAnnotationDto, AnnotationDto>(url, annotationData);
        const annotation = this.transformAnnotationDto(response);

        this.logger.info(`Updated annotation ${annotationId} successfully`);
        return annotation;
    }

    /**
     * Deletes an annotation by ID within a project
     */
    async deleteAnnotation(projectId: number, annotationId: number): Promise<void> {
        this.logger.info(`Deleting annotation ${annotationId} in project ${projectId}`);

        const url = this.buildProjectUrl(projectId, `annotations/${annotationId}`);
        await this.delete(url);

        this.logger.info(`Deleted annotation ${annotationId} successfully`);
    }

    /**
     * Bulk creates multiple annotations within a project
     */
    async createAnnotations(
        projectId: number, 
        annotationsData: CreateAnnotationDto[]
    ): Promise<Annotation[]> {
        this.logger.info(`Bulk creating ${annotationsData.length} annotations in project ${projectId}`);

        const url = this.buildProjectUrl(projectId, 'annotations/bulk');
        const response = await this.post<CreateAnnotationDto[], AnnotationDto[]>(url, annotationsData);
        const annotations = response.map(dto => this.transformAnnotationDto(dto));

        this.logger.info(`Created ${annotations.length} annotations successfully`);
        return annotations;
    }
}

export const annotationService = new AnnotationService();