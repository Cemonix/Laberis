import type { Project } from "@/types/project/project";
import type { ProjectMember } from "@/types/projectMember/projectMember";
import type { Label } from "@/types/label/label";

export interface ProjectState {
    currentProject: Project | null;
    teamMembers: ProjectMember[];
    projectLabels: Label[];
    loading: boolean;
    membersLoading: boolean;
    labelsLoading: boolean;
    currentStageType: string;
}