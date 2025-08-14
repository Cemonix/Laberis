<template>
    <div class="explorer-container">
        <!-- Permission check for data explorer access (Viewer and Manager only) -->
        <div v-if="!canAccessDataExplorer" class="access-denied-container">
            <div class="access-denied">
                <h2>Access Denied</h2>
                <p>You don't have permission to access the data explorer. This section is only available to viewers and managers.</p>
                <Button variant="secondary" @click="navigateToProject">Back to Project</Button>
            </div>
        </div>
        
        <template v-else>
            <header class="explorer-header">
            <div class="header-left">
                <nav class="breadcrumbs">
                    <Button 
                        variant="secondary" 
                        @click="navigateToProject"
                        class="home-button"
                        aria-label="Back to Project"
                    >
                        <font-awesome-icon :icon="faHome" />
                    </Button>
                    <span class="current-source-name">{{ dataSourceName }}</span>
                </nav>
            </div>
            <div class="header-right">
                <Button variant="secondary" @click="refreshAssets" :disabled="isLoading">
                    Refresh
                </Button>
                <Button 
                    v-permission="{ permission: PERMISSIONS.DATA_SOURCE.UPDATE }"
                    variant="primary" 
                    @click="openUploadModal"
                >
                    Upload Assets
                </Button>
            </div>
        </header>

        <main class="explorer-main-content">
            <div v-if="isLoading" class="loading-container">
                <div class="loading-message">Loading assets...</div>
            </div>
            
            <div v-else-if="error" class="error-container">
                <div class="error-message">{{ error }}</div>
                <Button variant="secondary" @click="loadAssets">Try Again</Button>
            </div>
            
            <div v-else class="content-container">
                <div class="assets-header">
                    <div class="assets-count">
                        {{ paginationInfo.totalItems }} asset{{ paginationInfo.totalItems !== 1 ? 's' : '' }}
                    </div>
                    <div class="view-controls">
                        <div class="filter-controls">
                            <!-- TODO: Add more advanced filtering options (date range, file type, size) -->
                            <select v-model="filterStatus" @change="handleFilterChange" class="filter-select">
                                <option value="">All Statuses</option>
                                <option v-for="status in assetStatuses" :key="status.value" :value="status.value">
                                    {{ status.text }}
                                </option>
                            </select>
                            <!-- TODO: Add autocomplete functionality for filename search -->
                            <input 
                                v-model="searchQuery" 
                                @input="handleSearchChange"
                                type="text" 
                                placeholder="Search files..." 
                                class="search-input"
                            >
                            <!-- TODO: Add sort direction toggle and column selector -->
                            <Button 
                                v-if="filterStatus || searchQuery"
                                variant="secondary"
                                @click="clearFilters"
                                class="clear-filters-button"
                            >
                                Clear Filters
                            </Button>
                        </div>
                    </div>
                </div>

                <!-- Content area that changes based on asset state -->
                <div v-if="assets.length === 0" class="empty-state">
                    <div v-if="hasActiveFilters" class="no-results-message">
                        <h3>No assets match your filters</h3>
                        <p>Try adjusting your search criteria or clearing the filters.</p>
                        <Button variant="secondary" @click="clearFilters">
                            Clear All Filters
                        </Button>
                    </div>
                    <div v-else class="no-assets-message">
                        <h3>No assets found</h3>
                        <p>This data source doesn't contain any assets yet.</p>
                        <Button variant="primary" @click="openUploadModal">
                            Upload Your First Asset
                        </Button>
                    </div>
                </div>
                
                <div v-else class="assets-content">
                    <div class="asset-grid">
                        <AssetThumbnail 
                            v-for="asset in assets" 
                            :key="asset.id"
                            :asset="asset"
                        />
                    </div>
                    
                    <div v-if="hasMoreAssets" class="load-more-container">
                        <Button 
                            variant="secondary" 
                            @click="loadMoreAssets"
                            :disabled="isLoadingMore"
                        >
                            {{ isLoadingMore ? 'Loading...' : 'Load More' }}
                        </Button>
                    </div>
                </div>
            </div>
        </main>

            <UploadImagesModal
                v-if="currentDataSource"
                v-model:isOpen="isUploadModalOpen"
                :dataSource="currentDataSource"
                @upload-complete="handleUploadComplete"
            />
        </template>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faHome} from '@fortawesome/free-solid-svg-icons';
import {type Asset, type AssetListParams} from '@/types/asset';
import {AssetStatus} from '@/types/asset';
import type {DataSource} from '@/types/dataSource';
import AssetThumbnail from '@/components/project/AssetThumbnail.vue';
import Button from '@/components/common/Button.vue';
import UploadImagesModal from '@/components/project/UploadImagesModal.vue';
import {assetService, dataSourceService} from '@/services/api/projects';
import {useAlert} from '@/composables/useAlert';
import {AppLogger} from '@/utils/logger';
import {usePermissions} from '@/composables/usePermissions';
import {PERMISSIONS} from '@/types/permissions';

const logger = AppLogger.createComponentLogger('DataExplorerView');
const route = useRoute();
const router = useRouter();
const { showAlert } = useAlert();
const { hasProjectPermission } = usePermissions();

const projectId = Number(route.params.projectId);
const dataSourceId = Number(route.params.dataSourceId);

// Reactive state
const dataSourceName = ref('Loading...');
const currentDataSource = ref<DataSource | null>(null);
const assets = ref<Asset[]>([]);
const isLoading = ref(false);
const isLoadingMore = ref(false);
const error = ref<string | null>(null);
const isUploadModalOpen = ref(false);
const filterStatus = ref('');
const searchQuery = ref('');
const searchTimeout = ref<NodeJS.Timeout | null>(null);

const assetStatuses = ref([
    { value: AssetStatus.PENDING_IMPORT, text: 'Pending Import' },
    { value: AssetStatus.IMPORTED, text: 'Imported' },
    { value: AssetStatus.IMPORT_ERROR, text: 'Import Error' },
    { value: AssetStatus.PENDING_PROCESSING, text: 'Pending Processing' },
    { value: AssetStatus.PROCESSING, text: 'Processing' },
    { value: AssetStatus.PROCESSING_ERROR, text: 'Processing Error' },
    { value: AssetStatus.READY_FOR_ANNOTATION, text: 'Ready for Annotation' },
    { value: AssetStatus.ANNOTATION_IN_PROGRESS, text: 'Annotation in Progress' },
    { value: AssetStatus.ANNOTATED, text: 'Annotated' },
    { value: AssetStatus.PENDING_REVIEW, text: 'Pending Review' },
    { value: AssetStatus.REVIEW_IN_PROGRESS, text: 'Review in Progress' },
    { value: AssetStatus.REVIEW_ACCEPTED, text: 'Review Accepted' },
    { value: AssetStatus.REVIEW_REJECTED, text: 'Review Rejected' },
    { value: AssetStatus.EXPORTED, text: 'Exported' },
    { value: AssetStatus.ARCHIVED, text: 'Archived' }
]);

// Pagination state
const currentPage = ref(1);
const pageSize = ref(25);
const paginationInfo = ref({
    totalItems: 0,
    totalPages: 0,
    currentPage: 1
});

// Computed properties
const hasMoreAssets = computed(() => 
    paginationInfo.value.currentPage < paginationInfo.value.totalPages
);

const hasActiveFilters = computed(() => 
    Boolean(filterStatus.value || (searchQuery.value && searchQuery.value.trim()))
);

const canAccessDataExplorer = computed(() => 
    hasProjectPermission(PERMISSIONS.DATA_EXPLORER.READ)
);

// Methods
const navigateToProject = () => {
    router.push(`/projects/${projectId}/data-sources`);
};

const loadDataSource = async () => {
    try {
        const dataSource = await dataSourceService.getDataSource(projectId, dataSourceId);
        currentDataSource.value = dataSource;
        dataSourceName.value = dataSource.name;
        logger.info(`Loaded data source: ${dataSource.name}`);
    } catch (err) {
        logger.error('Failed to load data source', err);
        dataSourceName.value = 'Unknown Data Source';
        await showAlert('Error', 'Failed to load data source information.');
    }
};

const loadAssets = async (page: number = 1, append: boolean = false) => {
    if (!append) {
        isLoading.value = true;
        error.value = null;
    } else {
        isLoadingMore.value = true;
    }

    try {
        const params: AssetListParams = {
            pageNumber: page,
            pageSize: pageSize.value,
            dataSourceId: dataSourceId,
            sortBy: 'createdAt',
            isAscending: false // desc order for newest first
        };

        // Add filter parameters only if they have values
        if (filterStatus.value) {
            params.filterOn = 'status';
            params.filterQuery = filterStatus.value;
        } else if (searchQuery.value && searchQuery.value.trim()) {
            params.filterOn = 'filename';
            params.filterQuery = searchQuery.value.trim();
        }

        const response = await assetService.getAssets(projectId, params);

        // TODO: Implement empty response handling with user-friendly messages

        if (append) {
            assets.value.push(...response.data);
        } else {
            assets.value = response.data;
        }

        paginationInfo.value = {
            totalItems: response.totalItems,
            totalPages: response.totalPages,
            currentPage: response.currentPage
        };

        currentPage.value = response.currentPage;
        
        logger.info(`Loaded ${response.data.length} assets (page ${page})`);
    } catch (err) {
        logger.error('Failed to load assets', err);
        error.value = 'Failed to load assets. Please try again.';
        
        if (!append) {
            await showAlert('Error', 'Failed to load assets. Please try again.');
        }
    } finally {
        isLoading.value = false;
        isLoadingMore.value = false;
    }
};

const loadMoreAssets = async () => {
    if (hasMoreAssets.value && !isLoadingMore.value) {
        await loadAssets(currentPage.value + 1, true);
    }
};

const refreshAssets = async () => {
    currentPage.value = 1;
    await loadAssets(1, false);
};

const openUploadModal = () => {
    isUploadModalOpen.value = true;
};

const handleUploadComplete = async (count: number) => {
    isUploadModalOpen.value = false;
    logger.info(`Upload completed: ${count} assets`);
    await showAlert('Success', `Successfully uploaded ${count} asset${count !== 1 ? 's' : ''}`);
    // Refresh assets to show newly uploaded ones
    await refreshAssets();
};

const handleFilterChange = () => {
    currentPage.value = 1;
    loadAssets(1, false);
};

const handleSearchChange = () => {
    // Debounce search to avoid too many API calls
    if (searchTimeout.value) {
        clearTimeout(searchTimeout.value);
    }
    searchTimeout.value = setTimeout(() => {
        currentPage.value = 1;
        loadAssets(1, false);
    }, 500);
};

const clearFilters = () => {
    filterStatus.value = '';
    searchQuery.value = '';
    currentPage.value = 1;
    loadAssets(1, false);
};

// Lifecycle
onMounted(async () => {
    await Promise.all([
        loadDataSource(),
        loadAssets()
    ]);
});
</script>

<style scoped>
.explorer-container {
    display: flex;
    flex-direction: column;
    height: 100vh;
    background-color: var(--color-gray-100);
}

.explorer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    background-color: var(--color-white);
    border-bottom: 1px solid var(--color-gray-400);
    flex-shrink: 0;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.breadcrumbs {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 1.25rem;
}

.breadcrumbs .home-button {
    min-width: 32px;
    height: 32px;
    padding: 0;
    margin-right: 0.5rem;
    font-size: 1.2rem;
    font-weight: bold;
}

.breadcrumbs a {
    color: var(--color-primary);
    text-decoration: none;
}

.breadcrumbs a:hover {
    text-decoration: underline;
}

.breadcrumbs .separator {
    color: var(--color-gray-600);
    margin: 0 0.25rem;
}

.breadcrumbs .current-source-name {
    color: var(--color-gray-800);
    font-weight: 600;
}

.header-right {
    display: flex;
    gap: 1rem;
}

.explorer-main-content {
    flex-grow: 1;
    overflow-y: auto;
    padding: 1.5rem;
}

.loading-container,
.error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 50vh;
    gap: 1rem;
}

.loading-message {
    font-size: 1.25rem;
    color: var(--color-gray-600);
}

.error-message {
    font-size: 1.25rem;
    color: var(--color-error);
    text-align: center;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 50vh;
    gap: 1.5rem;
    text-align: center;
}

.no-results-message,
.no-assets-message {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
}

.empty-state h3 {
    font-size: 1.5rem;
    color: var(--color-gray-800);
    margin: 0;
}

.empty-state p {
    font-size: 1.25rem;
    color: var(--color-gray-600);
    margin: 0;
    max-width: 50ch;
}

.assets-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    padding-bottom: 1rem;
    border-bottom: 1px solid var(--color-gray-400);
}

.assets-count {
    font-size: 1.25rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.view-controls {
    display: flex;
    gap: 1rem;
    align-items: center;
}

.filter-controls {
    display: flex;
    gap: 0.5rem;
    align-items: center;
}

.filter-select,
.search-input {
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 0.375rem;
    font-size: 0.875rem;
    background-color: var(--color-white);
}

.filter-select {
    min-width: 160px;
}

.search-input {
    min-width: 200px;
}

.filter-select:focus,
.search-input:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 1px var(--color-primary);
}

.clear-filters-button {
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    white-space: nowrap;
    margin-left: 0.5rem;
}

.asset-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2rem;
}

.load-more-container {
    display: flex;
    justify-content: center;
    padding: 1.5rem 0;
}

@media (max-width: 768px) {
    .explorer-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
    }

    .breadcrumbs {
        font-size: 1rem;
    }

    .header-right {
        justify-content: center;
    }

    .assets-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
    }

    .filter-controls {
        flex-direction: column;
        gap: 0.75rem;
    }

    .filter-select,
    .search-input {
        min-width: auto;
        width: 100%;
    }

    .asset-grid {
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1rem;
    }

    .explorer-main-content {
        padding: 1rem;
    }
}

.access-denied-container {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    padding: 2rem;
}

.access-denied {
    text-align: center;
    max-width: 500px;
    padding: 2rem;
    background-color: var(--color-white);
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    
    h2 {
        color: var(--color-error);
        margin-bottom: 1rem;
        font-size: 1.5rem;
    }
    
    p {
        color: var(--color-gray-600);
        margin-bottom: 2rem;
        line-height: 1.5;
    }
}
</style>