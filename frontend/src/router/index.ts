import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomePage from '@/views/HomeView.vue';
import AnnotationWorkspace from '@/views/AnnotationWorkspace.vue';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import WorkspaceLayout from '@/layouts/WorkspaceLayout.vue';
import DataExplorerLayout from '@/layouts/DataExplorerLayout.vue';
import { useAuthStore } from '@/stores/authStore';
import { authorizationService } from '@/services/auth';
import { AppLogger } from '@/core/logger/logger';
import { PUBLIC_ROUTE_NAMES, AUTH_ROUTE_NAMES } from './routes';
import { PERMISSIONS } from '@/services/auth/permissions.types';

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
            permissions: [PERMISSIONS.TASK.READ],
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
            permissions: [PERMISSIONS.PROJECT.READ],
        },
        children: [
            {
                path: '',
                name: 'ProjectDashboard',
                component: () => import('@/views/project/ProjectDashboardView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.PROJECT.READ] },
            },
            {
                path: 'label-schemes',
                name: 'ProjectLabels',
                component: () => import('@/views/project/LabelSchemesView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.LABEL_SCHEME.READ] },
            },
            {
                path: 'data-sources',
                name: 'ProjectDataSources',
                component: () => import('@/views/project/DataSourcesView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.DATA_SOURCE.UPDATE] },
            },
            {
                path: 'data-explorer/:dataSourceId',
                name: 'DataExplorer',
                component: () => import('@/views/dataExplorer/DataExplorerView.vue'),
                props: true,
                meta: {
                    layout: DataExplorerLayout,
                    permissions: [PERMISSIONS.DATA_SOURCE.UPDATE],
                }
            },
            {
                path: 'workflows',
                name: 'ProjectWorkflows',
                component: () => import('@/views/project/WorkflowsView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.WORKFLOW.READ] },
            },
            {
                path: 'workflows/:workflowId/pipeline',
                name: 'WorkflowPipeline',
                component: () => import('@/views/project/WorkflowPipelineView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.WORKFLOW.UPDATE] },
            },
            {
                path: 'workflows/:workflowId/stages/:stageId/tasks',
                name: 'StageTasks',
                component: () => import('@/views/project/TasksView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.TASK.READ] },
            },
            {
                path: 'settings',
                name: 'ProjectSettings',
                component: () => import('@/views/project/ProjectSettingsView.vue'),
                props: true,
                meta: { permissions: [PERMISSIONS.PROJECT_SETTINGS.READ] },
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

const logger = AppLogger.createServiceLogger('Router');

// Navigation guards
router.beforeEach(async (to, from, next) => {
    const authStore = useAuthStore();
    
    const isPublicRoute = PUBLIC_ROUTE_NAMES.includes(to.name as typeof PUBLIC_ROUTE_NAMES[number]);
    const isAuthRoute = AUTH_ROUTE_NAMES.includes(to.name as typeof AUTH_ROUTE_NAMES[number]);
    
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

    // Check authentication and handle redirects
    if (isAuthRoute && authStore.isAuthenticated) {
        // Only redirect from auth pages (Login, Register) when user is already authenticated
        const redirectUrl = authStore.getPostLoginRedirectUrl();
        next(redirectUrl);
    } else if (!isPublicRoute && !authStore.isAuthenticated) {
        // If user is not authenticated and trying to access protected routes, redirect to login
        next({ name: 'Login' });
    } else if (authStore.isAuthenticated) {
        // If route defines permissions, verify via backend authorize (batch)
        const requiredPermissions = (to.meta?.permissions as string[] | undefined) || [];
        if (requiredPermissions.length === 0) {
            next();
            return;
        }

        try {
            const projectId = to.params.projectId ? Number(to.params.projectId) : undefined;
            const checks = requiredPermissions.map(p => ({ permission: p, context: projectId ? { projectId } : undefined }));
            const { results } = await authorizationService.authorizeBatch(checks);
            const allow = results.every(Boolean);
            if (!allow) {
                logger.warn(`Permission denied for route ${String(to.name)} with perms: ${requiredPermissions.join(', ')}`);
                next({ name: 'Error', params: { type: 'forbidden' } });
                return;
            }
            next();
        } catch (error) {
            logger.error('Authorization check failed', error);
            next({ name: 'Error', params: { type: 'unauthorized' } });
        }
    } else {
        next();
    }
});

export default router;
