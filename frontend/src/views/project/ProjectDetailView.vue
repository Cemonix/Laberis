<template>
    <div class="page-container">
        <div v-if="loading" class="loading-state">
            <p>Loading project details...</p>
        </div>

        <div v-else-if="error" class="error-state">
            <p>{{ error }}</p>
        </div>

        <div v-else-if="project">
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
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { Project } from '@/types/project/project';
import { projectService } from '@/services/api/projectService';
import { useAlert } from '@/composables/useAlert';

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
        console.error('Error fetching project:', err);
        error.value = 'Failed to load project details. Please try again.';
        await showAlert('Error', 'Project not found. You will be redirected to the projects list.');
        router.push('/projects');
    } finally {
        loading.value = false;
    }
};

onMounted(() => {
    fetchProject();
});
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.page-container {
    padding: vars.$padding-large;
    width: 100%;
    max-width: vars.$max-width-wide;
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
        font-size: vars.$font_size_large;
        color: vars.$theme-text-light;
    }
}

.error-state p {
    color: vars.$color-error;
}

.project-header {
    margin-bottom: vars.$margin-medium;
    .project-name {
        font-size: 2.5rem;
        color: vars.$theme-text;
    }
    .project-description {
        font-size: vars.$font_size_large;
        color: vars.$theme-text-light;
        margin-top: vars.$margin-small;
    }
}

.project-sub-nav {
    display: flex;
    gap: vars.$gap-small;
    border-bottom: vars.$border-width solid vars.$color-gray-300;
    margin-bottom: vars.$margin-large * 2;
}

.sub-nav-link {
    padding: vars.$padding-small vars.$padding-medium;
    text-decoration: none;
    color: vars.$color-gray-700;
    border-bottom: 3px solid transparent;
    transition: color 0.2s ease-in-out, border-color 0.2s ease-in-out;

    &:hover {
        color: vars.$theme-text;
    }

    &.is-active {
        color: vars.$color-primary;
        border-bottom-color: vars.$color-primary;
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