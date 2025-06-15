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
