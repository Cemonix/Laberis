import { defineStore } from "pinia";
import { projectService, projectMemberService } from "@/services/project";
import type { Project } from "@/services/project/project.types";
import type { ProjectMember } from "@/services/project/projectMember.types";
import type { Label } from "@/services/project/labelScheme/label.types";
import type { ProjectRole } from "@/services/project/project.types";
import { useErrorHandler } from "@/composables/useErrorHandler";
import { AppLogger } from "@/core/logger/logger";
import { LastProjectManager, LastStageManager } from "@/core/persistence";
import { useAuthStore } from "@/stores/authStore";
import type { ProjectState } from "./projectStore.types";
import type { LastStageData } from "@/core/persistence/lastProject.types";

const logger = AppLogger.createServiceLogger("ProjectStore");

export const useProjectStore = defineStore("project", {
    state: (): ProjectState => ({
        currentProject: null,
        teamMembers: [],
        projectLabels: [],
        loading: false,
        membersLoading: false,
        labelsLoading: false,
        currentStageType: '',
    }),

    getters: {
        currentProjectId(state): number | null {
            return state.currentProject?.id || null;
        },
        isProjectLoaded(state): boolean {
            return !!state.currentProject;
        },
        activeMembers(state): ProjectMember[] {
            // All active members (joined or invited)
            return state.teamMembers || [];
        },
        joinedMembers(state): ProjectMember[] {
            // Members who have actually joined (accepted their invitation)
            return (state.teamMembers || []).filter((member) => member.joinedAt !== undefined);
        },
        pendingMembers(state): ProjectMember[] {
            // Members who are invited but haven't joined yet
            return (state.teamMembers || []).filter((member) => member.joinedAt === undefined);
        },
        teamMemberCount(state): number {
            return state.teamMembers.length;
        },
        labelCount(state): number {
            return state.projectLabels.length;
        }
    },

    actions: {
        async setCurrentProject(projectId: number): Promise<void> {
            const { handleError } = useErrorHandler();

            if (this.currentProject?.id === projectId) {
                logger.info(`Project ${projectId} already loaded`);
                return;
            }

            this.loading = true;
            try {
                const project = await projectService.getProject(projectId);
                this.currentProject = project;
                logger.info(`Loaded project: ${project.name} (ID: ${projectId})`);

                // Save this project as the user's last accessed project
                const authStore = useAuthStore();
                if (authStore.user?.email) {
                    LastProjectManager.saveLastProject(projectId, project.name, authStore.user.email);
                }

                // Load related data in parallel
                await Promise.all([
                    this.loadTeamMembers(projectId),
                    this.loadProjectLabels(projectId),
                ]);
            } catch (error) {
                logger.error(`Failed to load project ${projectId}`, error);
                handleError(error, "Failed to load project");
                throw error;
            } finally {
                this.loading = false;
            }
        },

        async loadTeamMembers(projectId: number): Promise<void> {
            const { handleError } = useErrorHandler();

            this.membersLoading = true;
            try {
                const members = await projectMemberService.getProjectMembers(projectId);
                this.teamMembers = members;
                logger.info(`Loaded ${members.length} team members for project ${projectId}`);
            } catch (error) {
                logger.error(`Failed to load team members for project ${projectId}`, error);
                handleError(error, "Failed to load team members");
            } finally {
                this.membersLoading = false;
            }
        },

        async loadProjectLabels(projectId: number): Promise<void> {
            const { handleError } = useErrorHandler();

            this.labelsLoading = true;
            try {
                // TODO: This would need a proper API endpoint for project labels
                // For now, we'll skip this functionality until the API is available
                this.projectLabels = [];
                logger.info(`Labels functionality not yet implemented for project ${projectId}`);
            } catch (error) {
                logger.error(`Failed to load labels for project ${projectId}`, error);
                handleError(error, "Failed to load project labels");
            } finally {
                this.labelsLoading = false;
            }
        },

        addTeamMember(member: ProjectMember): void {
            this.teamMembers.push(member);
            logger.info(`Added team member: ${member.userName || member.email}`);
        },

        removeTeamMember(memberEmail: string): void {
            const index = this.teamMembers.findIndex((member) => member.email === memberEmail);
            if (index !== -1) {
                const removedMember = this.teamMembers.splice(index, 1)[0];
                logger.info(`Removed team member: ${removedMember.userName || removedMember.email}`);
            }
        },

        updateTeamMemberRole(memberEmail: string, newRole: ProjectRole): void {
            const member = this.teamMembers.find((m) => m.email === memberEmail);
            if (member) {
                member.role = newRole;
                logger.info(`Updated role for ${member.userName || member.email} to ${newRole}`);
            }
        },

        addLabel(label: Label): void {
            this.projectLabels.push(label);
            logger.info(`Added label: ${label.name}`);
        },

        updateLabel(updatedLabel: Label): void {
            const index = this.projectLabels.findIndex((label) => label.labelId === updatedLabel.labelId);
            if (index !== -1) {
                this.projectLabels[index] = updatedLabel;
                logger.info(`Updated label: ${updatedLabel.name}`);
            }
        },

        removeLabel(labelId: number): void {
            const index = this.projectLabels.findIndex((label) => label.labelId === labelId);
            if (index !== -1) {
                const removedLabel = this.projectLabels.splice(index, 1)[0];
                logger.info(`Removed label: ${removedLabel.name}`);
            }
        },

        updateProject(updatedProject: Project): void {
            this.currentProject = updatedProject;
            logger.info(`Updated project: ${updatedProject.name}`);
        },

        clearProject(): void {
            this.currentProject = null;
            this.teamMembers = [];
            this.projectLabels = [];
            this.currentStageType = '';
            logger.info("Cleared project data");
        },

        clearLastProject(): void {
            const authStore = useAuthStore();
            if (authStore.user?.email) {
                LastProjectManager.clearLastProject(authStore.user.email);
            }
        },

        getLastProject() {
            const authStore = useAuthStore();
            return authStore.user?.email ? LastProjectManager.getLastProject(authStore.user.email) : null;
        },

        clearLastStage(): void {
            LastStageManager.clearLastStage();
        },

        hasLastProject(): boolean {
            const authStore = useAuthStore();
            return authStore.user?.email ? LastProjectManager.hasLastProject(authStore.user.email) : false;
        },

        hasLastStage(): boolean {
            return LastStageManager.hasLastStage();
        },

        hasLastStageForCurrentProject(): boolean {
            return this.currentProjectId ? LastStageManager.isLastStageFromProject(this.currentProjectId) : false;
        },

        getLastStage(): LastStageData | null {
            return LastStageManager.getLastStage();
        },

        async refreshTeamMembers(): Promise<void> {
            if (this.currentProjectId) {
                await this.loadTeamMembers(this.currentProjectId);
            }
        },

        async refreshProjectLabels(): Promise<void> {
            if (this.currentProjectId) {
                await this.loadProjectLabels(this.currentProjectId);
            }
        },

        setCurrentStageType(stageType: string): void {
            this.currentStageType = stageType;
            logger.info(`Updated current stage type: ${stageType}`);
        },

        clearCurrentStageType(): void {
            this.currentStageType = '';
            logger.info("Cleared current stage type");
        }
    }
});
