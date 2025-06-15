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
        <div class="form-section-divider">
            <label>Labels</label>
        </div>
        <div class="labels-preview-list" v-if="formData.labels.length > 0">
            <div
                v-for="(label, index) in formData.labels"
                :key="index"
                class="label-preview-item"
            >
                <span
                    class="label-preview-color"
                    :style="{ backgroundColor: label.color }"
                ></span>
                <span class="label-preview-name">{{ label.name }}</span>
                <Button
                    type="button"
                    @click="removeLabel(index)"
                    class="remove-label-btn"
                    aria-label="Remove label"
                    >&times;</Button
                >
            </div>
        </div>
        <div class="add-label-group">
            <input
                type="text"
                v-model.trim="newLabelName"
                class="add-label-input"
                placeholder="New label name"
                :disabled="props.disabled"
                @keydown.enter.prevent="addLabel"
            />
            <input
                type="color"
                v-model="newLabelColor"
                class="color-picker"
                :disabled="props.disabled"
                title="Select label color"
            />
            <Button
                type="button"
                @click.prevent="addLabel"
                class="btn btn-primary"
                :disabled="props.disabled"
                >Add</Button
            >
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
import { ref } from "vue";
import Form from "@/components/common/Form.vue";
import Button from "@/components/common/Button.vue";
import type { FormPayloadLabelScheme } from "@/types/label/labelScheme";
import { useAlert } from "@/composables/useAlert";

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
    labels: [],
});

const newLabelName = ref("");
const newLabelColor = ref("#4287f5");

const addLabel = async () => {
    if (!newLabelName.value) {
        await showAlert(
            "Missing Information",
            "Please enter a name for the label."
        );
        return;
    }

    if (
        formData.value.labels.some(
            (label) =>
                label.name.toLowerCase() === newLabelName.value.toLowerCase()
        )
    ) {
        await showAlert(
            "Duplicate Name",
            "This label name already exists in the list."
        );
        return;
    }

    formData.value.labels.push({
        name: newLabelName.value,
        color: newLabelColor.value,
    });

    newLabelName.value = "";
};

const removeLabel = (index: number) => {
    formData.value.labels.splice(index, 1);
};

const handleSubmit = async () => {
    if (!formData.value.name.trim()) {
        await showAlert("Validation Error", "Scheme Name is required.");
        return;
    }
    emit("save", formData.value);
};
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;
@use "@/styles/variables/theme" as theme;

.form-section-divider {
    margin-top: vars.$margin-medium;
    padding-bottom: vars.$padding-small;
    border-bottom: vars.$border-width solid vars.$theme-border;
    label {
        font-weight: vars.$font-weight-heading;
        color: vars.$theme-text-light;
    }
}

.labels-preview-list {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-tiny;
    margin-top: vars.$margin-small;
    max-height: 150px;
    overflow-y: auto;
}

.label-preview-item {
    display: flex;
    align-items: center;
    gap: vars.$gap-small;
    background-color: vars.$color-gray-200;
    padding: vars.$padding-small;
    border-radius: vars.$border-radius-sm;
}

.label-preview-color {
    width: 16px;
    height: 16px;
    border-radius: 3px;
    flex-shrink: 0;
}

.label-preview-name {
    flex-grow: 1;
}

.add-label-input {
    padding: vars.$padding-small;
    border: vars.$border-width solid vars.$color-gray-400;
    border-radius: vars.$border-radius-sm;
    font-size: vars.$font_size_medium;
}

.remove-label-btn {
    background: none;
    border: none;
    font-size: 1.5rem;
    color: vars.$theme-text;
    cursor: pointer;
    line-height: 1;
    &:hover {
        color: vars.$color-error;
    }
}

.add-label-group {
    display: flex;
    gap: vars.$gap-small;
    align-items: center;
    margin-top: vars.$margin-small;

    input[type="text"] {
        flex-grow: 1;
    }
    .color-picker {
        height: 38px;
        width: 45px;
        padding: 2px;
        border: vars.$border-width solid vars.$color-gray-300;
        border-radius: vars.$border-radius-sm;
        cursor: pointer;
    }
}
</style>
