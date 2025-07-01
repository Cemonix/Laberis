import type { ProjectRole } from '../project';

export interface ProjectInvitationDto {
    id: number;
    projectId: number;
    email: string;
    role: ProjectRole;
    invitationToken: string;
    expiresAt: string;
    isAccepted: boolean;
    invitedByUserId?: string;
    createdAt: string;
}
