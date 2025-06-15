import { createApp } from 'vue'
import { createPinia } from 'pinia'

import './styles/main.scss'
import App from './App.vue'
import router from './router'

import { setupInterceptors } from '@/services/api/apiClient'
import { useAuthStore } from '@/stores/authStore'
import { logger, piniaLogger } from '@/utils/logger'

const app = createApp(App);

const pinia = createPinia();
pinia.use(piniaLogger);
app.use(pinia);
app.use(logger);

const authStore = useAuthStore();

setupInterceptors(authStore);

authStore.initializeAuth();

app.use(router);
app.mount('#app');