<template>
    <ModalWindow
        title="Dashboard Settings"
        :isOpen="true"
        @close="$emit('close')"
        size="medium"
    >
        <div class="dashboard-settings">
            <div class="settings-form">
                <!-- Auto Refresh Settings -->
                <div class="settings-section">
                    <h4>Auto Refresh</h4>

                    <div class="form-group">
                        <label class="checkbox-label">
                            <input
                                v-model="localSettings.autoRefresh"
                                type="checkbox"
                                class="form-checkbox"
                            />
                            <span class="checkbox-text"
                                >Enable auto refresh</span
                            >
                        </label>
                    </div>

                    <div v-if="localSettings.autoRefresh" class="form-group">
                        <label for="refresh-interval">Refresh Interval</label>
                        <select
                            id="refresh-interval"
                            v-model="localSettings.refreshInterval"
                            class="form-select"
                        >
                            <option :value="60000">1 minute</option>
                            <option :value="300000">5 minutes</option>
                            <option :value="600000">10 minutes</option>
                            <option :value="1800000">30 minutes</option>
                            <option :value="3600000">1 hour</option>
                        </select>
                    </div>
                </div>

                <!-- Theme Settings -->
                <div class="settings-section">
                    <h4>Appearance</h4>

                    <div class="form-group">
                        <label for="theme">Theme</label>
                        <select
                            id="theme"
                            v-model="localSettings.theme"
                            class="form-select"
                        >
                            <option value="light">Light</option>
                            <option value="dark">Dark</option>
                            <option value="auto">Auto (System)</option>
                        </select>
                    </div>
                </div>

                <!-- Performance Settings -->
                <div class="settings-section">
                    <h4>Performance</h4>

                    <div class="form-group">
                        <label class="checkbox-label">
                            <input
                                v-model="localSettings.enableAnimations"
                                type="checkbox"
                                class="form-checkbox"
                            />
                            <span class="checkbox-text">Enable animations</span>
                        </label>
                    </div>

                    <div class="form-group">
                        <label class="checkbox-label">
                            <input
                                v-model="localSettings.enableTooltips"
                                type="checkbox"
                                class="form-checkbox"
                            />
                            <span class="checkbox-text">Show tooltips</span>
                        </label>
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
                <Button @click="saveSettings" variant="primary">
                    Save Settings
                </Button>
            </div>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { reactive } from "vue";
import ModalWindow from "@/components/common/modal/ModalWindow.vue";
import Button from "@/components/common/Button.vue";

interface Props {
    autoRefresh: boolean;
    refreshInterval: number;
    theme: string;
}

interface Emits {
    (
        e: "save",
        settings: {
            autoRefresh: boolean;
            refreshInterval: number;
            theme: string;
            enableAnimations: boolean;
            enableTooltips: boolean;
        }
    ): void;
    (e: "close"): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Local settings state
const localSettings = reactive({
    autoRefresh: props.autoRefresh,
    refreshInterval: props.refreshInterval,
    theme: props.theme,
    enableAnimations: true,
    enableTooltips: true,
});

// Methods
const resetToDefaults = () => {
    localSettings.autoRefresh = true;
    localSettings.refreshInterval = 300000; // 5 minutes
    localSettings.theme = "light";
    localSettings.enableAnimations = true;
    localSettings.enableTooltips = true;
};

const saveSettings = () => {
    emit("save", { ...localSettings });
};
</script>

<style scoped>
.dashboard-settings {
    max-height: 70vh;
    overflow-y: auto;
}

.settings-form {
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

.settings-section {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.settings-section h4 {
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

.form-select {
    padding: 0.75rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 0.375rem;
    font-size: 0.875rem;
    background: var(--color-white);
    color: var(--color-gray-800);
}

.form-select:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 0.125rem rgba(0, 123, 255, 0.2);
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

.checkbox-text {
    user-select: none;
}

.modal-actions {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
}


/* Responsive Design */
@media (max-width: 768px) {
    .modal-actions {
        flex-direction: column-reverse;
    }

    .modal-actions :deep(.btn) {
        width: 100%;
    }
}
</style>
