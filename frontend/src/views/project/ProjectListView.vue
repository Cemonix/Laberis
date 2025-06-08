<template>
    <div class="page-container">
        <h1 class="page-title">Projects</h1>
        <div class="project-grid">
            <ProjectCard
                v-for="project in projects"
                :key="project.projectId"
                :project="project"
            />
        </div>

        <button @click="openModal" class="fab" aria-label="Create new project">+</button>

        <ModalWindow 
            :is-open="isModalOpen" 
            title="Create New Project" 
            :hide-footer="true"
            @close="closeModal"
        >
            <CreateProjectForm @cancel="closeModal" @save="handleCreateProject" />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import ProjectCard from '@/components/project/ProjectCard.vue';
import ModalWindow from '@/components/common/ModalWindow.vue';
import CreateProjectForm from '@/components/project/CreateProjectForm.vue';
import type { Project } from '@/types/project/project';
import { ProjectStatus, ProjectType } from '@/types/project/project';

const isModalOpen = ref(false);
const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const projects = ref<Project[]>([]);

const handleCreateProject = (formData: { name: string; description: string; projectType: ProjectType }) => {
    // Create a new project object and add it to our local mock list
    const newProject: Project = {
        projectId: Date.now(), // Use timestamp as a temporary unique ID
        name: formData.name,
        description: formData.description,
        projectType: formData.projectType,
        status: ProjectStatus.ACTIVE, // Default to active
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
    };
    projects.value.unshift(newProject); // Add to the beginning of the list

    // TODO: API service here
    // await projectService.createProject(formData);
    // await fetchProjects(); // And then refresh the list

    closeModal();
};

// TODO: Replace with actual API call to fetch projects
onMounted(() => {
    projects.value = [
        {
            projectId: 1,
            name: 'Urban Object Detection',
            description: 'Detecting cars, pedestrians, and traffic signs in city environments.',
            projectType: ProjectType.OBJECT_DETECTION,
            status: ProjectStatus.ACTIVE,
            createdAt: '2024-05-15T10:00:00Z',
            updatedAt: '2024-06-03T14:30:00Z',
        },
        {
            projectId: 2,
            name: 'Medical Image Segmentation',
            description: 'Segmenting tumors in MRI scans.',
            projectType: ProjectType.IMAGE_SEGMENTATION,
            status: ProjectStatus.ACTIVE,
            createdAt: '2024-03-20T09:00:00Z',
            updatedAt: '2024-05-28T11:00:00Z',
        },
        {
            projectId: 3,
            name: 'Sentiment Analysis for Reviews',
            projectType: ProjectType.TEXT_ANNOTATION,
            status: ProjectStatus.READ_ONLY,
            createdAt: '2023-11-10T12:00:00Z',
            updatedAt: '2024-01-25T16:45:00Z',
        },
        {
            projectId: 4,
            name: 'Legacy Project - Archived',
            description: 'Old project for data classification.',
            projectType: ProjectType.IMAGE_CLASSIFICATION,
            status: ProjectStatus.ARCHIVED,
            createdAt: '2022-01-15T18:00:00Z',
            updatedAt: '2022-09-30T17:00:00Z',
        },
    ];
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.page-container {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    padding: vars.$padding-large;
    width: 100%;
    max-width: vars.$max-width-standard;
    margin: 0 auto;
}

.page-title {
    font-size: vars.$font_size_xxlarge;
    margin-bottom: vars.$margin-large;
    color: vars.$theme-text;
}

.project-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: vars.$gap-large;
}

.fab {
    position: absolute;
    bottom: vars.$padding-xlarge;
    right: vars.$padding-xlarge;
    width: 60px;
    height: 60px;
    border-radius: 50%;
    background-color: vars.$color-primary;
    color: vars.$color-white;
    border: none;
    font-size: 2.5rem;
    line-height: 1;
    box-shadow: vars.$shadow-md;
    cursor: pointer;
    padding-bottom: 4px;
    transition: transform 0.2s ease-in-out, background-color 0.2s ease-in-out;
    display: flex;
    justify-content: center;
    align-items: center;
    flex-direction: row;

    &:hover {
        background-color: vars.$color-primary-hover;
        transform: scale(1.1);
    }
}
</style>