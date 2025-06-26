import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomePage from '@/views/HomeView.vue';
import AnnotationWorkspace from '@/views/AnnotationWorkspace.vue';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import WorkspaceLayout from '@/layouts/WorkspaceLayout.vue';
import DataExplorerLayout from '@/layouts/DataExplorerLayout.vue';
import { useAuthStore } from '@/stores/authStore';

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
    // Account management routes
    {
        path: '/account',
        name: 'Account',
        component: () => import('@/views/AccountView.vue'),
        meta: {
            layout: DefaultLayout,
        }
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
                component: { template: '<div>Workflows Management Page</div>' },
                props: true,
            },
            {
                path: 'settings',
                name: 'ProjectSettings',
                component: { template: '<div>Project Settings Page</div>' },
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

// Navigation guards
router.beforeEach(async (to, _from, next) => {
    const authStore = useAuthStore();
    
    // Ensure auth store is initialized on page refresh
    if (!authStore.isInitialized) {
        await authStore.initializeAuth();
    }
    
    const publicRoutes = ['Login', 'Register', 'Home'];    
    const authRoutes = ['Login', 'Register'];
    
    if (authRoutes.includes(to.name as string) && authStore.isAuthenticated) {
        // If user is already authenticated and trying to access auth pages, redirect to home
        next({ name: 'Home' });
    } else if (!publicRoutes.includes(to.name as string) && !authStore.isAuthenticated) {
        // If user is not authenticated and trying to access protected routes, redirect to login
        next({ name: 'Login' });
    } else {
        next();
    }
});

export default router;
