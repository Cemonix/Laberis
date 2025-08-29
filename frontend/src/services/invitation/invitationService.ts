import { BaseService } from '../base/baseService';
import type { ProjectInvitationDto } from './invitation.types';

/**
 * Service class for managing project invitations.
 * Extends BaseService to inherit common functionality.
 */
class InvitationService extends BaseService {
    protected readonly baseUrl = '/invitations';

    constructor() {
        super('InvitationService');
    }

    /**
     * Validates an invitation token
     */
    async validateInvitationToken(token: string): Promise<ProjectInvitationDto> {
        this.logger.info('Validating invitation token', { tokenLength: token.length });

        try {
            const encodedToken = encodeURIComponent(token);
            const url = this.getBaseUrl(`validate/${encodedToken}`);
            const response = await this.get<ProjectInvitationDto>(url);

            this.logger.info('Invitation token validated successfully');
            return response;
        } catch (error: any) {
            // Handle 404 as expected business logic (invalid/expired token)
            if (error.response?.status === 404) {
                this.logger.warn('Invitation token not found or expired');
                throw new Error('');
            }
            
            this.logger.error('Failed to validate invitation token', error);
            throw error;
        }
    }

    /**
     * Accepts an invitation for an existing authenticated user
     */
    async acceptInvitation(token: string): Promise<void> {
        this.logger.info('Accepting invitation', { tokenLength: token.length });

        try {
            const encodedToken = encodeURIComponent(token);
            const url = this.getBaseUrl(`accept/${encodedToken}`);
            await this.post<void, void>(url, undefined, false);

            this.logger.info('Invitation accepted successfully');
        } catch (error: any) {
            // Handle specific error cases
            if (error.response?.status === 404) {
                this.logger.warn('Invitation token not found when accepting');
                throw new Error('Invalid or expired invitation token');
            }
            
            if (error.response?.status === 409) {
                this.logger.warn('User already member of project');
                throw new Error('You are already a member of this project');
            }

            this.logger.error('Failed to accept invitation', error);
            throw error;
        }
    }
}

export const invitationService = new InvitationService();