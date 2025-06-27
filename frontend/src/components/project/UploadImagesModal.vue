<template>
    <ModalWindow :isOpen="isOpen" title="Upload Images" @close="handleModalClose">
        <div class="upload-container">
            <!-- Upload Type Selection -->
            <div class="upload-type-section">
                <h4>Upload Type</h4>
                <div class="upload-type-options">
                    <label class="upload-option">
                        <input 
                            type="radio" 
                            v-model="uploadType" 
                            value="files"
                            name="uploadType"
                        >
                        <span>Select Individual Images</span>
                    </label>
                    <label class="upload-option">
                        <input 
                            type="radio" 
                            v-model="uploadType" 
                            value="folder"
                            name="uploadType"
                        >
                        <span>Upload Folder</span>
                    </label>
                </div>
            </div>

            <!-- File Selection Area -->
            <div class="file-selection-section">
                <div 
                    class="dropzone"
                    :class="{ 'dragover': isDragOver, 'has-files': selectedFiles.length > 0 }"
                    @dragover.prevent="handleDragOver"
                    @dragleave.prevent="handleDragLeave"
                    @drop.prevent="handleDrop"
                    @click="triggerFileInput"
                >
                    <input
                        ref="fileInput"
                        type="file"
                        :multiple="uploadType === 'files'"
                        :webkitdirectory="uploadType === 'folder'"
                        accept="image/*"
                        @change="handleFileSelection"
                        class="hidden-input"
                    >
                    
                    <div v-if="selectedFiles.length === 0" class="dropzone-content">
                        <svg class="upload-icon" width="48" height="48" viewBox="0 0 24 24">
                            <path fill="currentColor" d="M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z" />
                        </svg>
                        <p class="dropzone-text">
                            <span v-if="uploadType === 'files'">
                                Drag and drop images here, or click to select files
                            </span>
                            <span v-else>
                                Click to select a folder containing images
                            </span>
                        </p>
                        <p class="dropzone-subtext">
                            Supports: JPG, PNG, GIF, WebP, BMP
                        </p>
                    </div>

                    <div v-else class="selected-files">
                        <div class="files-header">
                            <h5>Selected Files ({{ selectedFiles.length }})</h5>
                            <Button variant="secondary" size="small" @click="clearSelection">
                                Clear All
                            </Button>
                        </div>
                        <div class="files-list">
                            <div 
                                v-for="(file, index) in selectedFiles" 
                                :key="index"
                                class="file-item"
                            >
                                <div class="file-preview">
                                    <img 
                                        v-if="file.type.startsWith('image/')"
                                        :src="getFilePreview(file)"
                                        :alt="file.name"
                                        class="file-thumbnail"
                                    >
                                    <div v-else class="file-icon">
                                        <svg width="24" height="24" viewBox="0 0 24 24">
                                            <path fill="currentColor" d="M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z" />
                                        </svg>
                                    </div>
                                </div>
                                <div class="file-info">
                                    <span class="file-name">{{ file.name }}</span>
                                    <span class="file-size">{{ formatFileSize(file.size) }}</span>
                                </div>
                                <Button 
                                    variant="secondary" 
                                    size="small"
                                    @click.stop="removeFile(index)"
                                    title="Remove file"
                                >
                                    ×
                                </Button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Upload Progress -->
            <div v-if="isUploading" class="upload-progress-section">
                <h4>Upload Progress</h4>
                <div class="progress-bar">
                    <div 
                        class="progress-fill" 
                        :style="{ width: `${uploadProgress}%` }"
                    ></div>
                </div>
                <p class="progress-text">
                    Uploading {{ currentFileIndex }} of {{ selectedFiles.length }} files...
                </p>
            </div>

            <!-- Error Display -->
            <div v-if="uploadErrors.length > 0" class="upload-errors-section">
                <h4>Upload Errors</h4>
                <div class="error-list">
                    <div 
                        v-for="(error, index) in uploadErrors" 
                        :key="index"
                        class="error-item"
                    >
                        <span class="error-file">{{ error.fileName }}:</span>
                        <span class="error-message">{{ error.message }}</span>
                    </div>
                </div>
            </div>
        </div>

        <template #footer>
            <div class="modal-actions">
                <Button 
                    variant="secondary" 
                    @click="handleModalClose"
                    :disabled="isUploading"
                >
                    Cancel
                </Button>
                <Button 
                    variant="primary" 
                    @click="startUpload"
                    :disabled="selectedFiles.length === 0 || isUploading"
                    :loading="isUploading"
                >
                    Upload {{ selectedFiles.length }} {{ selectedFiles.length === 1 ? 'Image' : 'Images' }}
                </Button>
            </div>
        </template>
        
        <!-- Alert Modal -->
        <AlertModal 
            :is-open="isAlertOpen"
            :title="alertTitle"
            :message="alertMessage"
            @confirm="handleAlertConfirm"
        />
    </ModalWindow>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import AlertModal from '@/components/common/modal/AlertModal.vue';
import Button from '@/components/common/Button.vue';
import type { DataSource } from '@/types/dataSource/dataSource';
import { NoFilesProvidedError, UploadError } from '@/types/asset';
import { ApiResponseError, ServerError, NetworkError } from '@/types/common/errors';
import { AppLogger } from '@/utils/logger';
import { useAlert } from '@/composables/useAlert';
import assetService from '@/services/api/assetService';

const logger = AppLogger.createServiceLogger('UploadImagesModal');

// Use alert composable
const { isAlertOpen, alertTitle, alertMessage, showAlert, handleAlertConfirm } = useAlert();

const props = defineProps<{
    dataSource: DataSource;
    isOpen: boolean;
}>();

const emit = defineEmits<{
    'update:isOpen': [value: boolean];
    'upload-complete': [count: number];
}>();

// Upload state
const uploadType = ref<'files' | 'folder'>('files');
const selectedFiles = ref<File[]>([]);
const isDragOver = ref(false);
const isUploading = ref(false);
const uploadProgress = ref(0);
const currentFileIndex = ref(0);
const uploadErrors = ref<Array<{ fileName: string; message: string }>>([]);

// File input reference
const fileInput = ref<HTMLInputElement>();

// Computed properties
const isOpen = computed({
    get: () => props.isOpen,
    set: (value) => emit('update:isOpen', value)
});

// File handling methods
const triggerFileInput = () => {
    fileInput.value?.click();
};

// Helper function to add files with duplicate checking
const addFilesToSelection = (newImageFiles: File[]) => {
    if (uploadType.value === 'files') {
        // For individual files, check for duplicates and add only new ones
        const newFiles: File[] = [];
        const duplicateFiles: string[] = [];
        
        newImageFiles.forEach(file => {
            const isDuplicate = selectedFiles.value.some(existingFile => 
                existingFile.name === file.name && 
                existingFile.size === file.size
            );
            
            if (isDuplicate) {
                duplicateFiles.push(file.name);
            } else {
                newFiles.push(file);
            }
        });
        
        // Add only new files to the selection
        selectedFiles.value = [...selectedFiles.value, ...newFiles];
        
        // Show alert if duplicates were found
        if (duplicateFiles.length > 0) {
            const title = duplicateFiles.length === 1 ? 'Duplicate File' : 'Duplicate Files';
            const message = duplicateFiles.length === 1 
                ? `"${duplicateFiles[0]}" is already selected.`
                : `${duplicateFiles.length} files are already selected:\n${duplicateFiles.join(', ')}`;
            
            showAlert(title, message);
            logger.warn(`${duplicateFiles.length} file(s) already selected: ${duplicateFiles.join(', ')}`);
        }
        
        // Log info about new files added
        if (newFiles.length > 0) {
            logger.info(`Added ${newFiles.length} new file(s) to selection`);
        }
    } else {
        // For folder upload, replace selection
        selectedFiles.value = newImageFiles;
    }
};

const handleFileSelection = (event: Event) => {
    const target = event.target as HTMLInputElement;
    if (target.files && target.files.length > 0) {
        const files = Array.from(target.files);
        const imageFiles = files.filter(file => file.type.startsWith('image/'));
        
        // Use helper function to add files with duplicate checking
        addFilesToSelection(imageFiles);
        
        // Show alert for non-image files that were filtered out
        if (imageFiles.length !== files.length) {
            const nonImageCount = files.length - imageFiles.length;
            const title = nonImageCount === 1 ? 'Non-Image File' : 'Non-Image Files';
            const message = nonImageCount === 1 
                ? 'One file was skipped because it is not an image.'
                : `${nonImageCount} files were skipped because they are not images.`;
            
            showAlert(title, message);
            logger.warn(`Filtered out ${nonImageCount} non-image files`);
        }
    }
    // Clear the input value so the same file can be selected again
    // and to prevent cancelled selections from affecting future selections
    target.value = '';
};

const handleDragOver = () => {
    isDragOver.value = true;
};

const handleDragLeave = () => {
    isDragOver.value = false;
};

const handleDrop = (event: DragEvent) => {
    isDragOver.value = false;
    
    if (event.dataTransfer?.files) {
        const files = Array.from(event.dataTransfer.files);
        const imageFiles = files.filter(file => file.type.startsWith('image/'));
        
        // Use helper function to add files with duplicate checking
        addFilesToSelection(imageFiles);
        
        // Show alert for non-image files that were filtered out
        if (imageFiles.length !== files.length) {
            const nonImageCount = files.length - imageFiles.length;
            const title = nonImageCount === 1 ? 'Non-Image File' : 'Non-Image Files';
            const message = nonImageCount === 1 
                ? 'One file was skipped because it is not an image.'
                : `${nonImageCount} files were skipped because they are not images.`;
            
            showAlert(title, message);
            logger.warn(`Filtered out ${nonImageCount} non-image files`);
        }
    }
};

const removeFile = (index: number) => {
    selectedFiles.value.splice(index, 1);
};

const clearSelection = () => {
    selectedFiles.value = [];
    if (fileInput.value) {
        fileInput.value.value = '';
    }
};

const getFilePreview = (file: File): string => {
    return URL.createObjectURL(file);
};

const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

const handleUploadError = (error: unknown) => {
    logger.error('Upload process failed', error);
    
    if (error instanceof NoFilesProvidedError) {
        showAlert('No Files Selected', 'Please select at least one image file to upload.');
        return;
    }

    if (error instanceof ServerError) {
        const statusText = error.statusCode ? ` (${error.statusCode})` : '';
        showAlert('Server Error', `The server encountered an error during upload${statusText}. Please try again.`);
        return;
    }

    if (error instanceof NetworkError) {
        showAlert('Network Error', 'A network error occurred during upload. Please check your connection and try again.');
        return;
    }

    if (error instanceof ApiResponseError) {
        showAlert('Response Error', 'The server response was invalid. Please try again or contact support.');
        return;
    }
    
    if (error instanceof UploadError) {
        showAlert('Upload Error', error.message || 'An upload error occurred. Please try again.');
        return;
    }
    
    // Default fallback for any other error
    showAlert('Upload Failed', 'The upload process failed unexpectedly. Please try again.');
};

const startUpload = async () => {
    if (selectedFiles.value.length === 0) {
        logger.warn('Upload attempted with no files selected');
        showAlert('No Files Selected', 'Please select at least one image file to upload.');
        return;
    }
    
    isUploading.value = true;
    uploadProgress.value = 0;
    currentFileIndex.value = 0;
    uploadErrors.value = [];
    
    try {
        logger.info(`Starting upload of ${selectedFiles.value.length} files to data source ${props.dataSource.id}`);
        
        // Use bulk upload for better performance and user experience
        const result = await assetService.uploadAssets(
            props.dataSource.projectId,
            props.dataSource.id,
            selectedFiles.value,
            undefined, // TODO: No additional metadata for now
            (progress) => {
                uploadProgress.value = progress;
            }
        );
        
        // Process results
        const failedUploads = result.results.filter(r => !r.success);
        uploadErrors.value = failedUploads.map(r => ({
            fileName: r.filename,
            message: r.errorMessage || 'Unknown error'
        }));
        
        logger.info(`Upload completed. ${result.successfulUploads} files uploaded successfully, ${result.failedUploads} errors`);
        
        if (result.allSuccessful) {
            const title = 'Upload Successful';
            const message = result.successfulUploads === 1 
                ? '1 image was uploaded successfully.'
                : `${result.successfulUploads} images were uploaded successfully.`;
            
            showAlert(title, message);
            
            emit('upload-complete', result.successfulUploads);
            closeModal();
        } else {
            const title = 'Upload Completed with Errors';
            const message = `${result.successfulUploads} images uploaded successfully.\n${result.failedUploads} images failed to upload.\n\nError details:\n${uploadErrors.value.map(e => `${e.fileName}: ${e.message}`).join('\n')}`;
            
            showAlert(title, message);
            
            // Still emit the successful count for refreshing the UI
            emit('upload-complete', result.successfulUploads);
        }
        
    } catch (error) {
        handleUploadError(error);
    } finally {
        isUploading.value = false;
    }
};

const handleModalClose = () => {
    if (!isUploading.value) {
        closeModal();
    }
};

const closeModal = () => {
    // Clear all state
    selectedFiles.value = [];
    uploadErrors.value = [];
    uploadProgress.value = 0;
    currentFileIndex.value = 0;
    if (fileInput.value) {
        fileInput.value.value = '';
    }
    // Close the modal
    emit('update:isOpen', false);
};
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.upload-container {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-large;
}

.upload-type-section {
    h4 {
        margin: 0 0 vars.$gap-medium 0;
        color: vars.$theme-text;
    }
}

.upload-type-options {
    display: flex;
    gap: vars.$gap-large;
}

.upload-option {
    display: flex;
    align-items: center;
    gap: vars.$gap-small;
    cursor: pointer;
    
    input[type="radio"] {
        accent-color: vars.$color-primary;
    }
    
    span {
        font-weight: 500;
    }
}

.file-selection-section {
    .dropzone {
        border: 2px dashed vars.$color-gray-300;
        border-radius: vars.$border-radius-lg;
        padding: vars.$gap-large;
        text-align: center;
        cursor: pointer;
        transition: all 0.3s ease;
        min-height: 300px;
        display: flex;
        align-items: center;
        justify-content: center;
        
        &:hover, &.dragover {
            border-color: vars.$color-primary;
            background-color: vars.$color-gray-100;
        }
        
        &.has-files {
            padding: vars.$gap-medium;
            text-align: left;
            align-items: stretch;
        }
    }
    
    .hidden-input {
        display: none;
    }
    
    .dropzone-content {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: vars.$gap-medium;
        
        .upload-icon {
            color: vars.$color-gray-400;
        }
        
        .dropzone-text {
            font-size: vars.$font-size-large;
            font-weight: 500;
            color: vars.$theme-text;
            margin: 0;
        }
        
        .dropzone-subtext {
            font-size: vars.$font-size-small;
            color: vars.$theme-text-light;
            margin: 0;
        }
    }
}

.selected-files {
    width: 100%;
    
    .files-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: vars.$gap-medium;
        
        h5 {
            margin: 0;
            color: vars.$theme-text;
        }
    }
    
    .files-list {
        max-height: 300px;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: vars.$gap-small;
    }
    
    .file-item {
        display: flex;
        align-items: center;
        gap: vars.$gap-medium;
        padding: vars.$gap-small;
        border: 1px solid vars.$color-gray-200;
        border-radius: vars.$border-radius-md;
        background-color: vars.$color-gray-100;
        
        .file-preview {
            flex-shrink: 0;
            
            .file-thumbnail {
                width: 48px;
                height: 48px;
                object-fit: cover;
                border-radius: vars.$border-radius-sm;
            }
            
            .file-icon {
                width: 48px;
                height: 48px;
                display: flex;
                align-items: center;
                justify-content: center;
                background-color: vars.$color-gray-200;
                border-radius: vars.$border-radius-sm;
                color: vars.$color-gray-900;
            }
        }
        
        .file-info {
            flex-grow: 1;
            display: flex;
            flex-direction: column;
            gap: vars.$gap-xsmall;
            
            .file-name {
                font-weight: 500;
                color: vars.$theme-text;
                word-break: break-word;
            }
            
            .file-size {
                font-size: vars.$font-size-small;
                color: vars.$theme-text-light;
            }
        }
    }
}

.upload-progress-section {
    h4 {
        margin: 0 0 vars.$gap-medium 0;
        color: vars.$theme-text;
    }
    
    .progress-bar {
        width: 100%;
        height: 8px;
        background-color: vars.$color-gray-200;
        border-radius: vars.$border-radius-sm;
        overflow: hidden;
        margin-bottom: vars.$gap-small;
        
        .progress-fill {
            height: 100%;
            background-color: vars.$color-primary;
            transition: width 0.3s ease;
        }
    }
    
    .progress-text {
        margin: 0;
        font-size: vars.$font-size-small;
        color: vars.$theme-text-light;
    }
}

.upload-errors-section {
    h4 {
        margin: 0 0 vars.$gap-medium 0;
        color: vars.$color-error;
    }
    
    .error-list {
        max-height: 200px;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: vars.$gap-small;
    }
    
    .error-item {
        padding: vars.$gap-small;
        background-color: vars.$color-gray-200;
        border: 1px solid vars.$color-error;
        border-radius: vars.$border-radius-sm;
        font-size: vars.$font-size-small;
        
        .error-file {
            font-weight: 500;
            color: vars.$color-error;
        }
        
        .error-message {
            color: vars.$theme-text;
        }
    }
}

.modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: vars.$gap-medium;
}
</style>
