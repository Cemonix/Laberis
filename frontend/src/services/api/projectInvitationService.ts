import apiClient from './apiClient';
import { transformApiError, isValidApiResponse } from '@/services/utils';
import type { ProjectInvitationDto } from '@/types/projectInvitation';

class ProjectInvitationService {
    /**
     * Validates an invitation token
     * @param token The invitation token to validate
     * @returns The invitation details if valid, throws error if invalid
     */
    async validateInvitationToken(token: string): Promise<ProjectInvitationDto> {
        try {
            const encodedToken = encodeURIComponent(token);
            const response = await apiClient.get<ProjectInvitationDto>(`/invitations/validate/${encodedToken}`);

            if (!isValidApiResponse(response)) {
                throw new Error('Invalid response format');
            }

            return response.data;
        } catch (error: any) {
            // Handle 404 as expected business logic (invalid/expired token)
            if (error.response?.status === 404) {
                throw new Error('');
            }
            
            // Handle other errors with the standard error transform
            throw transformApiError(error, 'Failed to validate invitation token');
        }
    }

    /**
     * Accepts an invitation for an existing authenticated user
     * @param token The invitation token to accept
     * @returns Success response
     */
    async acceptInvitation(token: string): Promise<void> {
        try {
            const encodedToken = encodeURIComponent(token);
            const response = await apiClient.post(`/invitations/accept/${encodedToken}`);

            if (!isValidApiResponse(response)) {
                throw new Error('Invalid response format');
            }
        } catch (error: any) {
            // Handle specific error cases
            if (error.response?.status === 404) {
                throw new Error('Invalid or expired invitation token');
            }
            
            if (error.response?.status === 400) {
                throw new Error('Invalid, expired, or already accepted invitation token');
            }

            if (error.response?.status === 409) {
                throw new Error('You are already a member of this project');
            }
            
            // Handle other errors with the standard error transform
            throw transformApiError(error, 'Failed to accept invitation');
        }
    }
}

export const projectInvitationService = new ProjectInvitationService();
