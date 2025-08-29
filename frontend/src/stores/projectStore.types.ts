import type { Project } from "@/services/project/project.types";
import type { ProjectMember } from "@/services/project/projectMember.types";
import type { Label } from "@/services/project/labelScheme/label.types";

export interface ProjectState {
    currentProject: Project | null;
    teamMembers: ProjectMember[];
    projectLabels: Label[];
    loading: boolean;
    membersLoading: boolean;
    labelsLoading: boolean;
    currentStageType: string;
}