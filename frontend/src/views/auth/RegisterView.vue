<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>{{ isInviteRegistration ? 'Complete Your Invitation' : 'Create Account' }}</h1>
            <Form @submit="handleRegister" class="auth-form">
                <div v-if="errorMessage" class="error-banner">
                    {{ errorMessage }}
                </div>
                
                <!-- Invitation Status Banner -->
                <div v-if="isValidatingToken" class="info-banner">
                    <span class="loading-text">Validating invitation...</span>
                </div>
                <div v-else-if="isInviteRegistration && invitationData" class="success-banner">
                    <strong>You're invited!</strong> 
                    You've been invited to join a project. Please complete your registration below.
                    <br><small>Email: {{ invitationData.email }}</small>
                </div>
                <div v-else-if="isInviteRegistration" class="warning-banner">
                    <strong>Invitation expired or invalid.</strong>
                    You can still create an account without the invitation.
                </div>
                <div class="form-group">
                    <label for="userName">Username</label>
                    <input 
                        type="text" 
                        id="userName" 
                        v-model="userName" 
                        placeholder="Enter your username" 
                        required 
                        :disabled="isLoading"
                    />
                </div>
                <div class="form-group">
                    <label for="email">Email</label>
                    <input 
                        type="email" 
                        id="email" 
                        v-model="email" 
                        placeholder="Enter your email" 
                        required 
                        :disabled="isLoading"
                        :readonly="!!(isInviteRegistration && invitationData)"
                        :class="{ 'readonly': !!(isInviteRegistration && invitationData) }"
                    />
                    <div v-if="isInviteRegistration && invitationData" class="field-help">
                        Email is set from your invitation and cannot be changed.
                    </div>
                </div>
                <div class="form-group">
                    <label for="password">Password</label>
                    <input 
                        type="password" 
                        id="password" 
                        v-model="password" 
                        placeholder="Enter your password (min. 6 characters)" 
                        required 
                        minlength="6"
                        :disabled="isLoading"
                    />
                </div>
                <div class="form-group">
                    <label for="confirmPassword">Confirm Password</label>
                    <input 
                        type="password" 
                        id="confirmPassword" 
                        v-model="confirmPassword" 
                        placeholder="Confirm your password" 
                        required 
                        :class="{ 'error': !passwordsMatch }"
                        :disabled="isLoading"
                    />
                    <span v-if="!passwordsMatch && confirmPassword" class="error-message">
                        Passwords do not match
                    </span>
                </div>
                <div class="form-actions">
                    <Button type="submit" class="btn btn-primary" :disabled="isLoading || !isFormValid">
                        {{ isLoading ? 'Creating Account...' : 'Create Account' }}
                    </Button>
                </div>
            </Form>
            <div class="auth-footer">
                <p>Already have an account? <router-link to="/login" class="auth-link">Sign in</router-link></p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from "vue";
import {useRouter} from "vue-router";
import {useAuthStore} from "@/stores/authStore";
import {useToast} from "@/composables/useToast";
import {projectInvitationService} from "@/services/api";
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";
import type {ProjectInvitationDto} from "@/types/projectInvitation";
import {AppLogger} from "@/utils/logger";

const logger = AppLogger.createComponentLogger('RegisterView');

const router = useRouter();
const authStore = useAuthStore();
const { showError } = useToast();

const userName = ref("");
const email = ref("");
const password = ref("");
const confirmPassword = ref("");
const isLoading = ref(false);
const errorMessage = ref("");

// Invitation token handling
const inviteToken = ref<string | null>(null);
const invitationData = ref<ProjectInvitationDto | null>(null);
const isValidatingToken = ref(false);

// Form validation
const isFormValid = computed(() => {
    return userName.value.trim() && 
            email.value.trim() && 
            password.value.length >= 6 && 
            password.value === confirmPassword.value;
});

const passwordsMatch = computed(() => {
    return password.value === confirmPassword.value || confirmPassword.value === "";
});

// Check if user is registering via invitation
const isInviteRegistration = computed(() => {
    return inviteToken.value !== null;
});

// Validate invitation token
const validateInvitationToken = async (token: string): Promise<void> => {
    if (!token) return;
    
    isValidatingToken.value = true;
    
    try {
        const invitation = await projectInvitationService.validateInvitationToken(token);
        invitationData.value = invitation;
        
        // Pre-fill email if invitation exists
        if (invitation.email && !email.value) {
            email.value = invitation.email;
        }
    } catch (error) {
        showError("Invalid Invitation", "This invitation link is invalid or has expired. You can still create an account without the invitation.");
    } finally {
        isValidatingToken.value = false;
    }
};

// Initialize invitation token from URL
const initializeInviteToken = (): void => {
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get('inviteToken');
    
    if (token) {
        inviteToken.value = token;
        validateInvitationToken(token);
    }
};

const handleRegister = async () => {
    if (!passwordsMatch.value) {
        errorMessage.value = "Passwords do not match";
        return;
    }
    
    if (!isFormValid.value) {
        errorMessage.value = "Please fill in all fields correctly";
        return;
    }

    // Additional validation for invitation registration
    if (isInviteRegistration.value && invitationData.value) {
        // Check if email matches invitation
        if (email.value.trim().toLowerCase() !== invitationData.value.email.toLowerCase()) {
            errorMessage.value = `Email must match the invitation email: ${invitationData.value.email}`;
            return;
        }
    }

    isLoading.value = true;
    errorMessage.value = "";

    try {
        await authStore.register({
            email: email.value.trim(),
            userName: userName.value.trim(),
            password: password.value,
            confirmPassword: confirmPassword.value,
            inviteToken: inviteToken.value || undefined
        });
        
        // Use smart redirect based on last project after successful registration
        const smartRedirectUrl = authStore.getPostLoginRedirectUrl();
        router.push(smartRedirectUrl);
    } catch (error) {
        logger.error("Registration failed:", error);
        errorMessage.value = error instanceof Error ? error.message : "Registration failed. Please try again.";
    } finally {
        isLoading.value = false;
    }
};

// Initialize on component mount
onMounted(() => {
    initializeInviteToken();
});
</script>

<style scoped>
@import "@/styles/auth.css";

/* Additional styles for invitation banners */
.info-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-info-light);
    border: 1px solid var(--color-info);
    border-radius: 8px;
    color: var(--color-info-dark);
    font-size: 0.875rem;
    
    .loading-text {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        
        &::before {
            content: '⏳';
        }
    }
}

.success-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-success-light);
    border: 1px solid var(--color-success);
    border-radius: 8px;
    color: var(--color-success-dark);
    font-size: 0.875rem;
    
    strong {
        font-weight: 600;
    }
    
    small {
        font-size: 0.75rem;
        opacity: 0.8;
    }
}

.warning-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-warning-light);
    border: 1px solid var(--color-warning);
    border-radius: 8px;
    color: var(--color-warning-dark);
    font-size: 0.875rem;
    text-align: center;
    
    strong {
        font-weight: 600;
    }
}

.readonly {
    background-color: var(--color-gray-600) !important;
    cursor: not-allowed;
}

.field-help {
    margin-top: 0.25rem;
    font-size: 0.75rem;
    color: var(--color-gray-800);
    font-style: italic;
}
</style>
