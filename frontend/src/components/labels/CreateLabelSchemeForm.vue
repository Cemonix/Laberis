<template>
    <Form @submit="handleSubmit">
        <div class="form-group">
            <label for="schemeName">Scheme Name</label>
            <input
                id="schemeName"
                v-model="formData.name"
                type="text"
                required
                :disabled="props.disabled"
                placeholder="e.g., General Object Classes"
            />
        </div>
        <div class="form-group">
            <label for="schemeDescription">Description (Optional)</label>
            <textarea
                id="schemeDescription"
                v-model="formData.description"
                rows="3"
                :disabled="props.disabled"
                placeholder="A short description of this label scheme."
            ></textarea>
        </div>
        <div class="form-actions">
            <Button
                type="button"
                @click="$emit('cancel')"
                class="btn btn-secondary"
                :disabled="props.disabled"
                >Cancel</Button
            >
            <Button 
                type="submit" 
                class="btn btn-primary"
                :disabled="props.disabled"
            >
                {{ props.disabled ? 'Creating...' : 'Create Scheme' }}
            </Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import {ref} from "vue";
import Form from "@/components/common/Form.vue";
import Button from "@/components/common/Button.vue";
import type {FormPayloadLabelScheme} from "@/types/label/labelScheme";
import {useAlert} from "@/composables/useAlert";

const props = defineProps<{
    disabled?: boolean;
}>();

const { showAlert } = useAlert();

const emit = defineEmits<{
    (e: "save", formData: FormPayloadLabelScheme): void;
    (e: "cancel"): void;
}>();

const formData = ref<FormPayloadLabelScheme>({
    name: "",
    description: "",
});

const handleSubmit = async () => {
    if (!formData.value.name.trim()) {
        await showAlert("Validation Error", "Scheme Name is required.");
        return;
    }
    emit("save", formData.value);
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
        font-family: inherit;
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
