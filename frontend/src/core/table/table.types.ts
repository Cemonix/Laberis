export interface TableColumn {
    key: string;
    label: string;
    sortable?: boolean;
    align?: 'left' | 'center' | 'right';
    format?: 'date' | 'datetime' | 'number' | 'currency';
    component?: any;
    headerClass?: string;
    cellClass?: string;
    width?: string;
}

export interface TableAction {
    key: string;
    label: string;
    icon?: any;
    variant?: 'primary' | 'secondary' | 'danger';
    disabled?: boolean;
}

export interface TableRowAction<T = any> {
    key: string;
    label?: string;
    icon?: any;
    variant?: 'primary' | 'secondary' | 'danger';
    disabled?: (row: T) => boolean;
    tooltip?: (row: T) => string;
}

export interface TablePagination {
    currentPage: number;
    pageSize: number;
    totalPages: number;
    totalItems: number;
}