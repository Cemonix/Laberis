import type { ProjectRole } from "./project.types";

export interface ProjectMember {
    id: number;
    role: ProjectRole;
    invitedAt: string;
    joinedAt?: string;
    createdAt: string;
    updatedAt: string;
    projectId: number;
    email: string;
    userName?: string;
}

export interface InviteMemberRequest {
    email: string;
    role: ProjectRole;
}