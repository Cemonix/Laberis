import {createApp} from 'vue'
import {createPinia} from 'pinia'

import './styles/main.css'
import './styles/underline-animation.css'
import App from './App.vue'
import router from './router'

import {setupInterceptors} from '@/services/api/apiClient'
import {useAuthStore} from '@/stores/authStore'
import {logger, piniaLogger, AppLogger} from '@/utils/logger'
import {useErrorHandler} from '@/composables/useErrorHandler'
import {registerPermissionDirective} from '@/directives/vPermission';
import {clickOutside} from '@/directives/clickOutside';

const main_logger = AppLogger.createServiceLogger('Main');

async function initializeApp() {
    const { handleError } = useErrorHandler();

    const app = createApp(App);

    // === Global Error Handling Setup ===
    // Catches errors from Vue components (setup, render, lifecycle hooks)
    app.config.errorHandler = (err, instance, info) => {
        const context = `Vue component: ${instance?.$options.name || 'Unknown'}, hook: ${info}`;
        handleError(err, context);
    };

    // Catches unhandled promise rejections
    window.addEventListener('unhandledrejection', (event) => {
        handleError(event.reason, 'Unhandled Promise Rejection');
    });
    // ===================================

    const pinia = createPinia();
    pinia.use(piniaLogger);
    app.use(pinia);
    app.use(logger);
    
    // Register directives
    registerPermissionDirective(app);
    app.directive('click-outside', clickOutside);

    const authStore = useAuthStore();
    setupInterceptors(authStore);
    
    // Auth will be initialized lazily by the router when needed
    // This prevents unnecessary token refresh attempts on public pages
    main_logger.info('App initialized - auth will be initialized on demand');

    app.use(router);
    app.mount('#app');
}

initializeApp().catch(error => {
    // Fallback for errors during the async initializeApp itself
    const { handleError } = useErrorHandler();
    handleError(error, 'initializeApp top-level catch');
    document.body.innerHTML = `
        <div style="
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background-color: #f8f9fa;
            color: #343a40;
            text-align: center;
            padding: 2rem;
        ">
            <div style="
                background: white;
                border-radius: 8px;
                padding: 3rem;
                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                max-width: 500px;
            ">
                <h1 style="color: #dc3545; margin-bottom: 1rem; font-size: 2rem;">Application Failed to Start</h1>
                <p style="margin-bottom: 1.5rem; line-height: 1.6;">
                    We're sorry, but the application could not be initialized properly.
                </p>
                <p style="margin-bottom: 1.5rem; color: #6c757d;">
                    Please try refreshing the page or check the browser console for technical details.
                </p>
                <button onclick="window.location.reload()" style="
                    background-color: #007bff;
                    color: white;
                    border: none;
                    padding: 0.75rem 1.5rem;
                    border-radius: 4px;
                    cursor: pointer;
                    font-size: 1rem;
                ">
                    Refresh Page
                </button>
            </div>
        </div>
    `;
});