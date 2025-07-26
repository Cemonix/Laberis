<template>
    <div class="change-password-view">
        <Card>
            <template #header>
                <h2>Change Password</h2>
                <p>Update your account password for better security</p>
            </template>
            
            <Form @submit="handlePasswordChange" class="password-form">
                <div v-if="errorMessage" class="error-banner">
                    {{ errorMessage }}
                </div>
                
                <div v-if="successMessage" class="success-banner">
                    {{ successMessage }}
                </div>
                
                <div class="form-group">
                    <label for="currentPassword">Current Password</label>
                    <input 
                        type="password" 
                        id="currentPassword" 
                        v-model="currentPassword" 
                        placeholder="Enter your current password" 
                        required 
                        :disabled="isLoading"
                        autocomplete="current-password"
                    />
                </div>
                
                <div class="form-group">
                    <label for="newPassword">New Password</label>
                    <input 
                        type="password" 
                        id="newPassword" 
                        v-model="newPassword" 
                        placeholder="Enter your new password (min. 6 characters)" 
                        required 
                        minlength="6"
                        :disabled="isLoading"
                        autocomplete="new-password"
                        :class="{ 'error': (!isNewPasswordValid && newPassword.length > 0) || isSameAsCurrentPassword }"
                    />
                    <div v-if="!isNewPasswordValid && newPassword.length > 0" class="field-error">
                        Password must be at least 6 characters long
                    </div>
                    <div v-if="isSameAsCurrentPassword" class="field-error">
                        New password must be different from your current password
                    </div>
                </div>
                
                <div class="form-group">
                    <label for="confirmNewPassword">Confirm New Password</label>
                    <input 
                        type="password" 
                        id="confirmNewPassword" 
                        v-model="confirmNewPassword" 
                        placeholder="Confirm your new password" 
                        required 
                        :disabled="isLoading"
                        autocomplete="new-password"
                        :class="{ 'error': !passwordsMatch && confirmNewPassword.length > 0 }"
                    />
                    <div v-if="!passwordsMatch && confirmNewPassword.length > 0" class="field-error">
                        Passwords do not match
                    </div>
                </div>
                
                <div class="password-requirements">
                    <h4>Password Requirements:</h4>
                    <ul>
                        <li :class="{ 'valid': newPassword.length >= 6 }">
                            At least 6 characters long
                        </li>
                        <li :class="{ 'valid': !isSameAsCurrentPassword && newPassword.length > 0 }">
                            Must be different from current password
                        </li>
                        <li :class="{ 'valid': passwordsMatch && confirmNewPassword.length > 0 }">
                            Passwords must match
                        </li>
                    </ul>
                </div>
                
                <div class="form-actions">
                    <Button 
                        type="submit" 
                        class="btn btn-primary" 
                        :disabled="isLoading || !isFormValid"
                    >
                        {{ isLoading ? 'Updating Password...' : 'Update Password' }}
                    </Button>
                </div>
            </Form>
        </Card>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/composables/useToast';
import Card from '@/components/common/Card.vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import { authService } from '@/services/auth/authService';
import type { ChangePasswordDto } from '@/types/auth/auth';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createComponentLogger('ChangePasswordView');

const authStore = useAuthStore();
const { showSuccess, showError } = useToast();

const currentPassword = ref('');
const newPassword = ref('');
const confirmNewPassword = ref('');
const isLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const isNewPasswordValid = computed(() => {
    return newPassword.value.length >= 6;
});

const passwordsMatch = computed(() => {
    return newPassword.value === confirmNewPassword.value || confirmNewPassword.value === '';
});

const isSameAsCurrentPassword = computed(() => {
    return currentPassword.value.length > 0 && 
           newPassword.value.length > 0 && 
           newPassword.value === currentPassword.value;
});

const isFormValid = computed(() => {
    return currentPassword.value.length > 0 && 
           isNewPasswordValid.value && 
           newPassword.value === confirmNewPassword.value &&
           !isSameAsCurrentPassword.value;
});

const resetForm = () => {
    currentPassword.value = '';
    newPassword.value = '';
    confirmNewPassword.value = '';
    errorMessage.value = '';
    successMessage.value = '';
};

const handlePasswordChange = async () => {
    if (!isFormValid.value) {
        errorMessage.value = 'Please check all fields and ensure passwords match';
        return;
    }

    isLoading.value = true;
    errorMessage.value = '';
    successMessage.value = '';

    try {
        const changePasswordData: ChangePasswordDto = {
            currentPassword: currentPassword.value,
            newPassword: newPassword.value,
            confirmNewPassword: confirmNewPassword.value
        };
        
        logger.info('Password change attempt for user:', authStore.user?.email);
        
        const response = await authService.changePassword(changePasswordData);
        
        successMessage.value = response.message || 'Password updated successfully!';
        showSuccess('Password Changed', 'Your password has been updated successfully.');
        
        // Reset form after successful change
        setTimeout(() => {
            resetForm();
        }, 2000);
        
    } catch (error) {
        logger.error('Password change failed:', error);
        const errorMsg = error instanceof Error ? error.message : 'Failed to change password. Please try again.';
        errorMessage.value = errorMsg;
        showError('Password Change Failed', errorMsg);
    } finally {
        isLoading.value = false;
    }
};
</script>

<style scoped>
.change-password-view {
    padding: 2rem;
}

.password-form {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.form-group label {
    font-weight: 600;
    color: var(--color-text-primary);
    font-size: 0.875rem;
}

.form-group input {
    padding: 0.75rem;
    border: 1px solid var(--color-border-light);
    border-radius: 8px;
    font-size: 1rem;
    transition: border-color 0.2s ease;
}

.form-group input:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(var(--color-primary), 0.1);
}

.form-group input.error {
    border-color: var(--color-error);
    background-color: var(--color-error-light);
}

.form-group input:disabled {
    background-color: var(--color-gray-200);
    cursor: not-allowed;
}

.field-error {
    color: var(--color-error);
    font-size: 0.875rem;
    margin-top: 0.25rem;
}

.error-banner {
    padding: 0.75rem 1rem;
    background-color: var(--color-error-light);
    border: 1px solid var(--color-error);
    border-radius: 8px;
    color: var(--color-error-dark);
    font-size: 0.875rem;
}

.success-banner {
    padding: 0.75rem 1rem;
    background-color: var(--color-success-light);
    border: 1px solid var(--color-success);
    border-radius: 8px;
    color: var(--color-success-dark);
    font-size: 0.875rem;
}

.password-requirements {
    background-color: var(--color-gray-100);
    padding: 1rem;
    border-radius: 8px;
    border: 1px solid var(--color-border-light);
}

.password-requirements h4 {
    margin-bottom: 0.5rem;
    color: var(--color-text-primary);
    font-size: 0.875rem;
}

.password-requirements ul {
    list-style: none;
    padding: 0;
    margin: 0;
}

.password-requirements li {
    padding: 0.25rem 0;
    font-size: 0.875rem;
    color: var(--color-text-secondary);
    position: relative;
    padding-left: 1.5rem;
}

.password-requirements li::before {
    content: '✗';
    position: absolute;
    left: 0;
    color: var(--color-error);
    font-weight: bold;
}

.password-requirements li.valid::before {
    content: '✓';
    color: var(--color-success);
}

.password-requirements li.valid {
    color: var(--color-success-dark);
}

.form-actions {
    display: flex;
    justify-content: flex-start;
    margin-top: 1rem;
}

.btn {
    padding: 0.75rem 1.5rem;
    border-radius: 8px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s ease;
    border: none;
    font-size: 1rem;
}

.btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.btn-primary {
    background-color: var(--color-primary);
    color: var(--color-white);
}

.btn-primary:hover:not(:disabled) {
    background-color: var(--color-primary-hover);
}

.btn-secondary {
    background-color: var(--color-gray-300);
    color: var(--color-text-primary);
}

.btn-secondary:hover:not(:disabled) {
    background-color: var(--color-gray-400);
}
</style>