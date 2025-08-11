import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomePage from '@/views/HomeView.vue';
import AnnotationWorkspace from '@/views/AnnotationWorkspace.vue';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import WorkspaceLayout from '@/layouts/WorkspaceLayout.vue';
import DataExplorerLayout from '@/layouts/DataExplorerLayout.vue';
import { useAuthStore } from '@/stores/authStore';
import { projectMemberService } from '@/services/api/projects/projectMemberService';
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

const logger = AppLogger.createServiceLogger('Router');

// Navigation guards
router.beforeEach(async (to, _from, next) => {
    const authStore = useAuthStore();
    
    const publicRoutes = ['Login', 'Register', 'Home', 'InviteAccept', 'EmailVerification'];    
    const authRoutes = ['Login', 'Register'];
    const isPublicRoute = publicRoutes.includes(to.name as string);
    
    // Only initialize auth for protected routes or if we already have some auth state
    if (!authStore.isInitialized && (!isPublicRoute || authStore.tokens)) {
        await authStore.initializeAuth();
    }
    
    // Check authentication first
    if (authRoutes.includes(to.name as string) && authStore.isAuthenticated) {
        // If user is already authenticated and trying to access auth pages, redirect to appropriate page
        const redirectUrl = authStore.getPostLoginRedirectUrl();
        next(redirectUrl);
    } else if (!isPublicRoute && !authStore.isAuthenticated) {
        // If user is not authenticated and trying to access protected routes, redirect to login
        next({ name: 'Login' });
    } else if (authStore.isAuthenticated && to.params.projectId) {
        // Project-specific route - validate project membership
        try {
            const projectId = Number(to.params.projectId);
            if (projectId && !isNaN(projectId)) {
                // Check if user is a member of the project
                const membership = await projectMemberService.getCurrentUserMembership(projectId);
                if (!membership) {
                    logger.warn(`User is not a member of project ${projectId}`);
                    next({ name: 'Error', params: { type: 'unauthorized' } });
                    return;
                }
                logger.info(`User has ${membership.role} role in project ${projectId}`);
            }
            next();
        } catch (error) {
            logger.error('Error validating project membership', error);
            // If validation fails, redirect to error page
            next({ name: 'Error', params: { type: 'unauthorized' } });
        }
    } else {
        next();
    }
});

export default router;
