<template>
    <Form @submit="handleSubmit">
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
            <Button type="button" @click="$emit('cancel')">Cancel</Button>
            <Button type="submit">Create Project</Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import {ref} from 'vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import {ProjectType} from '@/services/project/project.types';
import {useToast} from '@/composables/useToast';

const { showWarning } = useToast();

const emit = defineEmits<{
    (e: 'save', formData: { name: string; description: string; projectType: ProjectType }): void;
    (e: 'cancel'): void;
}>();

const projectTypes = Object.values(ProjectType);

const formData = ref({
    name: '',
    description: '',
    projectType: ProjectType.OTHER,
});

const handleSubmit = async () => {
    if (!formData.value.name || !formData.value.projectType) {
        showWarning('Missing Information', 'Please fill in all required fields.');
        return;
    }
    emit('save', { ...formData.value });
};
</script>

<style lang="scss" scoped>
</style>