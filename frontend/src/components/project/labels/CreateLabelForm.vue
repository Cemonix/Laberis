<template>
    <Form @submit="handleSubmit">
        <div class="form-group">
            <label for="labelName">Label Name</label>
            <input
                id="labelName"
                v-model="formData.name"
                type="text"
                required
                :disabled="props.disabled"
                placeholder="e.g., Person, Car, Tree"
            />
        </div>
        
        <div class="form-group">
            <label for="labelColor">Color</label>
            <div class="color-input-group">
                <input
                    id="labelColor"
                    v-model="formData.color"
                    type="color"
                    required
                    :disabled="props.disabled"
                    class="color-picker"
                />
                <input
                    v-model="formData.color"
                    type="text"
                    :disabled="props.disabled"
                    placeholder="#FF0000"
                    class="color-text"
                    pattern="^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"
                />
            </div>
        </div>
        
        <div class="form-group">
            <label for="labelDescription">Description (Optional)</label>
            <textarea
                id="labelDescription"
                v-model="formData.description"
                rows="3"
                :disabled="props.disabled"
                placeholder="A short description of this label."
            ></textarea>
        </div>

        <div class="label-preview">
            <h4>Preview:</h4>
            <LabelChip 
                v-if="formData.name && formData.color"
                :label="{
                    labelId: 0,
                    name: formData.name,
                    color: formData.color,
                    description: formData.description,
                    labelSchemeId: 0,
                    createdAt: new Date().toISOString()
                }"
            />
            <p v-else class="preview-placeholder">Enter a name and color to see preview</p>
        </div>

        <div class="form-actions">
            <Button
                type="button"
                variant="secondary"
                @click="handleCancel"
                :disabled="props.disabled"
            >
                Cancel
            </Button>
            <Button
                type="submit"
                variant="primary"
                :disabled="props.disabled || !isFormValid"
            >
                Create Label
            </Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import {computed, ref} from 'vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import LabelChip from './LabelChip.vue';
import type {CreateLabelRequest} from '@/services/project/labelScheme/label.types';
import {generateRandomColor, isValidHexColor} from '@/core/theming';

const props = defineProps<{
    disabled?: boolean;
}>();

const emit = defineEmits<{
    cancel: [];
    save: [formData: CreateLabelRequest];
}>();

const formData = ref<CreateLabelRequest>({
    name: '',
    color: generateRandomColor(),
    description: ''
});

const isFormValid = computed(() => {
    return formData.value.name.trim().length > 0 && isValidHexColor(formData.value.color);
});

const handleSubmit = () => {
    if (!isFormValid.value) return;
    
    const submitData: CreateLabelRequest = {
        name: formData.value.name.trim(),
        color: formData.value.color,
        description: formData.value.description?.trim() || undefined
    };
    
    emit('save', submitData);
    
    // Reset form for next label creation
    formData.value = {
        name: '',
        color: generateRandomColor(),
        description: ''
    };
};

const handleCancel = () => {
    emit('cancel');
};
</script>

<style scoped>
.form-group {
    margin-bottom: 1.5rem;

    label {
        display: block;
        margin-bottom: 0.5rem;
        font-weight: 700;
        color: var(--color-gray-800);
    }

    input, textarea {
        width: 100%;
        padding: 1rem;
        border: 1px solid var(--color-gray-400);
        border-radius: 4px;
        font-size: 1rem;
        
        &:focus {
            outline: none;
            border-color: var(--color-primary);
            box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
        }
        
        &:disabled {
            background-color: var(--color-gray-200);
            color: var(--color-gray-600);
            cursor: not-allowed;
        }
    }

    textarea {
        resize: vertical;
        min-height: 100px;
        max-height: 300px;
    }
}

.color-input-group {
    display: flex;
    gap: 1rem;
    align-items: center;

    .color-picker {
        width: 60px;
        height: 40px;
        padding: 0;
        border: 1px solid var(--color-gray-400);
        border-radius: 4px;
        cursor: pointer;
        
        &::-webkit-color-swatch-wrapper {
            padding: 0;
        }
        
        &::-webkit-color-swatch {
            border: none;
            border-radius: calc(4px - 1px);
        }
    }

    .color-text {
        flex: 1;
        font-family: monospace;
    }
}

.label-preview {
    margin: 1.5rem 0;
    padding: 1.5rem;
    background-color: var(--color-gray-200);
    border-radius: 4px;

    h4 {
        margin-bottom: 1rem;
        color: var(--color-gray-800);
    }

    .preview-placeholder {
        color: var(--color-gray-600);
        font-style: italic;
    }
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
    margin-top: 2rem;
    padding-top: 1.5rem;
    border-top: 1px solid var(--color-gray-400);
}
</style>
