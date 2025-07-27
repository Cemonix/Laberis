import { defineStore } from "pinia";
import { projectService, projectMemberService } from "@/services/api/projects";
import type { Project } from "@/types/project/project";
import type { ProjectMember } from "@/types/projectMember/projectMember";
import type { Label } from "@/types/label/label";
import type { ProjectRole } from "@/types/project/project";
import { useErrorHandler } from "@/composables/useErrorHandler";
import { AppLogger } from "@/utils/logger";
import { LastProjectManager } from "@/core/storage";

const logger = AppLogger.createServiceLogger("ProjectStore");

interface ProjectState {
    currentProject: Project | null;
    teamMembers: ProjectMember[];
    projectLabels: Label[];
    loading: boolean;
    membersLoading: boolean;
    labelsLoading: boolean;
}

export const useProjectStore = defineStore("project", {
    state: (): ProjectState => ({
        currentProject: null,
        teamMembers: [],
        projectLabels: [],
        loading: false,
        membersLoading: false,
        labelsLoading: false,
    }),

    getters: {
        currentProjectId(state): number | null {
            return state.currentProject?.id || null;
        },
        isProjectLoaded(state): boolean {
            return !!state.currentProject;
        },
        activeMembers(state): ProjectMember[] {
            return state.teamMembers.filter((member) => member.joinedAt !== undefined);
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
                LastProjectManager.saveLastProject(projectId, project.name);

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
            logger.info("Cleared project data");
        },

        clearLastProject(): void {
            LastProjectManager.clearLastProject();
        },

        getLastProject() {
            return LastProjectManager.getLastProject();
        },

        hasLastProject(): boolean {
            return LastProjectManager.hasLastProject();
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
        }
    }
});
