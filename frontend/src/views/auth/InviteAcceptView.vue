<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Project Invitation</h1>
            
            <!-- Loading state while validating token -->
            <div v-if="isValidatingToken" class="info-banner">
                <span class="loading-text">Validating invitation...</span>
            </div>
            
            <!-- Valid invitation display for authenticated users -->
            <div v-else-if="invitationData && !isAccepted && isAuthenticated" class="invitation-details">
                <div class="success-banner">
                    <strong>You're invited!</strong> 
                    You've been invited to join the project <strong>{{ projectName }}</strong>.
                    <br><small>Role: {{ invitationData.role }}</small>
                </div>
                
                <div v-if="errorMessage" class="error-banner">
                    {{ errorMessage }}
                </div>
                
                <div class="form-actions">
                    <Button 
                        @click="handleAcceptInvitation" 
                        class="btn btn-primary" 
                        :disabled="isAccepting"
                    >
                        {{ isAccepting ? 'Accepting...' : 'Accept Invitation' }}
                    </Button>
                    <Button 
                        @click="handleDecline" 
                        class="btn btn-secondary"
                        :disabled="isAccepting"
                    >
                        Decline
                    </Button>
                </div>
            </div>
            
            <!-- Success state -->
            <div v-else-if="isAccepted" class="success-state">
                <div class="success-banner">
                    <strong>Invitation Accepted!</strong>
                    You have successfully joined the project.
                </div>
                <div class="form-actions">
                    <Button @click="navigateToProject" class="btn btn-primary">
                        Go to Project
                    </Button>
                    <Button @click="navigateToHome" class="btn btn-secondary">
                        Go to Home
                    </Button>
                </div>
            </div>
            
            <!-- Invalid invitation -->
            <div v-else class="error-state">
                <div class="error-banner">
                    <strong>Invalid Invitation</strong>
                    This invitation link is invalid, expired, or has already been used.
                </div>
                <div class="form-actions">
                    <Button @click="navigateToHome" class="btn btn-primary">
                        Go to Home
                    </Button>
                </div>
            </div>
            
            <!-- Authentication required message -->
            <div v-if="!isAuthenticated && invitationData" class="auth-required">
                <div class="warning-banner">
                    <strong>Login Required</strong>
                    You need to be logged in to accept this invitation for <strong>{{ projectName }}</strong>.
                </div>
                <div class="form-actions">
                    <Button @click="navigateToLogin" class="btn btn-primary">
                        Login to Accept
                    </Button>
                    <Button @click="navigateToRegister" class="btn btn-secondary">
                        Create Account
                    </Button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from "vue";
import {useRoute, useRouter} from "vue-router";
import {useAuthStore} from "@/stores/authStore";
import {useToast} from "@/composables/useToast";
import {projectInvitationService} from "@/services/api";
import {projectService} from "@/services/api/projects";
import Button from "@/components/common/Button.vue";
import type {ProjectInvitationDto} from "@/types/projectInvitation";
import type {Project} from "@/types/project/project";
import {AppLogger} from "@/utils/logger";

const logger = AppLogger.createComponentLogger('InviteAcceptView');

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { showSuccess, showError } = useToast();

// State management
const invitationData = ref<ProjectInvitationDto | null>(null);
const projectData = ref<Project | null>(null);
const isValidatingToken = ref(false);
const isAccepting = ref(false);
const isAccepted = ref(false);
const errorMessage = ref("");

// Computed properties
const isAuthenticated = computed(() => authStore.isAuthenticated);
const projectName = computed(() => {
    return projectData.value?.name || `Project ${invitationData.value?.projectId}`;
});

// Get invitation token from route params
const inviteToken = computed(() => route.params.token as string);

// Validate invitation token
const validateInvitationToken = async (token: string): Promise<void> => {
    if (!token) {
        errorMessage.value = "No invitation token provided";
        return;
    }
    
    isValidatingToken.value = true;
    errorMessage.value = "";
    
    try {
        const invitation = await projectInvitationService.validateInvitationToken(token);
        invitationData.value = invitation;
        
        // Fetch project details to get the project name
        await fetchProjectDetails(invitation.projectId);
    } catch (error) {
        logger.error("Failed to validate invitation token:", error);
        errorMessage.value = error instanceof Error ? error.message : "Failed to validate invitation";
    } finally {
        isValidatingToken.value = false;
    }
};

// Fetch project details
const fetchProjectDetails = async (projectId: number): Promise<void> => {
    try {
        const project = await projectService.getProject(projectId);
        projectData.value = project;
    } catch (error) {
        logger.error("Failed to fetch project details:", error);
        // Don't set error message as this is not critical for invitation flow
    }
};

// Accept invitation
const handleAcceptInvitation = async (): Promise<void> => {
    if (!inviteToken.value || !isAuthenticated.value) {
        return;
    }
    
    isAccepting.value = true;
    errorMessage.value = "";
    
    try {
        await projectInvitationService.acceptInvitation(inviteToken.value);
        isAccepted.value = true;
        showSuccess("Invitation Accepted", "You have successfully joined the project!");
    } catch (error) {
        logger.error("Failed to accept invitation:", error);
        errorMessage.value = error instanceof Error ? error.message : "Failed to accept invitation";
        showError("Invitation Error", errorMessage.value);
    } finally {
        isAccepting.value = false;
    }
};

// Navigation functions
const handleDecline = (): void => {
    router.push('/home');
};

const navigateToProject = (): void => {
    if (invitationData.value) {
        router.push(`/projects/${invitationData.value.projectId}`);
    } else {
        router.push('/home');
    }
};

const navigateToHome = (): void => {
    router.push('/home');
};

const navigateToLogin = (): void => {
    // Include the invitation token in the login URL
    router.push(`/login?inviteToken=${encodeURIComponent(inviteToken.value)}`);
};

const navigateToRegister = (): void => {
    // Redirect to register with invitation token
    router.push(`/register?inviteToken=${encodeURIComponent(inviteToken.value)}`);
};

// Initialize on component mount
onMounted(async () => {
    if (inviteToken.value) {
        await validateInvitationToken(inviteToken.value);
    }
});
</script>

<style lang="scss" scoped>
@import "@/styles/auth.css";

.invitation-details {
    text-align: center;
}

.invitation-info {
    margin: 1.5rem 0;
    padding: 1rem;
    background-color: var(--color-gray-100);
    border-radius: 8px;
    
    p {
        margin-bottom: 0.5rem;
        color: var(--color-gray-900);
        
        &:last-child {
            margin-bottom: 0;
        }
    }
}

.success-state, .error-state {
    text-align: center;
}

.auth-required {
    text-align: center;
    margin-top: 1.5rem;
}

// Additional styles for invitation banners
.info-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-info-light);
    border: 1px solid var(--color-info);
    border-radius: 8px;
    color: var(--color-info-dark);
    font-size: 0.875rem;
    text-align: center;
    
    .loading-text {
        display: flex;
        align-items: center;
        justify-content: center;
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
    text-align: center;
    
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

.error-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-error-light);
    border: 1px solid var(--color-error);
    border-radius: 8px;
    color: var(--color-error-dark);
    font-size: 0.875rem;
    text-align: center;
    
    strong {
        font-weight: 600;
    }
}

.form-actions {
    display: flex;
    gap: 1rem;
    justify-content: center;
    margin-top: 1.5rem;
    
    .btn {
        min-width: 120px;
    }
}
</style>
