import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { projectService } from "@/services/api/projectService";
import { projectMemberService } from "@/services/api/projectMemberService";
import type { Project } from "@/types/project/project";
import type { ProjectMember } from "@/types/projectMember/projectMember";
import type { Label } from "@/types/label/label";
import type { ProjectRole } from "@/types/project/project";
import { useErrorHandler } from "@/composables/useErrorHandler";
import { AppLogger } from "@/utils/logger";

const logger = AppLogger.createServiceLogger("ProjectStore");

export const useProjectStore = defineStore("project", () => {
    const { handleError } = useErrorHandler();

    // State
    const currentProject = ref<Project | null>(null);
    const teamMembers = ref<ProjectMember[]>([]);
    const projectLabels = ref<Label[]>([]);
    const loading = ref(false);
    const membersLoading = ref(false);
    const labelsLoading = ref(false);

    // Computed
    const currentProjectId = computed(() => currentProject.value?.id || null);
    const isProjectLoaded = computed(() => !!currentProject.value);
    const activeMembers = computed(() =>
        teamMembers.value.filter((member) => member.joinedAt !== undefined)
    );
    const teamMemberCount = computed(() => teamMembers.value.length);
    const labelCount = computed(() => projectLabels.value.length);

    // Actions
    const setCurrentProject = async (projectId: number): Promise<void> => {
        if (currentProject.value?.id === projectId) {
            logger.info(`Project ${projectId} already loaded`);
            return;
        }

        loading.value = true;
        try {
            const project = await projectService.getProject(projectId);
            currentProject.value = project;
            logger.info(`Loaded project: ${project.name} (ID: ${projectId})`);

            // Load related data in parallel
            await Promise.all([
                loadTeamMembers(projectId),
                loadProjectLabels(projectId),
            ]);
        } catch (error) {
            logger.error(`Failed to load project ${projectId}`, error);
            handleError(error, "Failed to load project");
            throw error;
        } finally {
            loading.value = false;
        }
    };

    const loadTeamMembers = async (projectId: number): Promise<void> => {
        membersLoading.value = true;
        try {
            const members = await projectMemberService.getProjectMembers(
                projectId
            );
            teamMembers.value = members;
            logger.info(
                `Loaded ${members.length} team members for project ${projectId}`
            );
        } catch (error) {
            logger.error(
                `Failed to load team members for project ${projectId}`,
                error
            );
            handleError(error, "Failed to load team members");
        } finally {
            membersLoading.value = false;
        }
    };

    const loadProjectLabels = async (projectId: number): Promise<void> => {
        labelsLoading.value = true;
        try {
            // Note: This would need a proper API endpoint for project labels
            // For now, we'll skip this functionality until the API is available
            projectLabels.value = [];
            logger.info(
                `Labels functionality not yet implemented for project ${projectId}`
            );
        } catch (error) {
            logger.error(
                `Failed to load labels for project ${projectId}`,
                error
            );
            handleError(error, "Failed to load project labels");
        } finally {
            labelsLoading.value = false;
        }
    };

    const addTeamMember = (member: ProjectMember): void => {
        teamMembers.value.push(member);
        logger.info(`Added team member: ${member.userName || member.email}`);
    };

    const removeTeamMember = (memberEmail: string): void => {
        const index = teamMembers.value.findIndex(
            (member) => member.email === memberEmail
        );
        if (index !== -1) {
            const removedMember = teamMembers.value.splice(index, 1)[0];
            logger.info(
                `Removed team member: ${
                    removedMember.userName || removedMember.email
                }`
            );
        }
    };

    const updateTeamMemberRole = (
        memberEmail: string,
        newRole: ProjectRole
    ): void => {
        const member = teamMembers.value.find((m) => m.email === memberEmail);
        if (member) {
            member.role = newRole;
            logger.info(
                `Updated role for ${
                    member.userName || member.email
                } to ${newRole}`
            );
        }
    };

    const addLabel = (label: Label): void => {
        projectLabels.value.push(label);
        logger.info(`Added label: ${label.name}`);
    };

    const updateLabel = (updatedLabel: Label): void => {
        const index = projectLabels.value.findIndex(
            (label) => label.labelId === updatedLabel.labelId
        );
        if (index !== -1) {
            projectLabels.value[index] = updatedLabel;
            logger.info(`Updated label: ${updatedLabel.name}`);
        }
    };

    const removeLabel = (labelId: number): void => {
        const index = projectLabels.value.findIndex(
            (label) => label.labelId === labelId
        );
        if (index !== -1) {
            const removedLabel = projectLabels.value.splice(index, 1)[0];
            logger.info(`Removed label: ${removedLabel.name}`);
        }
    };

    const updateProject = (updatedProject: Project): void => {
        currentProject.value = updatedProject;
        logger.info(`Updated project: ${updatedProject.name}`);
    };

    const clearProject = (): void => {
        currentProject.value = null;
        teamMembers.value = [];
        projectLabels.value = [];
        logger.info("Cleared project data");
    };

    const refreshTeamMembers = async (): Promise<void> => {
        if (currentProjectId.value) {
            await loadTeamMembers(currentProjectId.value);
        }
    };

    const refreshProjectLabels = async (): Promise<void> => {
        if (currentProjectId.value) {
            await loadProjectLabels(currentProjectId.value);
        }
    };

    return {
        // State
        currentProject,
        teamMembers,
        projectLabels,
        loading,
        membersLoading,
        labelsLoading,

        // Computed
        currentProjectId,
        isProjectLoaded,
        activeMembers,
        teamMemberCount,
        labelCount,

        // Actions
        setCurrentProject,
        loadTeamMembers,
        loadProjectLabels,
        addTeamMember,
        removeTeamMember,
        updateTeamMemberRole,
        addLabel,
        updateLabel,
        removeLabel,
        updateProject,
        clearProject,
        refreshTeamMembers,
        refreshProjectLabels,
    };
});
