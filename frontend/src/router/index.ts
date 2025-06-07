import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomePage from '@/views/HomeView.vue';
import AnnotationWorkspace from '@/views/AnnotationWorkspace.vue';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import WorkspaceLayout from '@/layouts/WorkspaceLayout.vue';

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
        component: () => import('@/views/project/ProjectListView.vue'),
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
                path: 'workflows',
                name: 'ProjectWorkflows',
                component: { template: '<div>Workflows Management Page</div>' },
                props: true,
            },
            {
                path: 'assets',
                name: 'ProjectAssets',
                component: { template: '<div>Assets Management Page</div>' },
                props: true,
            },
            {
                path: 'label-schemes',
                name: 'ProjectLabels',
                component: () => import('@/views/project/ProjectLabelSchemesView.vue'),
                props: true,
            },
            {
                path: 'settings',
                name: 'ProjectSettings',
                component: { template: '<div>Project Settings Page</div>' },
                props: true,
            },
        ],
    }
];

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
});

export default router;
