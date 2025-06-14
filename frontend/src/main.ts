import { createApp } from 'vue'
import { createPinia } from 'pinia'

import './styles/main.scss'
import App from './App.vue'
import router from './router'

import { setupInterceptors } from '@/services/api/apiClient'
import { useAuthStore } from '@/stores/authStore'

const app = createApp(App);

const pinia = createPinia();
app.use(pinia);

const authStore = useAuthStore();

setupInterceptors(authStore);

authStore.initializeAuth();

app.use(router);
app.mount('#app');