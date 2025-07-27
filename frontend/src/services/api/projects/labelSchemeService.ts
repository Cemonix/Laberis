/**
 * Label Scheme Service - Handles API operations for label schemes
 */

import { BaseProjectService } from '../base';
import type { LabelSchemeResponse } from '@/types/label/responses';
import type {
    CreateLabelSchemeRequest,
    UpdateLabelSchemeRequest
} from '@/types/label/requests';
import type { LabelScheme, LabelSchemeDeletionImpact } from '@/types/label/labelScheme';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { QueryParams } from '@/types/api';

/**
 * Transforms a backend LabelSchemeResponse object to a frontend LabelScheme object.
 */
function transformLabelSchemeResponse(response: LabelSchemeResponse): LabelScheme {
    return {
        labelSchemeId: response.id,
        name: response.name,
        description: response.description,
        projectId: response.projectId,
        isDefault: response.isDefault,
        isActive: response.isActive,
        createdAt: response.createdAt,
        deletedAt: response.deletedAt,
    };
}

/**
 * Service class for managing label schemes.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class LabelSchemeService extends BaseProjectService {
    constructor() {
        super('LabelSchemeService');
    }

    /**
     * Retrieves all label schemes for a specific project with optional filtering and pagination.
     */
    async getLabelSchemesForProject(
        projectId: number,
        queryParams: QueryParams = {}
    ): Promise<PaginatedResponse<LabelScheme>> {
        this.logger.info('Fetching label schemes for project', { projectId, query: queryParams });

        const url = this.buildProjectUrl(projectId, 'labelschemes');
        const response = await this.getPaginated<LabelSchemeResponse>(url, queryParams);

        const schemes = response.data.map(transformLabelSchemeResponse);

        this.logger.info('Successfully fetched label schemes', {
            projectId,
            count: schemes.length,
            currentPage: response.currentPage,
            totalPages: response.totalPages
        });

        return {
            data: schemes,
            currentPage: response.currentPage,
            pageSize: response.pageSize,
            totalPages: response.totalPages,
            totalItems: response.totalItems
        };
    }

    /**
     * Retrieves a single label scheme by its ID.
     */
    async getLabelSchemeById(projectId: number, schemeId: number): Promise<LabelScheme> {
        this.logger.info('Fetching label scheme by ID', { projectId, schemeId });

        const url = this.buildProjectUrl(projectId, `labelschemes/${schemeId}`);
        const response = await this.get<LabelSchemeResponse>(url);
        const scheme = transformLabelSchemeResponse(response);

        this.logger.info('Successfully fetched label scheme', { projectId, schemeId, name: scheme.name });
        return scheme;
    }

    /**
     * Creates a new label scheme within a project.
     */
    async createLabelScheme(
        projectId: number,
        data: CreateLabelSchemeRequest
    ): Promise<LabelScheme> {
        this.logger.info('Creating new label scheme', { projectId, name: data.name });

        const url = this.buildProjectUrl(projectId, 'labelschemes');
        const response = await this.post<CreateLabelSchemeRequest, LabelSchemeResponse>(url, data);
        const scheme = transformLabelSchemeResponse(response);

        this.logger.info('Successfully created label scheme', {
            projectId,
            schemeId: scheme.labelSchemeId,
            name: scheme.name
        });

        return scheme;
    }

    /**
     * Updates an existing label scheme.
     */
    async updateLabelScheme(
        projectId: number,
        schemeId: number,
        data: UpdateLabelSchemeRequest
    ): Promise<LabelScheme> {
        this.logger.info('Updating label scheme', { projectId, schemeId, updates: data });

        const url = this.buildProjectUrl(projectId, `labelschemes/${schemeId}`);
        const response = await this.put<UpdateLabelSchemeRequest, LabelSchemeResponse>(url, data);
        const scheme = transformLabelSchemeResponse(response);

        this.logger.info('Successfully updated label scheme', {
            projectId,
            schemeId,
            name: scheme.name
        });

        return scheme;
    }

    /**
     * Deletes a label scheme from a project.
     */
    async deleteLabelScheme(projectId: number, schemeId: number): Promise<void> {
        this.logger.info('Deleting label scheme', { projectId, schemeId });

        const url = this.buildProjectUrl(projectId, `labelschemes/${schemeId}`);
        await this.delete(url);
        
        this.logger.info('Successfully deleted label scheme', { projectId, schemeId });
    }

    /**
     * Soft deletes a label scheme from a project, preserving annotations.
     */
    async softDeleteLabelScheme(projectId: number, schemeId: number): Promise<void> {
        this.logger.info('Soft deleting label scheme', { projectId, schemeId });

        const url = this.buildProjectUrl(projectId, `labelschemes/${schemeId}/soft-delete`);
        await this.delete(url);
        
        this.logger.info('Successfully soft deleted label scheme', { projectId, schemeId });
    }

    /**
     * Gets deletion impact statistics for a label scheme.
     */
    async getDeletionImpact(projectId: number, schemeId: number): Promise<LabelSchemeDeletionImpact> {
        this.logger.info('Fetching deletion impact for label scheme', { projectId, schemeId });

        const url = this.buildProjectUrl(projectId, `labelschemes/${schemeId}/deletion-impact`);
        const response = await this.get<LabelSchemeDeletionImpact>(url);
        
        this.logger.info('Successfully fetched deletion impact', { 
            projectId, 
            schemeId, 
            totalAnnotations: response.totalAnnotationsCount 
        });
        
        return response;
    }

    /**
     * Reactivates a soft-deleted label scheme.
     */
    async reactivateLabelScheme(projectId: number, schemeId: number): Promise<void> {
        this.logger.info('Reactivating label scheme', { projectId, schemeId });

        const url = this.buildProjectUrl(projectId, `labelschemes/${schemeId}/reactivate`);
        await this.patch(url, {}, false);
        
        this.logger.info('Successfully reactivated label scheme', { projectId, schemeId });
    }
}

export const labelSchemeService = new LabelSchemeService();