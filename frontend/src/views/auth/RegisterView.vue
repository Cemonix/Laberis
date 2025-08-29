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

            <!-- Success Message for Email Verification -->
            <div v-if="showVerificationMessage" class="verification-success">
                <div class="success-icon">✓</div>
                <h2>Account Created Successfully!</h2>
                <p>We've sent a verification email to <strong>{{ registeredEmail }}</strong></p>
                <p>Please check your email and click the verification link to complete your account setup.</p>
                
                <div class="verification-actions">
                    <Button 
                        @click="goToLogin" 
                        class="btn btn-primary"
                    >
                        Go to Login
                    </Button>
                    <Button 
                        @click="resendVerificationEmail" 
                        class="btn btn-outline" 
                        :disabled="isResending"
                        v-if="canResendVerification"
                    >
                        {{ isResending ? 'Sending...' : 'Resend Email' }}
                    </Button>
                </div>
                
                <div class="verification-help">
                    <p><small>Didn't receive the email? Check your spam folder or try resending.</small></p>
                </div>
            </div>

            <div v-else class="auth-footer">
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
import {invitationService} from "@/services/invitation";
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";
import type {ProjectInvitationDto} from "@/services/invitation/invitation.types";
import {AppLogger} from "@/core/logger/logger";

const logger = AppLogger.createComponentLogger('RegisterView');

const router = useRouter();
const authStore = useAuthStore();
const { showError, showSuccess } = useToast();

const userName = ref("");
const email = ref("");
const password = ref("");
const confirmPassword = ref("");
const isLoading = ref(false);
const errorMessage = ref("");

// Email verification states
const showVerificationMessage = ref(false);
const registeredEmail = ref("");
const isResending = ref(false);
const canResendVerification = ref(true);

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
        const invitation = await invitationService.validateInvitationToken(token);
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
        
        // Show verification message instead of redirecting
        registeredEmail.value = email.value.trim();
        showVerificationMessage.value = true;
        logger.info("Registration successful - showing email verification message");
    } catch (error) {
        logger.error("Registration failed:", error);
        errorMessage.value = error instanceof Error ? error.message : "Registration failed. Please try again.";
    } finally {
        isLoading.value = false;
    }
};

// Navigate to login page
const goToLogin = () => {
    router.push('/login');
};

// Resend verification email function
const resendVerificationEmail = async () => {
    if (!canResendVerification.value) return;
    
    isResending.value = true;
    
    try {
        // Call the unauthenticated resend endpoint with email
        await authStore.resendEmailVerification(registeredEmail.value);
        
        // Show success message
        showSuccess("Email Sent", "Verification email has been resent. Please check your email.");
        
        // Disable resend button temporarily to prevent spam
        canResendVerification.value = false;
        setTimeout(() => {
            canResendVerification.value = true;
        }, 30000); // 30 seconds
        
    } catch (error) {
        logger.error("Failed to resend verification email:", error);
        showError("Resend Failed", "Failed to resend verification email. Please try again or contact support.");
    } finally {
        isResending.value = false;
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

.verification-success {
    text-align: center;
    padding: 2rem 1rem;
    
    .success-icon {
        font-size: 3rem;
        color: var(--color-success);
        margin-bottom: 1rem;
    }
    
    h2 {
        color: var(--color-success);
        margin-bottom: 1rem;
    }
    
    p {
        margin-bottom: 1rem;
        line-height: 1.5;
    }
    
    .verification-actions {
        display: flex;
        gap: 1rem;
        justify-content: center;
        margin: 2rem 0;
        flex-wrap: wrap;
    }
    
    .verification-help {
        margin-top: 2rem;
        
        p {
            color: var(--color-gray-700);
            margin: 0;
        }
    }
}
</style>
