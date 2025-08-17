<template>
    <div class="data-table">
        <div v-if="title || actions.length > 0" class="table-header">
            <h3 v-if="title" class="table-title">{{ title }}</h3>
            <div v-if="actions.length > 0" class="table-actions">
                <Button
                    v-for="action in actions"
                    :key="action.key"
                    :variant="action.variant || 'secondary'"
                    @click="$emit('action', action.key)"
                    :disabled="action.disabled"
                >
                    <font-awesome-icon v-if="action.icon" :icon="action.icon" />
                    {{ action.label }}
                </Button>
            </div>
        </div>

        <div class="table-container">
            <div v-if="isLoading" class="table-loading">
                <div class="loading-spinner"></div>
                <p>Loading {{ title?.toLowerCase() || 'data' }}...</p>
            </div>
            
            <div v-else-if="error" class="table-error">
                <font-awesome-icon :icon="faExclamationTriangle" />
                <p>{{ error }}</p>
                <Button variant="secondary" @click="$emit('refresh')">Try Again</Button>
            </div>
            
            <div v-else-if="data.length === 0" class="table-empty">
                <font-awesome-icon :icon="faInbox" />
                <h4>{{ emptyMessage || `No ${title?.toLowerCase() || 'data'} found` }}</h4>
                <p>{{ emptyDescription || 'There are no items to display at this time.' }}</p>
            </div>
            
            <table v-else class="table">
                <thead>
                    <tr>
                        <th
                            v-for="column in columns"
                            :key="column.key"
                            :class="getColumnClass(column)"
                            @click="handleSort(column)"
                        >
                            <div class="column-header">
                                <slot
                                    :name="`header-${column.key}`"
                                    :column="column"
                                >
                                    <span>{{ column.label }}</span>
                                    <font-awesome-icon
                                        v-if="column.sortable"
                                        :icon="getSortIcon(column.key)"
                                        :class="getSortIconClass(column.key)"
                                    />
                                </slot>
                            </div>
                        </th>
                        <th v-if="hasRowActions" class="actions-column">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    <tr
                        v-for="(row, index) in sortedData"
                        :key="getRowKey(row, index)"
                        :class="getRowClass(row, index)"
                        v-bind="getRowAttributes(row)"
                        @click="handleRowClick(row, index)"
                    >
                        <td
                            v-for="column in columns"
                            :key="column.key"
                            :class="getCellClass(column, row)"
                        >
                            <div class="cell-content">
                                <slot
                                    :name="`cell-${column.key}`"
                                    :value="getCellValue(row, column.key)"
                                    :row="row"
                                    :column="column"
                                    :index="index"
                                >
                                    <component
                                        v-if="column.component"
                                        :is="column.component"
                                        :value="getCellValue(row, column.key)"
                                        :row="row"
                                        :column="column"
                                    />
                                    <span v-else :class="column.cellClass">
                                        {{ formatCellValue(getCellValue(row, column.key), column) }}
                                    </span>
                                </slot>
                            </div>
                        </td>
                        <td v-if="hasRowActions" class="actions-cell">
                            <div class="row-actions">
                                <Button
                                    v-for="action in rowActions"
                                    :key="action.key"
                                    :variant="action.variant || 'secondary'"
                                    size="sm"
                                    @click.stop="handleRowAction(action.key, row, index)"
                                    :disabled="action.disabled?.(row) || false"
                                    :title="action.tooltip?.(row)"
                                >
                                    <font-awesome-icon v-if="action.icon" :icon="action.icon" />
                                    <span v-if="action.label">{{ action.label }}</span>
                                </Button>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <div v-if="pagination && data.length > 0" class="table-pagination">
            <div class="pagination-info">
                Showing {{ paginationInfo.start }}-{{ paginationInfo.end }} of {{ paginationInfo.total }} items
            </div>
            <div class="pagination-controls">
                <Button
                    variant="secondary"
                    size="sm"
                    @click="goToPage(pagination.currentPage - 1)"
                    :disabled="pagination.currentPage <= 1"
                >
                    <font-awesome-icon :icon="faChevronLeft" />
                </Button>
                <span class="page-info">
                    Page {{ pagination.currentPage }} of {{ pagination.totalPages }}
                </span>
                <Button
                    variant="secondary"
                    size="sm"
                    @click="goToPage(pagination.currentPage + 1)"
                    :disabled="pagination.currentPage >= pagination.totalPages"
                >
                    <font-awesome-icon :icon="faChevronRight" />
                </Button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts" generic="T extends Record<string, any>">
import {computed, ref} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faChevronLeft,
    faChevronRight,
    faExclamationTriangle,
    faInbox,
    faSort,
    faSortDown,
    faSortUp
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import type {TableAction, TableColumn, TablePagination, TableRowAction} from '@/types/common';

interface Props {
    data: T[];
    columns: TableColumn[];
    title?: string;
    actions?: TableAction[];
    rowActions?: TableRowAction<T>[];
    isLoading?: boolean;
    error?: string | null;
    emptyMessage?: string;
    emptyDescription?: string;
    rowKey?: string;
    clickableRows?: boolean;
    pagination?: TablePagination;
}

const props = withDefaults(defineProps<Props>(), {
    actions: () => [],
    rowActions: () => [],
    isLoading: false,
    error: null,
    rowKey: 'id',
    clickableRows: false,
});

const emit = defineEmits<{
    'action': [key: string];
    'row-action': [key: string, row: T, index: number];
    'row-click': [row: T, index: number];
    'sort': [key: string, direction: 'asc' | 'desc'];
    'refresh': [];
    'page-change': [page: number];
}>();

const sortKey = ref<string>('');
const sortDirection = ref<'asc' | 'desc'>('asc');

const hasRowActions = computed(() => props.rowActions.length > 0);

const sortedData = computed(() => {
    if (!sortKey.value) return props.data;
    
    const sorted = [...props.data].sort((a, b) => {
        const aValue = getCellValue(a, sortKey.value);
        const bValue = getCellValue(b, sortKey.value);
        
        if (aValue < bValue) return sortDirection.value === 'asc' ? -1 : 1;
        if (aValue > bValue) return sortDirection.value === 'asc' ? 1 : -1;
        return 0;
    });
    
    return sorted;
});

const paginationInfo = computed(() => {
    if (!props.pagination) return { start: 0, end: 0, total: 0 };
    
    const { currentPage, pageSize, totalItems } = props.pagination;
    const start = (currentPage - 1) * pageSize + 1;
    const end = Math.min(currentPage * pageSize, totalItems);
    
    return { start, end, total: totalItems };
});

const getCellValue = (row: T, key: string): any => {
    return key.split('.').reduce((obj, k) => obj?.[k], row);
};

const formatCellValue = (value: any, column: TableColumn): string => {
    if (value == null) return '';
    
    switch (column.format) {
        case 'date':
            return new Date(value).toLocaleDateString();
        case 'datetime':
            return new Date(value).toLocaleString();
        case 'number':
            return Number(value).toLocaleString();
        case 'currency':
            return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
        default:
            return String(value);
    }
};

const getRowKey = (row: T, index: number): string | number => {
    return getCellValue(row, props.rowKey) || index;
};

const getColumnClass = (column: TableColumn) => ({
    'sortable': column.sortable,
    'sorted': sortKey.value === column.key,
    [`align-${column.align || 'left'}`]: true,
    [column.headerClass || '']: column.headerClass,
});

const getCellClass = (column: TableColumn, _row: T) => ({
    [`align-${column.align || 'left'}`]: true,
    [column.cellClass || '']: column.cellClass,
});

const getRowClass = (_row: T, index: number) => ({
    'clickable': props.clickableRows,
    'even': index % 2 === 0,
    'odd': index % 2 === 1,
});

const getRowAttributes = (row: T) => {
    const attributes: Record<string, string> = {};
    
    // Add data-task-status attribute for task rows that have a status property
    if ('status' in row && row.status) {
        attributes['data-task-status'] = String(row.status);
    }
    
    // Add data-clickable attribute for task rows that have clickability info
    if ('isClickable' in row && typeof row.isClickable === 'boolean') {
        attributes['data-clickable'] = String(row.isClickable);
    }
    
    return attributes;
};

const getSortIcon = (columnKey: string) => {
    if (sortKey.value !== columnKey) return faSort;
    return sortDirection.value === 'asc' ? faSortUp : faSortDown;
};

const getSortIconClass = (columnKey: string) => ({
    'sort-icon': true,
    'active': sortKey.value === columnKey,
});

const handleSort = (column: TableColumn) => {
    if (!column.sortable) return;
    
    if (sortKey.value === column.key) {
        sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc';
    } else {
        sortKey.value = column.key;
        sortDirection.value = 'asc';
    }
    
    emit('sort', column.key, sortDirection.value);
};

const handleRowClick = (row: T, index: number) => {
    if (props.clickableRows) {
        emit('row-click', row, index);
    }
};

const handleRowAction = (key: string, row: T, index: number) => {
    emit('row-action', key, row, index);
};

const goToPage = (page: number) => {
    emit('page-change', page);
};
</script>

<style lang="scss" scoped>
.data-table {
    background: var(--color-white);
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
    overflow: hidden;
}

.table-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    border-bottom: 1px solid var(--color-gray-400);
    background: var(--color-gray-50);
}

.table-title {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.table-actions {
    display: flex;
    gap: 0.5rem;
}

.table-container {
    position: relative;
    overflow-x: auto;
}

.table-loading,
.table-error,
.table-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    text-align: center;
    
    svg {
        font-size: 3rem;
        color: var(--color-gray-600);
        margin-bottom: 1rem;
    }
    
    h4 {
        margin: 0 0 0.5rem;
        color: var(--color-gray-800);
    }
    
    p {
        margin: 0 0 1rem;
        color: var(--color-gray-600);
    }
}

.loading-spinner {
    width: 3rem;
    height: 3rem;
    border: 3px solid var(--color-gray-200);
    border-top: 3px solid var(--color-primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 1rem;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.table {
    width: 100%;
    border-collapse: collapse;
    
    th, td {
        padding: 1rem;
        border-bottom: 1px solid var(--color-gray-400);
        vertical-align: middle;
    }
    
    th {
        background: var(--color-gray-50);
        font-weight: 600;
        color: var(--color-gray-800);
        border-bottom: 2px solid var(--color-gray-400);
        
        &.sortable {
            cursor: pointer;
            user-select: none;
            
            &:hover {
                background: var(--color-gray-100);
            }
        }
        
        &.align-center { text-align: center; }
        &.align-right { text-align: right; }
    }
    
    td {
        color: var(--color-gray-800);
        
        &.align-center { text-align: center; }
        &.align-right { text-align: right; }
    }
    
    tbody tr {
        &:hover {
            background: var(--color-gray-50);
        }
        
        &.clickable {
            cursor: pointer;
            
            &:hover {
                background: var(--color-primary-light);
            }
        }
    }
}

.column-header {
    display: flex;
    align-items: center;
    gap: 0.25rem;
}

.sort-icon {
    opacity: 0.5;
    transition: opacity 0.2s ease;
    
    &.active {
        opacity: 1;
    }
}

.cell-content {
    display: flex;
    align-items: center;
}

.actions-column,
.actions-cell {
    width: 1%;
    white-space: nowrap;
}

.row-actions {
    display: flex;
    gap: 0.25rem;
    justify-content: flex-end;
}

.table-pagination {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    border-top: 1px solid var(--color-gray-400);
    background: var(--color-gray-50);
}

.pagination-info {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.pagination-controls {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.page-info {
    font-size: 0.875rem;
    color: var(--color-gray-800);
    font-weight: 500;
}

@media (max-width: 768px) {
    .table-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
    }
    
    .table-actions {
        justify-content: center;
    }
    
    .table-pagination {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
        text-align: center;
    }
    
    .pagination-controls {
        justify-content: center;
    }
}
</style>
