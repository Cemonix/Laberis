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
import { ref, computed } from 'vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import LabelChip from './LabelChip.vue';
import type { CreateLabelRequest } from '@/types/label/requests';
import { generateRandomColor, isValidHexColor } from '@/utils/colors';

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

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.form-group {
    margin-bottom: vars.$margin-large;

    label {
        display: block;
        margin-bottom: vars.$margin-small;
        font-weight: vars.$font-weight-heading;
        color: vars.$theme-text;
    }

    input, textarea {
        width: 100%;
        padding: vars.$padding-medium;
        border: 1px solid vars.$theme-border;
        border-radius: vars.$border-radius;
        font-size: vars.$font_size_medium;
        
        &:focus {
            outline: none;
            border-color: vars.$color-primary;
            box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
        }
        
        &:disabled {
            background-color: vars.$color-gray-200;
            color: vars.$theme-text-light;
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
    gap: vars.$gap-medium;
    align-items: center;

    .color-picker {
        width: 60px;
        height: 40px;
        padding: 0;
        border: 1px solid vars.$theme-border;
        border-radius: vars.$border-radius;
        cursor: pointer;
        
        &::-webkit-color-swatch-wrapper {
            padding: 0;
        }
        
        &::-webkit-color-swatch {
            border: none;
            border-radius: calc(vars.$border-radius - 1px);
        }
    }

    .color-text {
        flex: 1;
        font-family: monospace;
    }
}

.label-preview {
    margin: vars.$margin-large 0;
    padding: vars.$padding-large;
    background-color: vars.$color-gray-200;
    border-radius: vars.$border-radius;

    h4 {
        margin-bottom: vars.$margin-medium;
        color: vars.$theme-text;
    }

    .preview-placeholder {
        color: vars.$theme-text-light;
        font-style: italic;
    }
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: vars.$gap-medium;
    margin-top: vars.$margin-xlarge;
    padding-top: vars.$padding-large;
    border-top: 1px solid vars.$theme-border;
}
</style>
