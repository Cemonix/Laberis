import { createApp } from 'vue'
import { createPinia } from 'pinia'

import './styles/main.scss'
import App from './App.vue'
import router from './router'

import { setupInterceptors } from '@/services/api/apiClient'
import { useAuthStore } from '@/stores/authStore'
import { logger, piniaLogger } from '@/utils/logger'

async function initializeApp() {
    const app = createApp(App);

    const pinia = createPinia();
    pinia.use(piniaLogger);
    app.use(pinia);
    app.use(logger);

    const authStore = useAuthStore();

    setupInterceptors(authStore);

    // Initialize auth store before mounting the app with timeout
    try {
        await Promise.race([
            authStore.initializeAuth(),
            new Promise((_, reject) => 
                setTimeout(() => reject(new Error('Auth initialization timeout')), 5000)
            )
        ]);
    } catch (error) {
        console.warn('Auth initialization failed or timed out:', error);
        // Continue with app mounting even if auth init fails
    }

    app.use(router);
    app.mount('#app');
}

initializeApp().catch(console.error);