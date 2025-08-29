/**
 * User permission context containing all applicable permissions for a user
 */
export interface UserPermissionContext {
    permissions: string[];
    projectPermissions: Record<number, string[]>;
    globalPermissions: string[];
}

/**
 * Permission directive binding interface for v-permission directive
 */
export interface PermissionDirectiveBinding {
    permission?: string;
    permissions?: string[];
    project?: number;
    mode?: 'all' | 'any'; // 'all' requires all permissions, 'any' requires at least one
    global?: boolean; // true for global permissions, false/undefined for project permissions
}

/**
 * Common permission strings used throughout the application
 */
export const PERMISSIONS = {
    // Global permissions
    ACCOUNT: {
        READ: 'account:read',
        UPDATE: 'account:update',
        CHANGE_PASSWORD: 'account:change:password'
    },

    // Project permissions
    PROJECT: {
        CREATE: 'project:create',
        READ: 'project:read',
        UPDATE: 'project:update',
        DELETE: 'project:delete',
        LIST: 'project:list'
    },

    PROJECT_MEMBER: {
        INVITE: 'projectMember:invite',
        READ: 'projectMember:read',
        UPDATE: 'projectMember:update',
        REMOVE: 'projectMember:remove'
    },

    PROJECT_SETTINGS: {
        READ: 'projectSettings:read',
        UPDATE: 'projectSettings:update'
    },

    LABEL_SCHEME: {
        CREATE: 'labelScheme:create',
        READ: 'labelScheme:read',
        UPDATE: 'labelScheme:update',
        DELETE: 'labelScheme:delete'
    },

    DATA_SOURCE: {
        CREATE: 'dataSource:create',
        READ: 'dataSource:read',
        UPDATE: 'dataSource:update',
        DELETE: 'dataSource:delete'
    },

    DATA_EXPLORER: {
        READ: 'dataExplorer:read'
    },

    WORKFLOW: {
        CREATE: 'workflow:create',
        READ: 'workflow:read',
        UPDATE: 'workflow:update',
        DELETE: 'workflow:delete'
    },

    TASK: {
        READ: 'task:read',
        ASSIGN: 'task:assign',
        UPDATE_STATUS: 'task:update:status',
        RETURN_FOR_REWORK: 'task:return:for:rework'
    },

    ANNOTATION: {
        CREATE: 'annotation:create',
        READ: 'annotation:read',
        UPDATE: 'annotation:update',
        DELETE: 'annotation:delete',
        REVIEW: 'annotation:review'
    }
} as const;