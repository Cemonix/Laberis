<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <!-- Loading State -->
            <div v-if="isVerifying" class="verification-loading">
                <div class="loading-icon">🔄</div>
                <h1>Verifying Your Email</h1>
                <p>Please wait while we verify your email address...</p>
            </div>

            <!-- Success State -->
            <div v-else-if="verificationSuccess" class="verification-success">
                <div class="success-icon">✓</div>
                <h1>Email Verified Successfully!</h1>
                <p>Your email has been verified. You can now log in to your account.</p>
                
                <div class="verification-actions">
                    <router-link to="/login" class="btn btn-primary">
                        Go to Login
                    </router-link>
                </div>
            </div>

            <!-- Error State -->
            <div v-else class="verification-error">
                <div class="error-icon">✗</div>
                <h1>Email Verification Failed</h1>
                <p v-if="errorMessage">{{ errorMessage }}</p>
                <p v-else>The verification link is invalid or has expired.</p>
                
                <div class="verification-actions">
                    <router-link to="/login" class="btn btn-primary">
                        Back to Login
                    </router-link>
                    <router-link to="/register" class="btn btn-outline">
                        Create New Account
                    </router-link>
                </div>

                <div class="help-text">
                    <p><small>Need help? Contact support or try creating a new account.</small></p>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import { AppLogger } from "@/utils/logger";

const logger = AppLogger.createComponentLogger('EmailVerificationView');

const route = useRoute();
const authStore = useAuthStore();

const isVerifying = ref(true);
const verificationSuccess = ref(false);
const errorMessage = ref("");

// Extract token from URL and verify email
onMounted(async () => {
    const token = route.query.token as string;
    
    if (!token) {
        logger.error("No verification token found in URL");
        errorMessage.value = "No verification token provided.";
        isVerifying.value = false;
        return;
    }

    try {
        logger.info("Attempting email verification with token");
        const response = await authStore.verifyEmail(token);
        
        verificationSuccess.value = true;
        logger.info("Email verification successful:", response.message);
    } catch (error) {
        logger.error("Email verification failed:", error);
        verificationSuccess.value = false;
        errorMessage.value = error instanceof Error ? error.message : "Email verification failed.";
    } finally {
        isVerifying.value = false;
    }
});
</script>

<style scoped>
@import "@/styles/auth.css";

.verification-loading {
    text-align: center;
    padding: 2rem 1rem;
    
    .loading-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
        animation: spin 1s linear infinite;
    }
    
    h1 {
        color: var(--color-primary);
        margin-bottom: 1rem;
    }
    
    p {
        color: var(--color-gray-700);
        line-height: 1.5;
    }
}

.verification-success {
    text-align: center;
    padding: 2rem 1rem;
    
    .success-icon {
        font-size: 4rem;
        color: var(--color-success);
        margin-bottom: 1rem;
    }
    
    h1 {
        color: var(--color-success);
        margin-bottom: 1rem;
    }
    
    p {
        margin-bottom: 2rem;
        line-height: 1.5;
    }
    
    .verification-actions {
        display: flex;
        justify-content: center;
        gap: 1rem;
    }
}

.verification-error {
    text-align: center;
    padding: 2rem 1rem;
    
    .error-icon {
        font-size: 4rem;
        color: var(--color-error);
        margin-bottom: 1rem;
    }
    
    h1 {
        color: var(--color-error);
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
    
    .help-text {
        margin-top: 2rem;
        
        p {
            color: var(--color-gray-700);
            margin: 0;
        }
    }
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}
</style>