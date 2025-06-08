<template>
    <Form @submit="handleSubmit">
        <div class="form-group">
            <label for="dataSourceName">Data Source Name</label>
            <input id="dataSourceName" v-model="formData.name" type="text" required placeholder="e.g., training-images-q1" />
        </div>
        <div class="form-group">
            <label for="dataSourceDescription">Description (Optional)</label>
            <textarea id="dataSourceDescription" v-model="formData.description" rows="3" placeholder="A short description of this data source."></textarea>
        </div>
        <div class="form-group">
            <label for="dataSourceType">Data Source Type</label>
            <select id="dataSourceType" v-model="formData.type" required>
                <option v-for="type in dataSourceTypes" :key="type" :value="type">
                    {{ type.replace('_', ' ') }}
                </option>
            </select>
        </div>
        <div class="form-actions">
            <Button type="button" @click="$emit('cancel')" variant="secondary">Cancel</Button>
            <Button type="submit" variant="primary">Create Data Source</Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import { DataSourceType, type FormPayloadDataSource } from '@/types/project/dataSource';
import { useAlert } from '@/composables/useAlert';

const { showAlert } = useAlert();

const emit = defineEmits<{
    (e: 'save', formData: FormPayloadDataSource): void;
    (e: 'cancel'): void;
}>();

const dataSourceTypes = Object.values(DataSourceType);

const formData = ref<FormPayloadDataSource>({
    name: '',
    description: '',
    type: DataSourceType.S3_COMPATIBLE,
});

const handleSubmit = async () => {
    if (!formData.value.name) {
        await showAlert('Missing Information', 'Data Source Name is required.');
        return;
    }
    emit('save', { ...formData.value });
};
</script>