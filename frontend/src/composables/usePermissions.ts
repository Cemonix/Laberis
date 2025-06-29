import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { RoleEnum } from '@/types/auth/role';

export function usePermissions() {
    const authStore = useAuthStore();

    const hasRole = (role: RoleEnum): boolean => {
        return authStore.hasRole(role);
    };

    const hasAnyRole = (roles: RoleEnum[]): boolean => {
        return roles.some(role => authStore.hasRole(role));
    };

    const hasAllRoles = (roles: RoleEnum[]): boolean => {
        return roles.every(role => authStore.hasRole(role));
    };

    const isAdmin = computed(() => hasRole(RoleEnum.ADMIN));
    const isManager = computed(() => hasRole(RoleEnum.MANAGER));
    const isUser = computed(() => hasRole(RoleEnum.USER));

    return {
        hasRole,
        hasAnyRole,
        hasAllRoles,
        isAdmin,
        isManager,
        isUser
    };
}