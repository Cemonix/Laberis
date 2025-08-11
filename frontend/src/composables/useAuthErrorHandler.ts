import { useAuthStore } from '@/stores/authStore';
import { useRouter } from 'vue-router';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('AuthErrorHandler');

export function useAuthErrorHandler() {
    const handleAuthError = (error: any, context?: string) => {
        const router = useRouter();
        const authStore = useAuthStore();

        logger.error(`Authentication error in ${context || 'unknown context'}:`, error);
        
        // Check if it's a 401 error
        if (error?.response?.status === 401) {
            // Clear authentication state
            authStore.user = null;
            authStore.tokens = null;
            
            // Redirect to login if not already there
            if (router.currentRoute.value.name !== 'Login') {
                router.push('/login');
            }
            
            return true;
        }
        
        return false;
    };

    return {
        handleAuthError
    };
}