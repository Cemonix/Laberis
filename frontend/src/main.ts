import {createApp} from 'vue'
import {createPinia} from 'pinia'

import './styles/main.css'
import App from './App.vue'
import router from './router'

import {setupInterceptors} from '@/services/api/apiClient'
import {useAuthStore} from '@/stores/authStore'
import {logger, piniaLogger} from '@/utils/logger'
import {useErrorHandler} from '@/composables/useErrorHandler';

async function initializeApp() {
    // Instantiate error handler here to attach it globally
    const { handleError } = useErrorHandler();

    const app = createApp(App);

    // === Global Error Handling Setup ===
    // Catches errors from Vue components (setup, render, lifecycle hooks)
    app.config.errorHandler = (err, instance, info) => {
        const context = `Vue component: ${instance?.$options.name || 'Unknown'}, hook: ${info}`;
        handleError(err, context);
    };

    // Catches unhandled promise rejections (very important for async/await)
    window.addEventListener('unhandledrejection', (event) => {
        handleError(event.reason, 'Unhandled Promise Rejection');
    });
    // ===================================

    const pinia = createPinia();
    pinia.use(piniaLogger);
    app.use(pinia);
    app.use(logger);

    const authStore = useAuthStore();
    setupInterceptors(authStore);

    try {
        await Promise.race([
            authStore.initializeAuth(),
            new Promise((_, reject) => 
                setTimeout(() => reject(new Error('Auth initialization timeout')), 5000)
            )
        ]);
    } catch (error) {
        handleError(error, 'Application Initialization');
    }

    app.use(router);
    app.mount('#app');
}

initializeApp().catch(error => {
    // Fallback for errors during the async initializeApp itself
    const { handleError } = useErrorHandler();
    handleError(error, 'initializeApp top-level catch');
    // TODO: Add a more visible failure message here if the app can't mount
    document.body.innerHTML = '<h1>Application failed to start. Please check the console for errors.</h1>';
});