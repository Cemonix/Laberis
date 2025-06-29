import type { ProjectRole } from "../project"

export interface ProjectMember {
    id: number;
    role: ProjectRole;
    invitedAt: string;
    joinedAt?: string;
    createdAt: string;
    projectId: number;
    userId: string;
    userName?: string; 
    email?: string;
}

export interface InviteMemberRequest {
    email: string;
    role: ProjectRole;
}