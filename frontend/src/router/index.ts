import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';


const routes: Array<RouteRecordRaw> = [
    { path: '/', redirect: '/home' },
    // {
    //   path: '/home',
    //   name: 'Home',
    //   component: () => import('../views/HomePage.vue')
    // },
    {
        path: '/labelling',
        name: 'LabellingPage',
        component: () => import('../views/LabellingPage.vue'),
    },
];

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
});

export default router;
