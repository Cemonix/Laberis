import { ProjectRole } from '@/types/project';
import { WorkflowStageType } from '@/types/workflow';

/**
 * Defines which project roles are allowed to be assigned to which workflow stage types
 */
export const ROLE_STAGE_MAPPING: Record<WorkflowStageType, ProjectRole[]> = {
    [WorkflowStageType.ANNOTATION]: [ProjectRole.ANNOTATOR, ProjectRole.MANAGER],
    [WorkflowStageType.REVISION]: [ProjectRole.REVIEWER, ProjectRole.MANAGER], 
    [WorkflowStageType.COMPLETION]: [ProjectRole.MANAGER]
};

/**
 * Checks if a project role is allowed to be assigned to a specific workflow stage type
 */
export function canRoleBeAssignedToStageType(role: ProjectRole, stageType: WorkflowStageType): boolean {
    const allowedRoles = ROLE_STAGE_MAPPING[stageType];
    return allowedRoles.includes(role);
}

/**
 * Gets all roles that are allowed to be assigned to a specific workflow stage type
 */
export function getAllowedRolesForStageType(stageType: WorkflowStageType): ProjectRole[] {
    return ROLE_STAGE_MAPPING[stageType] || [];
}

/**
 * Gets all stage types that a specific role can be assigned to
 */
export function getAllowedStageTypesForRole(role: ProjectRole): WorkflowStageType[] {
    const allowedStageTypes: WorkflowStageType[] = [];
    
    for (const [stageType, allowedRoles] of Object.entries(ROLE_STAGE_MAPPING)) {
        if (allowedRoles.includes(role)) {
            allowedStageTypes.push(stageType as WorkflowStageType);
        }
    }
    
    return allowedStageTypes;
}

/**
 * Filters project members to only include those whose roles are compatible with the stage type
 */
export function filterMembersByStageType<T extends { role: ProjectRole }>(
    members: T[], 
    stageType: WorkflowStageType
): T[] {
    const allowedRoles = getAllowedRolesForStageType(stageType);
    return members.filter(member => allowedRoles.includes(member.role));
}

/**
 * Gets a human-readable description of which roles can be assigned to a stage type
 */
export function getRoleDescriptionForStageType(stageType: WorkflowStageType): string {
    const allowedRoles = getAllowedRolesForStageType(stageType);
    const roleNames = allowedRoles.map(role => {
        switch (role) {
            case ProjectRole.ANNOTATOR: return 'Annotators';
            case ProjectRole.REVIEWER: return 'Reviewers';
            case ProjectRole.MANAGER: return 'Managers';
            case ProjectRole.VIEWER: return 'Viewers';
            default: return role;
        }
    });
    
    if (roleNames.length === 1) {
        return roleNames[0];
    } else if (roleNames.length === 2) {
        return `${roleNames[0]} and ${roleNames[1]}`;
    } else {
        return `${roleNames.slice(0, -1).join(', ')}, and ${roleNames[roleNames.length - 1]}`;
    }
}