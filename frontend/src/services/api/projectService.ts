import apiClient from './apiClient';
import type { ApiResponse, PaginatedResponse } from '@/types/api/responses';
import type { Project } from '@/types/project/project';
import type { 
    CreateProjectRequest, 
    UpdateProjectRequest, 
    ProjectListParams,
    ProjectStatsResponse
} from '@/types/project/requests';

class ProjectService {
    private readonly baseUrl = '/projects';

    /**
     * Get all projects with optional filtering and pagination
     */
    async getProjects(params?: ProjectListParams): Promise<PaginatedResponse<Project>> {
        console.log('[ProjectService] Fetching projects with params:', params);
        
        try {
            const response = await apiClient.get<PaginatedResponse<Project>>(this.baseUrl, {
                params
            });
            
            console.log('[ProjectService] Projects fetched successfully:', response.data);
            return response.data;
        } catch (error) {
            console.error('[ProjectService] Failed to fetch projects:', error);
            throw error;
        }
    }

    /**
     * Get a single project by ID
     */
    async getProject(projectId: number): Promise<Project> {
        console.log('[ProjectService] Fetching project:', projectId);
        
        try {
            const response = await apiClient.get<Project>(`${this.baseUrl}/${projectId}`);
            
            console.log('[ProjectService] Project fetched successfully:', response.data);
            return response.data;
        } catch (error) {
            console.error('[ProjectService] Failed to fetch project:', error);
            throw error;
        }
    }

    /**
     * Create a new project
     */
    async createProject(projectData: CreateProjectRequest): Promise<Project> {
        console.log('[ProjectService] Creating project:', projectData);
        
        try {
            const response = await apiClient.post<Project>(this.baseUrl, projectData);
            
            console.log('[ProjectService] Project created successfully:', response.data);
            return response.data;
        } catch (error) {
            console.error('[ProjectService] Failed to create project:', error);
            throw error;
        }
    }

    /**
     * Update an existing project
     */
    async updateProject(projectId: number, projectData: UpdateProjectRequest): Promise<Project> {
        console.log('[ProjectService] Updating project:', projectId, projectData);
        
        try {
            const response = await apiClient.put<Project>(`${this.baseUrl}/${projectId}`, projectData);
            
            console.log('[ProjectService] Project updated successfully:', response.data);
            return response.data;
        } catch (error) {
            console.error('[ProjectService] Failed to update project:', error);
            throw error;
        }
    }

    /**
     * Delete a project (soft delete - archives the project)
     */
    async deleteProject(projectId: number): Promise<void> {
        console.log('[ProjectService] Deleting project:', projectId);
        
        try {
            await apiClient.delete(`${this.baseUrl}/${projectId}`);
            
            console.log('[ProjectService] Project deleted successfully');
        } catch (error) {
            console.error('[ProjectService] Failed to delete project:', error);
            throw error;
        }
    }

    /**
     * Archive a project (change status to archived)
     */
    async archiveProject(projectId: number): Promise<Project> {
        console.log('[ProjectService] Archiving project:', projectId);
        
        try {
            const response = await apiClient.patch<ApiResponse<Project>>(`${this.baseUrl}/${projectId}/archive`);
            
            console.log('[ProjectService] Project archived successfully:', response.data.data);
            return response.data.data;
        } catch (error) {
            console.error('[ProjectService] Failed to archive project:', error);
            throw error;
        }
    }

    /**
     * Restore an archived project
     */
    async restoreProject(projectId: number): Promise<Project> {
        console.log('[ProjectService] Restoring project:', projectId);
        
        try {
            const response = await apiClient.patch<ApiResponse<Project>>(`${this.baseUrl}/${projectId}/restore`);
            
            console.log('[ProjectService] Project restored successfully:', response.data.data);
            return response.data.data;
        } catch (error) {
            console.error('[ProjectService] Failed to restore project:', error);
            throw error;
        }
    }

    /**
     * Get project statistics and progress information
     */
    async getProjectStats(projectId: number): Promise<ProjectStatsResponse> {
        console.log('[ProjectService] Fetching project stats:', projectId);
        
        try {
            const response = await apiClient.get<ApiResponse<ProjectStatsResponse>>(`${this.baseUrl}/${projectId}/stats`);
            
            console.log('[ProjectService] Project stats fetched successfully:', response.data.data);
            return response.data.data;
        } catch (error) {
            console.error('[ProjectService] Failed to fetch project stats:', error);
            throw error;
        }
    }

    /**
     * Duplicate an existing project
     */
    async duplicateProject(projectId: number, newName: string): Promise<Project> {
        console.log('[ProjectService] Duplicating project:', projectId, 'with new name:', newName);
        
        try {
            const response = await apiClient.post<ApiResponse<Project>>(`${this.baseUrl}/${projectId}/duplicate`, {
                name: newName
            });
            
            console.log('[ProjectService] Project duplicated successfully:', response.data.data);
            return response.data.data;
        } catch (error) {
            console.error('[ProjectService] Failed to duplicate project:', error);
            throw error;
        }
    }

    /**
     * Export project data
     */
    async exportProject(projectId: number, format: 'json' | 'csv' | 'coco'): Promise<Blob> {
        console.log('[ProjectService] Exporting project:', projectId, 'in format:', format);
        
        try {
            const response = await apiClient.get(`${this.baseUrl}/${projectId}/export`, {
                params: { format },
                responseType: 'blob'
            });
            
            console.log('[ProjectService] Project exported successfully');
            return response.data;
        } catch (error) {
            console.error('[ProjectService] Failed to export project:', error);
            throw error;
        }
    }
}

export const projectService = new ProjectService();
