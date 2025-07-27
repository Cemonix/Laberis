import { BaseProjectService } from '../base';
import type { ProjectRole } from '@/types/project';
import type { ProjectMember, InviteMemberRequest } from '@/types/projectMember';

/**
 * Service class for managing project members.
 */
class ProjectMemberService extends BaseProjectService {
    constructor() {
        super('ProjectMemberService');
    }

    /**
     * Retrieves all members for a specific project
     */
    async getProjectMembers(projectId: number): Promise<ProjectMember[]> {
        this.logger.info(`Fetching project members for project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, 'projectmembers');
        const response = await this.getPaginated<ProjectMember>(url);
        
        this.logger.info(`Fetched ${response.data.length} project members for project ${projectId}`);
        return response.data;
    }

    /**
     * Invites a new member to the project
     */
    async inviteMember(projectId: number, data: InviteMemberRequest): Promise<{ message: string }> {
        this.logger.info(`Inviting member to project ${projectId}`, { email: data.email, role: data.role });
        
        const url = this.buildProjectUrl(projectId, 'projectmembers/invite');
        const response = await this.post<InviteMemberRequest, { message: string }>(url, data);
        
        this.logger.info(`Successfully invited member to project ${projectId}`, response);
        return response;
    }

    /**
     * Gets the current user's membership info for a project
     */
    async getCurrentUserMembership(projectId: number): Promise<ProjectMember | null> {
        this.logger.info(`Fetching current user membership for project ${projectId}`);
        
        try {
            const url = this.buildProjectUrl(projectId, 'projectmembers/my-membership');
            const response = await this.get<ProjectMember>(url);
            
            this.logger.info(`Current user role in project ${projectId}: ${response.role}`);
            return response;
        } catch (error) {
            this.logger.warn(`Current user is not a member of project ${projectId}`, error);
            return null;
        }
    }

    /**
     * Removes a member from the project by email
     */
    async removeMember(projectId: number, email: string): Promise<void> {
        this.logger.info(`Removing member from project ${projectId}`, { email });
        
        const url = this.buildProjectUrl(projectId, 'projectmembers/remove-by-email');
        
        // Note: Using apiClient directly for DELETE with request body
        // as our base service delete method doesn't support request bodies
        const apiClient = await import('../apiClient').then(m => m.default);
        await apiClient.delete(url, { data: { email } });
        
        this.logger.info(`Successfully removed member from project ${projectId}`, { email });
    }

    /**
     * Updates a member's role in the project
     */
    async updateMemberRole(projectId: number, email: string, role: ProjectRole): Promise<ProjectMember> {
        this.logger.info(`Updating member role in project ${projectId}`, { email, role });
        
        const url = this.buildProjectUrl(projectId, 'projectmembers/update-by-email');
        const response = await this.put<{ email: string; role: ProjectRole }, ProjectMember>(
            url, 
            { email, role }
        );
        
        this.logger.info(`Successfully updated member role in project ${projectId}`, response);
        return response;
    }
}

export const projectMemberService = new ProjectMemberService();