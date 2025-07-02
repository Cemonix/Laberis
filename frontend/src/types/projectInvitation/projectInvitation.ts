import type { ProjectRole } from '../project';

export interface ProjectInvitationDto {
    id: number;
    projectId: number;
    email: string;
    role: ProjectRole;
    expiresAt: string;
    isAccepted: boolean;
    invitedByEmail?: string;
    createdAt: string;
}
