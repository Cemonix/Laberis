<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Welcome Back</h1>
            <Form @submit="handleLogin" class="auth-form">
                <!-- Invitation notice -->
                <div v-if="inviteToken" class="info-banner">
                    <strong>Project Invitation</strong>
                    You're logging in to accept a project invitation. After logging in, you'll be redirected to accept the invitation.
                </div>
                
                <div v-if="errorMessage" class="error-banner">
                    {{ errorMessage }}
                </div>

                <!-- Email verification error banner -->
                <div v-if="showEmailVerificationError" class="verification-error-banner">
                    <div class="error-icon">⚠</div>
                    <div class="error-content">
                        <h3>Email Verification Required</h3>
                        <p>Please verify your email address before logging in. Check your email for a verification link.</p>
                        <div class="verification-actions">
                            <Button 
                                @click="resendVerificationEmail" 
                                class="btn btn-outline btn-sm" 
                                :disabled="isResendingVerification"
                            >
                                {{ isResendingVerification ? 'Sending...' : 'Resend Verification Email' }}
                            </Button>
                        </div>
                    </div>
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
                    />
                </div>
                <div class="form-group">
                    <label for="password">Password</label>
                    <input 
                        type="password" 
                        id="password" 
                        v-model="password" 
                        placeholder="Enter your password" 
                        required 
                        :disabled="isLoading"
                    />
                </div>
                <div class="form-actions">
                    <Button type="submit" class="btn btn-primary" :disabled="isLoading">
                        {{ isLoading ? 'Signing In...' : 'Sign In' }}
                    </Button>
                </div>
            </Form>
            <div class="auth-footer">
                <p>Don't have an account? <router-link to="/register" class="auth-link">Create one</router-link></p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {onMounted, ref} from "vue";
import {useRoute, useRouter} from "vue-router";
import {useAuthStore} from "@/stores/authStore";
import {useToast} from "@/composables/useToast";
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";
import {AppLogger} from "@/core/logger/logger";

const logger = AppLogger.createComponentLogger('LoginView');

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { showError, showSuccess } = useToast();

const email = ref("");
const password = ref("");
const isLoading = ref(false);
const errorMessage = ref("");
const inviteToken = ref<string | null>(null);

// Email verification states
const showEmailVerificationError = ref(false);
const isResendingVerification = ref(false);

// Check for invitation token in URL
onMounted(() => {
    const token = route.query.inviteToken as string;
    if (token) {
        inviteToken.value = token;
    }
});

const handleLogin = async () => {
    if (!email.value || !password.value) {
        errorMessage.value = "Please fill in all fields";
        return;
    }

    isLoading.value = true;
    errorMessage.value = "";

    try {
        await authStore.login({
            email: email.value,
            password: password.value
        });
        
        // Check if there's an invitation token to process
        if (inviteToken.value) {
            // Redirect to invitation acceptance page
            router.push(`/invite/accept/${inviteToken.value}`);
        } else {
            // Check if there's a redirect URL (for other cases)
            const urlParams = new URLSearchParams(window.location.search);
            const redirectUrl = urlParams.get('redirect');
            
            if (redirectUrl) {
                router.push(decodeURIComponent(redirectUrl));
            } else {
                // Use smart redirect based on last project
                const smartRedirectUrl = authStore.getPostLoginRedirectUrl();
                router.push(smartRedirectUrl);
            }
        }
    } catch (error) {
        logger.error("Login failed:", error);
        const errorMsg = error instanceof Error ? error.message : "Login failed. Please try again.";
        
        // Check if the error is about email verification
        if (errorMsg.toLowerCase().includes('verify your email') || 
            errorMsg.toLowerCase().includes('email verification') ||
            errorMsg.toLowerCase().includes('email not confirmed')) {
            showEmailVerificationError.value = true;
            errorMessage.value = ""; // Clear the regular error message
        } else {
            showEmailVerificationError.value = false;
            errorMessage.value = errorMsg;
        }
    } finally {
        isLoading.value = false;
    }
};

// Resend verification email function
const resendVerificationEmail = async () => {
    if (!email.value) {
        showError("Email Required", "Please enter your email address first.");
        return;
    }
    
    isResendingVerification.value = true;
    
    try {
        // Call the unauthenticated resend endpoint
        await authStore.resendEmailVerification(email.value);
        
        // Show success message and hide error banner
        showEmailVerificationError.value = false;
        showSuccess("Email Sent", "Verification email has been resent. Please check your email and spam folder.");
    } catch (error) {
        logger.error("Failed to resend verification email:", error);
        const errorMsg = error instanceof Error ? error.message : "Failed to resend verification email";
        showError("Resend Failed", errorMsg);
    } finally {
        isResendingVerification.value = false;
    }
};
</script>

<style scoped>
@import "@/styles/auth.css";

.info-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-info-light);
    border: 1px solid var(--color-info);
    border-radius: 8px;
    color: var(--color-info-dark);
    font-size: 0.875rem;
    text-align: center;
    
    strong {
        font-weight: 600;
    }
}

.verification-error-banner {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-warning-light);
    border: 1px solid var(--color-warning);
    border-radius: 8px;
    color: var(--color-warning-dark);
    
    .error-icon {
        font-size: 1.5rem;
        color: var(--color-warning);
        flex-shrink: 0;
        margin-top: 0.125rem;
    }
    
    .error-content {
        flex: 1;
        
        h3 {
            margin: 0 0 0.5rem 0;
            font-size: 1rem;
            font-weight: 600;
            color: var(--color-warning-dark);
        }
        
        p {
            margin: 0 0 1rem 0;
            line-height: 1.4;
            font-size: 0.875rem;
        }
        
        .verification-actions {
            margin-top: 0.75rem;
        }
    }
}
</style>
