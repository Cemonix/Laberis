import apiClient from './apiClient';
import { transformApiError, isValidPaginatedResponse } from '@/services/utils';
import type { PaginatedResponse } from '@/types/api';
import type { ProjectRole } from '@/types/project';
import type { ProjectMember, InviteMemberRequest } from '@/types/projectMember';

class ProjectMemberService {
    private getBaseUrl(projectId: number) {
        return `/projects/${projectId}/projectmembers`;
    }

    async getProjectMembers(projectId: number): Promise<ProjectMember[]> {
        try {
            const response = await apiClient.get<PaginatedResponse<ProjectMember>>(this.getBaseUrl(projectId));
            if (!isValidPaginatedResponse(response)) {
                throw new Error('Invalid response for project members.');
            }
            return response.data.data;
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch project members');
        }
    }

    async inviteMember(projectId: number, data: InviteMemberRequest): Promise<{ message: string }> {
        try {
            const response = await apiClient.post<{ message: string }>(`${this.getBaseUrl(projectId)}/invite`, data);
            return response.data;
        } catch (error) {
            throw transformApiError(error, 'Failed to invite member');
        }
    }

    async removeMember(projectId: number, email: string): Promise<void> {
        try {
            await apiClient.delete(`${this.getBaseUrl(projectId)}/remove-by-email`, {
                data: { email }
            });
        } catch (error) {
            throw transformApiError(error, 'Failed to remove member');
        }
    }

    async updateMemberRole(projectId: number, email: string, role: ProjectRole): Promise<ProjectMember> {
         try {
            const response = await apiClient.put<ProjectMember>(`${this.getBaseUrl(projectId)}/update-by-email`, { 
                email, 
                role 
            });
            return response.data;
        } catch (error) {
            throw transformApiError(error, 'Failed to update member role');
        }
    }
}

export const projectMemberService = new ProjectMemberService();