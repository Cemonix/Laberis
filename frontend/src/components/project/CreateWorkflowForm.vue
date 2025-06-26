<template>
    <form @submit.prevent="handleSubmit" class="create-workflow-form">
        <div class="form-field">
            <label for="workflow-name" class="form-label required">Workflow Name</label>
            <input
                id="workflow-name"
                v-model="form.name"
                type="text"
                class="form-input"
                placeholder="Enter workflow name"
                required
                maxlength="255"
                :disabled="isLoading"
            />
            <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>

        <div class="form-field">
            <label for="workflow-description" class="form-label">Description</label>
            <textarea
                id="workflow-description"
                v-model="form.description"
                class="form-textarea"
                placeholder="Enter workflow description (optional)"
                rows="3"
                maxlength="1000"
                :disabled="isLoading"
            ></textarea>
            <div class="field-help">Describe the purpose and scope of this workflow</div>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input
                    v-model="form.isDefault"
                    type="checkbox"
                    :disabled="isLoading"
                />
                <span class="checkbox-label">Set as default workflow</span>
            </label>
            <div class="field-help">Default workflows are automatically assigned to new tasks</div>
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
    </form>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue';
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
    description: '',
    isDefault: false
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
            description: form.description?.trim() || undefined,
            isDefault: form.isDefault
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
    form.isDefault = false;
    errors.name = '';
};

// Expose reset function if needed
defineExpose({ resetForm });
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.create-workflow-form {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-large;
    max-width: 500px;
    margin: 0 auto;
}

.form-field {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-small;
}

.form-label {
    font-weight: vars.$font-weight-medium;
    color: vars.$theme-text;
    font-size: vars.$font-size-medium;
    
    &.required::after {
        content: " *";
        color: vars.$color-error;
    }
}

.form-input,
.form-textarea {
    padding: vars.$padding-medium;
    border: 1px solid vars.$theme-border;
    border-radius: vars.$border-radius-sm;
    font-size: vars.$font-size-medium;
    font-family: inherit;
    background-color: vars.$theme-surface;
    color: vars.$theme-text;
    transition: border-color 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
    
    &:focus {
        outline: none;
        border-color: vars.$color-primary;
        box-shadow: 0 0 0 3px rgba(var(vars.$color-primary-rgb), 0.1);
    }
    
    &:disabled {
        background-color: vars.$theme-surface-variant;
        opacity: 0.6;
        cursor: not-allowed;
    }
    
    &::placeholder {
        color: vars.$theme-text-light;
    }
}

.form-textarea {
    resize: vertical;
    min-height: 80px;
    line-height: vars.$line-height-small;
}

.form-checkbox {
    display: flex;
    align-items: flex-start;
    gap: vars.$gap-small;
    cursor: pointer;
    
    input[type="checkbox"] {
        margin: 0;
        width: 18px;
        height: 18px;
        flex-shrink: 0;
        margin-top: 2px; // Align with first line of text
    }
    
    .checkbox-label {
        font-size: vars.$font-size-medium;
        color: vars.$theme-text;
        line-height: vars.$line-height-medium;
    }
    
    &:hover .checkbox-label {
        color: vars.$color-primary;
    }
}

.field-error {
    color: vars.$color-error;
    font-size: vars.$font-size-small;
    font-weight: vars.$font-weight-medium;
}

.field-help {
    color: vars.$theme-text-light;
    font-size: vars.$font-size-small;
    line-height: vars.$line-height-medium;
}

.form-actions {
    display: flex;
    gap: vars.$gap-medium;
    justify-content: flex-end;
    margin-top: vars.$margin-medium;
    padding-top: vars.$padding-medium;
    border-top: 1px solid vars.$theme-border;
    
    @media (max-width: 480px) {
        flex-direction: column-reverse;
        
        :deep(.btn) {
            width: 100%;
        }
    }
}
</style>
