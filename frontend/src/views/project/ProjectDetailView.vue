<template>
    <div class="page-container">
        <div v-if="loading" class="loading-state">
            <p>Loading project details...</p>
        </div>

        <div v-else-if="error" class="error-state">
            <p>{{ error }}</p>
        </div>

        <div v-else-if="project" class="project-detail-view">
            <header class="project-header">
                <h1 class="project-name">{{ project.name }}</h1>
                <p class="project-description">{{ project.description }}</p>
            </header>

            <nav class="project-sub-nav">
                <router-link :to="`/projects/${projectId}`" class="sub-nav-link" exact-active-class="is-active">Dashboard</router-link>
                <router-link :to="`/projects/${projectId}/label-schemes`" class="sub-nav-link" active-class="is-active">Label Schemes</router-link>
                <router-link :to="`/projects/${projectId}/data-sources`" class="sub-nav-link" active-class="is-active">Data Sources</router-link>
                <router-link :to="`/projects/${projectId}/workflows`" class="sub-nav-link" active-class="is-active">Workflows</router-link>
                <router-link :to="`/projects/${projectId}/settings`" class="sub-nav-link" active-class="is-active">Settings</router-link>
            </nav>

            <router-view v-slot="{ Component }">
                <transition name="fade-slide" mode="out-in">
                    <component :is="Component" />
                </transition>
            </router-view>
        </div>

        <div v-else class="error-state">
            <p>Project not found.</p>
        </div>
    </div>
</template>

<script setup lang="ts">
import {onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import type {Project} from '@/types/project/project';
import {projectService} from '@/services/api/projects';
import {useAlert} from '@/composables/useAlert';
import {AppLogger} from '@/utils/logger';

const logger = AppLogger.createComponentLogger('ProjectDetailView');

const route = useRoute();
const router = useRouter();
const { showAlert } = useAlert();

const project = ref<Project | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);
const projectId = route.params.projectId as string;

const fetchProject = async () => {
    loading.value = true;
    error.value = null;
    
    try {
        const projectIdNum = parseInt(projectId, 10);
        if (isNaN(projectIdNum)) {
            throw new Error('Invalid project ID');
        }
        
        project.value = await projectService.getProject(projectIdNum);
    } catch (err) {
        logger.error('Error fetching project:', err);
        error.value = 'Failed to load project details. Please try again.';
        await showAlert('Error', 'Project not found. You will be redirected to the projects list.');
        await router.push('/projects');
    } finally {
        loading.value = false;
    }
};

onMounted(() => {
    fetchProject();
});
</script>

<style lang="scss" scoped>
.page-container {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    padding: 2rem;
    width: 100%;
    margin: 0 auto;
}

.loading-state,
.error-state {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 200px;
    text-align: center;
    
    p {
        font-size: 1.25rem;
        color: var(--color-gray-800);
    }
}

.error-state p {
    color: var(--color-error);
}

.project-detail-view {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    border-radius: 8px;
    padding: 1.5rem;
}

.project-header {
    margin-bottom: 1rem;
    .project-name {
        font-size: 2.5rem;
        color: var(--color-gray-800);
    }
    .project-description {
        font-size: 1.25rem;
        color: var(--color-gray-600);
        margin-top: 0.5rem;
    }
}

.project-sub-nav {
    display: flex;
    gap: 0.5rem;
    border-bottom: 1px solid var(--color-gray-300);
    margin-bottom: 3rem;
}

.sub-nav-link {
    padding: 0.5rem 1rem;
    text-decoration: none;
    color: var(--color-gray-700);
    border-bottom: 3px solid transparent;
    transition: color 0.2s ease-in-out, border-color 0.2s ease-in-out;

    &:hover {
        color: var(--color-gray-800);
    }

    &.is-active {
        color: var(--color-primary);
        border-bottom-color: var(--color-primary);
    }
}

.fade-slide-enter-active,
.fade-slide-leave-active {
    transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-slide-enter-from,
.fade-slide-leave-to {
    opacity: 0;
    transform: translateY(20px);
}
</style>