<template>
    <div class="page-container">
        <h1 class="page-title">Projects</h1>
        
        <div v-if="loading" class="loading-state">
            Loading projects...
        </div>
        
        <div v-else-if="error" class="error-state">
            <p>Error loading projects: {{ error }}</p>
            <Button @click="fetchProjects">Retry</Button>
        </div>
        
        <div v-else class="project-grid">
            <div v-if="projects.length === 0" class="no-projects">
                No projects found. Create your first project!
            </div>
            <ProjectCard
                v-for="project in projects"
                :key="project.id"
                :project="project"
            />
        </div>

        <Button @click="openModal" class="fab" aria-label="Create new project">+</Button>

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
import ModalWindow from '@/components/common/modals/ModalWindow.vue';
import CreateProjectForm from '@/components/project/CreateProjectForm.vue';
import Button from '@/components/common/Button.vue';
import type { Project } from '@/types/project/project';
import { ProjectType } from '@/types/project/project';
import { projectService } from '@/services/api/projectService';
import type { CreateProjectRequest } from '@/types/project/requests';
import { useAlert } from '@/composables/useAlert';

const { showAlert } = useAlert();

const isModalOpen = ref(false);
const projects = ref<Project[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const fetchProjects = async () => {
    loading.value = true;
    error.value = null;
    
    try {
        const response = await projectService.getProjects({
            sortBy: 'name',
            sortOrder: 'desc'
        });
        projects.value = response.data;
    } catch (err) {
        console.error('Error fetching projects:', err);
        error.value = 'Failed to load projects. Please try again.';
    } finally {
        loading.value = false;
    }
};

const handleCreateProject = async (formData: { name: string; description: string; projectType: ProjectType }) => {
    try {
        const createRequest: CreateProjectRequest = {
            name: formData.name,
            description: formData.description,
            projectType: formData.projectType,
        };
        
        const newProject = await projectService.createProject(createRequest);
        projects.value.unshift(newProject);
        closeModal();
        
        await showAlert('Success', 'Project created successfully!');
    } catch (err) {
        console.error('Error creating project:', err);
        await showAlert('Error', 'Failed to create project. Please try again.');
    }
};

onMounted(() => {
    fetchProjects();
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
    font-size: vars.$font-size-xxlarge;
    margin-bottom: vars.$margin-large;
    color: vars.$theme-text;
}

.project-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: vars.$gap-large;
}

.loading-state,
.error-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: vars.$gap-medium;
    padding: vars.$padding-xlarge;
    color: vars.$theme-text-light;
    text-align: center;
}

.no-projects {
    grid-column: 1 / -1;
    text-align: center;
    padding: vars.$padding-xlarge;
    color: vars.$theme-text-light;
    font-style: italic;
}

.debug-info {
    margin-top: vars.$margin-large;
    padding: vars.$padding-medium;
    background-color: #f5f5f5;
    border-radius: vars.$border-radius-sm;
    font-family: monospace;
    font-size: 12px;
    
    pre {
        white-space: pre-wrap;
        max-height: 200px;
        overflow-y: auto;
    }
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