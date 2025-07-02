<template>
    <Form @submit="handleSubmit" class="create-workflow-form">
        <div class="form-group">
            <label for="workflow-name">Workflow Name</label>
            <input
                id="workflow-name"
                v-model="form.name"
                type="text"
                placeholder="Enter workflow name"
                required
                maxlength="255"
                :disabled="isLoading"
            />
            <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>

        <div class="form-group">
            <label for="workflow-description">Description (Optional)</label>
            <textarea
                id="workflow-description"
                v-model="form.description"
                placeholder="Enter workflow description (optional)"
                rows="3"
                maxlength="1000"
                :disabled="isLoading"
            ></textarea>
            <div class="field-help">Describe the purpose and scope of this workflow</div>
        </div>

        <div class="form-actions">
            <Button
                type="button"
                variant="secondary"
                @click="$emit('cancel')"
                :disabled="isLoading"
            >
                Cancel
            </Button>
            <Button
                type="submit"
                variant="primary"
                :loading="isLoading"
                :disabled="!isFormValid"
            >
                Create Workflow
            </Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import type { CreateWorkflowRequest } from '@/types/workflow';

interface Props {
    projectId: number;
}

interface Emits {
    (e: 'save', data: CreateWorkflowRequest): void;
    (e: 'cancel'): void;
}

defineProps<Props>();
const emit = defineEmits<Emits>();

const isLoading = ref(false);

const form = reactive<CreateWorkflowRequest>({
    name: '',
    description: ''
});

const errors = reactive({
    name: ''
});

const isFormValid = computed(() => {
    return form.name.trim().length >= 3 && !errors.name;
});

const validateForm = (): boolean => {
    errors.name = '';
    
    if (!form.name.trim()) {
        errors.name = 'Workflow name is required';
        return false;
    }
    
    if (form.name.trim().length < 3) {
        errors.name = 'Workflow name must be at least 3 characters long';
        return false;
    }
    
    if (form.name.trim().length > 255) {
        errors.name = 'Workflow name must be less than 255 characters';
        return false;
    }
    
    return true;
};

const handleSubmit = async () => {
    if (!validateForm()) {
        return;
    }
    
    isLoading.value = true;
    
    try {
        const formData: CreateWorkflowRequest = {
            name: form.name.trim(),
            description: form.description?.trim() || undefined
        };
        
        emit('save', formData);
    } catch (error) {
        console.error('Error preparing form data:', error);
    } finally {
        isLoading.value = false;
    }
};

// Reset form when component is mounted or reset
const resetForm = () => {
    form.name = '';
    form.description = '';
    errors.name = '';
};

// Expose reset function if needed
defineExpose({ resetForm });
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.create-workflow-form {
    max-width: 500px;
    margin: 0 auto;
}

.field-error {
    color: vars.$color-error;
    font-size: vars.$font-size-small;
    font-weight: vars.$font-weight-medium;
    margin-top: vars.$margin-xsmall;
}

.field-help {
    color: vars.$theme-text-light;
    font-size: vars.$font-size-small;
    line-height: vars.$line-height-medium;
    margin-top: vars.$margin-xsmall;
}
</style>
