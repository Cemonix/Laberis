/**
 * Logger types and configuration interfaces
 */

export enum LogLevel {
    DEBUG = 'debug',
    INFO = 'info',
    WARN = 'warn',
    ERROR = 'error'
}

export interface LogConfig {
    readonly level: LogLevel;
    readonly enableInProduction: boolean;
    readonly enableTimestamps: boolean;
    readonly enableServicePrefix: boolean;
}

/**
 * Context logger interface for components, services, and stores
 */
export interface ContextLogger {
    debug: (message: string, obj?: any) => void;
    info: (message: string, obj?: any) => void;
    warn: (message: string, obj?: any) => void;
    error: (message: string, obj?: any) => void;
}
