<template>
    <form @submit.prevent="handleSubmit" class="create-project-form">
        <div class="form-group">
            <label for="projectName">Project Name</label>
            <input id="projectName" v-model="formData.name" type="text" required placeholder="e.g., Autonomous Drone Vision" />
        </div>
        <div class="form-group">
            <label for="projectDescription">Description</label>
            <textarea id="projectDescription" v-model="formData.description" rows="3" placeholder="A short description of the project's goals."></textarea>
        </div>
        <div class="form-group">
            <label for="projectType">Project Type</label>
            <select id="projectType" v-model="formData.projectType" required>
                <option disabled value="">Please select a type</option>
                <option v-for="type in projectTypes" :key="type" :value="type">
                    {{ type.replace('_', ' ') }}
                </option>
            </select>
        </div>

        <div class="form-actions">
            <button type="button" @click="$emit('cancel')" class="btn btn-secondary">Cancel</button>
            <button type="submit" class="btn btn-primary">Create Project</button>
        </div>
    </form>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { ProjectType } from '@/types/project/project';

const emit = defineEmits<{
    (e: 'save', formData: { name: string; description: string; projectType: ProjectType }): void;
    (e: 'cancel'): void;
}>();

const projectTypes = Object.values(ProjectType);

const formData = ref({
    name: '',
    description: '',
    projectType: ProjectType.OTHER, // Default to OTHER type
});

const handleSubmit = () => {
    if (!formData.value.name || !formData.value.projectType) {
        alert('Project Name and Type are required.');
        return;
    }
    emit('save', { ...formData.value });
};
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.create-project-form {
    display: flex;
    flex-direction: column;
    gap: vars.$padding-medium;
}
</style>