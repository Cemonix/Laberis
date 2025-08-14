import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomePage from '@/views/HomeView.vue';
import AnnotationWorkspace from '@/views/AnnotationWorkspace.vue';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import WorkspaceLayout from '@/layouts/WorkspaceLayout.vue';
import DataExplorerLayout from '@/layouts/DataExplorerLayout.vue';
import { useAuthStore } from '@/stores/authStore';
import { usePermissionStore } from '@/stores/permissionStore';
import { useNavigationStore } from '@/stores/navigationStore';
import { AppLogger } from '@/utils/logger';

const routes: Array<RouteRecordRaw> = [
    {
        path: '/',
        redirect: '/home',
    },
    {
        path: '/home',
        name: 'Home',
        component: HomePage,
        meta: {
            layout: DefaultLayout,
        }
    },
    // Authentication routes
    {
        path: '/login',
        name: 'Login',
        component: () => import('@/views/auth/LoginView.vue'),
        meta: {
            layout: DefaultLayout,
        }
    },
    {
        path: '/register',
        name: 'Register',
        component: () => import('@/views/auth/RegisterView.vue'),
        meta: {
            layout: DefaultLayout,
        }
    },
    {
        path: '/invite/accept/:token',
        name: 'InviteAccept',
        component: () => import('@/views/auth/InviteAcceptView.vue'),
        meta: {
            layout: DefaultLayout,
        }
    },
    {
        path: '/verify-email',
        name: 'EmailVerification',
        component: () => import('@/views/auth/EmailVerificationView.vue'),
        meta: {
            layout: DefaultLayout,
        }
    },
    // Account management routes
    {
        path: '/account',
        name: 'Account',
        component: () => import('@/views/AccountView.vue'),
        meta: {
            layout: DefaultLayout,
        },
        children: [
            {
                path: 'change-password',
                name: 'ChangePassword',
                component: () => import('@/views/account/ChangePasswordView.vue'),
            }
        ]
    },
    // Workspace and project routes
    {
        
        path: '/workspace/project/:projectId/asset/:assetId',
        name: 'AnnotationWorkspace',
        component: AnnotationWorkspace,
        props: true,
        meta: {
            layout: WorkspaceLayout,
        }
    },
    {
        path: '/projects',
        name: 'ProjectList',
        component: () => import('@/views/ProjectView.vue'),
        meta: {
            layout: DefaultLayout,
        }
    },
    {
        path: '/projects/:projectId',
        name: 'ProjectDetail',
        component: () => import('@/views/project/ProjectDetailView.vue'),
        props: true,
        meta: {
            layout: DefaultLayout,
        },
        children: [
            {
                path: '',
                name: 'ProjectDashboard',
                component: () => import('@/views/project/ProjectDashboardView.vue'),
                props: true,
            },
            {
                path: 'label-schemes',
                name: 'ProjectLabels',
                component: () => import('@/views/project/LabelSchemesView.vue'),
                props: true,
            },
            {
                path: 'data-sources',
                name: 'ProjectDataSources',
                component: () => import('@/views/project/DataSourcesView.vue'),
                props: true,
            },
            {
                path: 'data-explorer/:dataSourceId',
                name: 'DataExplorer',
                component: () => import('@/views/dataExplorer/DataExplorerView.vue'),
                props: true,
                meta: {
                    layout: DataExplorerLayout,
                }
            },
            {
                path: 'workflows',
                name: 'ProjectWorkflows',
                component: () => import('@/views/project/WorkflowsView.vue'),
                props: true,
            },
            {
                path: 'workflows/:workflowId/pipeline',
                name: 'WorkflowPipeline',
                component: () => import('@/views/project/WorkflowPipelineView.vue'),
                props: true,
            },
            {
                path: 'workflows/:workflowId/stages/:stageId/tasks',
                name: 'StageTasks',
                component: () => import('@/views/project/TasksView.vue'),
                props: true,
            },
            {
                path: 'settings',
                name: 'ProjectSettings',
                component: () => import('@/views/project/ProjectSettingsView.vue'),
                props: true,
            },
        ],
    },
    {
        path: '/error',
        redirect: '/error/general',
    },
    {
        path: '/error/:type',
        name: 'Error',
        component: () => import('@/views/ErrorView.vue'),
        props: true,
        meta: {
            layout: DefaultLayout,
        }
    }
];

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
});

const publicRoutes = ['Login', 'Register', 'Home', 'InviteAccept', 'EmailVerification'];
const authRoutes = ['Login', 'Register'];

const logger = AppLogger.createServiceLogger('Router');

// Navigation guards
router.beforeEach(async (to, from, next) => {
    const authStore = useAuthStore();
    const permissionStore = usePermissionStore();
    const navigationStore = useNavigationStore();
    
    const isPublicRoute = publicRoutes.includes(to.name as string);
    const isAuthRoute = authRoutes.includes(to.name as string);
    
    // Initialize auth only when:
    // 1. Accessing a protected route, OR
    // 2. We already have tokens in memory (user might be logged in), OR
    // 3. This is the first navigation (page refresh/initial load) - we need to check for existing session
    if (!authStore.isInitialized) {
        const hasValidTokensInMemory = authStore.hasValidTokensInMemory;
        const isFirstNavigation = from.name === undefined; // Initial load/refresh
        const shouldInitializeAuth = !isPublicRoute || hasValidTokensInMemory || isFirstNavigation;

        if (shouldInitializeAuth) {
            logger.info(`Initializing auth for route: ${String(to.name)} (public: ${isPublicRoute}, hasTokens: ${hasValidTokensInMemory}, firstNav: ${isFirstNavigation})`);
            try {
                await authStore.initializeAuth();
                
                // After initialization, check if we actually got authenticated for protected routes
                if (!isPublicRoute && !authStore.isAuthenticated) {
                    logger.info(`Auth initialization completed but user not authenticated for protected route: ${String(to.name)}`);
                    next({ name: 'Login' });
                    return;
                }
            } catch (error) {
                logger.warn('Auth initialization failed with exception', error);
                // Clear any partial auth state
                authStore.clearAuthState();
                
                // If we failed to initialize auth for a protected route, redirect to login
                if (!isPublicRoute) {
                    next({ name: 'Login' });
                    return;
                }
            }
        } else {
            // This should rarely happen now, but keep as fallback
            logger.info(`Skipping auth initialization for route: ${String(to.name)}`);
            authStore.isInitialized = true;
        }
    }
    
    // Show loading for navigation between different routes (not auth initialization)
    if (authStore.isInitialized && from.name !== to.name && to.name !== 'Error') {
        navigationStore.startNavigation(getNavigationMessage(to.name as string));
    }

    // Check authentication and handle redirects
    if (isAuthRoute && authStore.isAuthenticated) {
        // Only redirect from auth pages (Login, Register) when user is already authenticated
        const redirectUrl = authStore.getPostLoginRedirectUrl();
        next(redirectUrl);
    } else if (!isPublicRoute && !authStore.isAuthenticated) {
        // If user is not authenticated and trying to access protected routes, redirect to login
        next({ name: 'Login' });
    } else if (authStore.isAuthenticated && to.params.projectId) {
        // Project-specific route - validate project membership using permission store
        try {
            const projectId = Number(to.params.projectId);
            if (projectId && !isNaN(projectId)) {
                // Ensure permissions are loaded
                if (!permissionStore.isInitialized) {
                    await permissionStore.loadUserPermissions();
                }
                
                // Check if user is a member of the project
                if (!permissionStore.isProjectMember(projectId)) {
                    logger.warn(`User is not a member of project ${projectId}`);
                    next({ name: 'Error', params: { type: 'unauthorized' } });
                    return;
                }
                
                // Check if route requires specific permissions
                const requiredPermissions = to.meta?.permissions as string[] | undefined;
                if (requiredPermissions && requiredPermissions.length > 0) {
                    const hasRequiredPermissions = permissionStore.hasAllProjectPermissions(
                        requiredPermissions, 
                        projectId
                    );
                    
                    if (!hasRequiredPermissions) {
                        logger.warn(
                            `User lacks required permissions for route ${String(to.name)}: ${requiredPermissions.join(', ')}`
                        );
                        next({ name: 'Error', params: { type: 'forbidden' } });
                        return;
                    }
                }
                
                logger.info(`User has access to project ${projectId}`);
            }
            next();
        } catch (error) {
            logger.error('Error validating project access', error);
            // If validation fails, redirect to error page
            next({ name: 'Error', params: { type: 'unauthorized' } });
        }
    } else if (authStore.isAuthenticated && to.meta?.permissions) {
        // Global route with permission requirements
        try {
            // Ensure permissions are loaded
            if (!permissionStore.isInitialized) {
                await permissionStore.loadUserPermissions();
            }
            
            const requiredPermissions = to.meta.permissions as string[];
            const hasRequiredPermissions = permissionStore.hasAllGlobalPermissions(requiredPermissions);
            
            if (!hasRequiredPermissions) {
                logger.warn(
                    `User lacks required global permissions for route ${String(to.name)}: ${requiredPermissions.join(', ')}`
                );
                next({ name: 'Error', params: { type: 'forbidden' } });
                return;
            }
            
            next();
        } catch (error) {
            logger.error('Error validating global permissions', error);
            next({ name: 'Error', params: { type: 'unauthorized' } });
        }
    } else {
        next();
    }
});

// Hide loading spinner when navigation completes
router.afterEach(() => {
    const navigationStore = useNavigationStore();
    navigationStore.finishNavigation();
});

// Helper function to get navigation message
function getNavigationMessage(routeName: string): string {
    const messages: Record<string, string> = {
        'ProjectDetail': 'Loading project...',
        'ProjectList': 'Loading projects...',
        'TasksView': 'Loading tasks...',
        'WorkflowPipelineView': 'Loading pipeline...',
        'DataExplorerView': 'Loading data explorer...',
        'LabelSchemesView': 'Loading label schemes...',
        'ProjectSettingsView': 'Loading settings...',
        'AnnotationWorkspace': 'Loading workspace...',
        'Account': 'Loading account...',
        'Home': 'Loading dashboard...',
    };
    
    return messages[routeName] || 'Loading page...';
}

export default router;
