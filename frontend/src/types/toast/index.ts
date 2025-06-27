export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
    id: string;
    title: string;
    message: string;
    type: ToastType;
    duration?: number;
    persistent?: boolean;
    createdAt: number;
}

export interface ToastOptions {
    duration?: number;
    persistent?: boolean;
}