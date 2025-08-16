<template>
    <ModalWindow
        :title="`Configure ${widget.title}`"
        :isOpen="true"
        @close="$emit('close')"
        size="medium"
    >
        <div class="widget-settings-modal">
            <div class="settings-form">
                <div class="form-section">
                    <h4>Widget Configuration</h4>
                    <div class="form-group">
                        <label for="widget-title">Widget Title</label>
                        <input
                            id="widget-title"
                            v-model="localSettings.title"
                            type="text"
                            class="form-input"
                            placeholder="Enter widget title"
                        />
                    </div>
                </div>
                <div
                    v-if="widgetDefinition?.availableSettings?.length"
                    class="form-section"
                >
                    <h4>Widget Settings</h4>
                    <div
                        v-for="setting in widgetDefinition.availableSettings"
                        :key="setting.key"
                        class="form-group"
                    >
                        <label :for="`setting-${setting.key}`">
                            {{ setting.label }}
                            <span v-if="setting.required" class="required">*</span>
                        </label>
                        <p v-if="setting.helpText" class="help-text">
                            {{ setting.helpText }}
                        </p>
                        <input
                            v-if="setting.type === 'text'"
                            :id="`setting-${setting.key}`"
                            v-model="localSettings.settings[setting.key]"
                            type="text"
                            class="form-input"
                            :placeholder="setting.defaultValue?.toString() || ''"
                            :required="setting.required"
                            :pattern="setting.validationRegex || undefined"
                        />
                        <input
                            v-else-if="setting.type === 'number'"
                            :id="`setting-${setting.key}`"
                            v-model.number="localSettings.settings[setting.key]"
                            type="number"
                            class="form-input"
                            :placeholder="setting.defaultValue?.toString() || ''"
                            :required="setting.required"
                        />
                        <label
                            v-else-if="setting.type === 'boolean'"
                            class="checkbox-label"
                        >
                            <input
                                :id="`setting-${setting.key}`"
                                v-model="localSettings.settings[setting.key]"
                                type="checkbox"
                                class="form-checkbox"
                            />
                            <span class="checkbox-text">{{ setting.label }}</span>
                        </label>
                        <select
                            v-else-if="setting.type === 'select'"
                            :id="`setting-${setting.key}`"
                            v-model="localSettings.settings[setting.key]"
                            class="form-select"
                            :required="setting.required"
                        >
                            <option value="" disabled>
                                Select {{ setting.label.toLowerCase() }}
                            </option>
                            <option
                                v-for="option in setting.options"
                                :key="option.value"
                                :value="option.value"
                            >
                                {{ option.label }}
                            </option>
                        </select>
                        <div
                            v-else-if="setting.type === 'date_range'"
                            class="date-range"
                        >
                            <input
                                :id="`setting-${setting.key}-from`"
                                v-model="localSettings.settings[`${setting.key}_from`]"
                                type="date"
                                class="form-input"
                                placeholder="From"
                            />
                            <span class="date-separator">to</span>
                            <input
                                :id="`setting-${setting.key}-to`"
                                v-model="localSettings.settings[`${setting.key}_to`]"
                                type="date"
                                class="form-input"
                                placeholder="To"
                            />
                        </div>
                        <div
                            v-if="validationErrors[setting.key]"
                            class="validation-error"
                        >
                            {{ validationErrors[setting.key] }}
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <template #footer>
            <div class="modal-actions">
                <Button @click="resetToDefaults" variant="secondary">
                    Reset to Defaults
                </Button>
                <Button @click="$emit('close')" variant="secondary">
                    Cancel
                </Button>
                <Button
                    @click="saveSettings"
                    :disabled="!isValid"
                    variant="primary"
                >
                    Save Settings
                </Button>
            </div>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { computed, reactive, onMounted } from "vue";
import ModalWindow from "@/components/common/modal/ModalWindow.vue";
import Button from "@/components/common/Button.vue";
import type {
    WidgetInstanceDto,
    WidgetDefinitionDto,
    WidgetSettingDto,
} from "@/types/dashboard/dashboard";

interface Props {
    widget: WidgetInstanceDto;
    widgetDefinition?: WidgetDefinitionDto;
    maxWidth?: number;
}

interface Emits {
    (e: "save", payload: { settings: Record<string, any> }): void;
    (e: "close"): void;
}

const props = withDefaults(defineProps<Props>(), {
    maxWidth: 12,
});

const emit = defineEmits<Emits>();

const localSettings = reactive({
    title: props.widget.title,
    isVisible: props.widget.isVisible,
    gridWidth: props.widget.gridWidth,
    gridHeight: props.widget.gridHeight,
    settings: { ...props.widget.settings },
});

const validationErrors = reactive<Record<string, string>>({});

const isValid = computed(() => {
    if (!props.widgetDefinition?.availableSettings) return true;
    return props.widgetDefinition.availableSettings.every((setting) => {
        if (!setting.required) return true;
        const value = localSettings.settings[setting.key];
        return value !== undefined && value !== null && value !== "";
    });
});

const validateSetting = (setting: WidgetSettingDto): string | null => {
    const value = localSettings.settings[setting.key];
    if (
        setting.required &&
        (value === undefined || value === null || value === "")
    ) {
        return `${setting.label} is required`;
    }
    if (setting.validationRegex && value && typeof value === "string") {
        const regex = new RegExp(setting.validationRegex);
        if (!regex.test(value)) {
            return `${setting.label} format is invalid`;
        }
    }
    return null;
};

const validateAllSettings = () => {
    Object.keys(validationErrors).forEach((key) => {
        delete validationErrors[key];
    });
    if (!props.widgetDefinition?.availableSettings) return;
    props.widgetDefinition.availableSettings.forEach((setting) => {
        const error = validateSetting(setting);
        if (error) {
            validationErrors[setting.key] = error;
        }
    });
};

const resetToDefaults = () => {
    localSettings.title = props.widgetDefinition?.title || props.widget.title;
    localSettings.isVisible = true;
    localSettings.gridWidth = props.widgetDefinition?.defaultWidth || 6;
    localSettings.gridHeight = props.widgetDefinition?.defaultHeight || 4;
    if (props.widgetDefinition?.availableSettings) {
        props.widgetDefinition.availableSettings.forEach((setting) => {
            if (setting.defaultValue !== undefined) {
                localSettings.settings[setting.key] = setting.defaultValue;
            } else {
                delete localSettings.settings[setting.key];
            }
        });
    }
    validateAllSettings();
};

const saveSettings = () => {
    validateAllSettings();
    if (!isValid.value) {
        return;
    }
    const updatedSettings = {
        title: localSettings.title,
        isVisible: localSettings.isVisible,
        gridWidth: localSettings.gridWidth,
        gridHeight: localSettings.gridHeight,
        settings: { ...localSettings.settings },
    };
    emit("save", { settings: updatedSettings });
};

onMounted(() => {
    if (props.widgetDefinition?.availableSettings) {
        props.widgetDefinition.availableSettings.forEach((setting) => {
            if (
                !(setting.key in localSettings.settings) &&
                setting.defaultValue !== undefined
            ) {
                localSettings.settings[setting.key] = setting.defaultValue;
            }
        });
    }
    validateAllSettings();
});
</script>

<style scoped>
.widget-settings-modal {
    max-height: 70vh;
    overflow-y: auto;
}
.settings-form {
    display: flex;
    flex-direction: column;
    gap: 2rem;
}
.form-section {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
.form-section h4 {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-gray-800);
    border-bottom: 1px solid var(--color-gray-300);
    padding-bottom: 0.5rem;
}
.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.form-group label {
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--color-gray-800);
}
.required {
    color: var(--color-error);
    margin-left: 0.125rem;
}
.help-text {
    margin: 0;
    font-size: 0.75rem;
    color: var(--color-gray-600);
    line-height: 1.4;
}
.form-input,
.form-select {
    padding: 0.75rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 0.375rem;
    font-size: 0.875rem;
    background: var(--color-white);
    color: var(--color-gray-800);
}
.form-input:focus,
.form-select:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 0.125rem rgba(0, 123, 255, 0.2);
}
.form-input:invalid {
    border-color: var(--color-error);
}
.checkbox-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
    font-size: 0.875rem;
}
.form-checkbox {
    width: 1rem;
    height: 1rem;
    cursor: pointer;
}
.date-range {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}
.date-separator {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}
.validation-error {
    font-size: 0.75rem;
    color: var(--color-error);
    margin-top: 0.25rem;
}
.modal-actions {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
}
@media (max-width: 768px) {
    .date-range {
        flex-direction: column;
        align-items: stretch;
    }
    .date-separator {
        text-align: center;
    }
    .modal-actions {
        flex-direction: column-reverse;
    }
    .modal-actions :deep(.btn) {
        width: 100%;
    }
}
</style>
