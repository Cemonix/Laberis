export interface TableColumn {
    key: string;
    label: string;
    sortable?: boolean;
    width?: string;
    align?: 'left' | 'center' | 'right';
    format?: 'text' | 'date' | 'datetime' | 'number' | 'currency' | 'custom';
    component?: any;
    cellClass?: string;
    headerClass?: string;
}

export interface TableAction {
    key: string;
    label: string;
    icon?: any;
    variant?: 'primary' | 'secondary';
    disabled?: boolean;
}

export interface TableRowAction<T = any> {
    key: string;
    label?: string;
    icon?: any;
    variant?: 'primary' | 'secondary';
    disabled?: (row: T) => boolean;
    tooltip?: (row: T) => string;
}

export interface TablePagination {
    currentPage: number;
    totalPages: number;
    pageSize: number;
    totalItems: number;
}

export interface TableSortConfig {
    key: string;
    direction: 'asc' | 'desc';
}

// Generic table data interface
export interface TableData {
    [key: string]: any;
}

// Table configuration interface
export interface TableConfig<T = TableData> {
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
    defaultSort?: TableSortConfig;
}
