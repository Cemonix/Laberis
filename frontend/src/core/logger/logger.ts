import type { App } from 'vue'
import 'pinia'
import { LogLevel } from './logger.types'
import { env } from '@/config/env'
import type { ContextLogger } from './logger.types'

/**
 * Centralized logging utility with context-aware loggers for different application layers.
 * 
 * Usage examples:
 * 
 * In Vue Components:
 * ```typescript
 * const logger = AppLogger.createComponentLogger('MyComponent');
 * logger.info('Component mounted');
 * ```
 * 
 * In Services:
 * ```typescript
 * const logger = AppLogger.createServiceLogger('UserService');
 * logger.debug('Fetching user data');
 * ```
 * 
 * In Pinia Stores:
 * ```typescript
 * const logger = AppLogger.createStoreLogger('authStore');
 * logger.warn('Authentication token expired');
 * ```
 * 
 * Global logging (use sparingly):
 * ```typescript
 * AppLogger.info('Application started');
 * ```
 */

class Logger {
    private logLevels = [LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR];

    constructor() {
        // Logger now uses env.LOGGING for configuration
    }

    private shouldLog(level: LogLevel): boolean {
        if (env.IS_PRODUCTION && !env.LOGGING.enableInProduction) {
            return false;
        }
        
        const currentLevelIndex = this.logLevels.indexOf(env.LOGGING.level);
        const requestedLevelIndex = this.logLevels.indexOf(level);
        
        return requestedLevelIndex >= currentLevelIndex;
    }

    private formatMessage(level: LogLevel, message: string, context?: string, contextType?: 'component' | 'service' | 'store'): string {
        let formattedMessage = message;
        
        if (env.LOGGING.enableTimestamps) {
            const timestamp = new Date().toISOString();
            formattedMessage = `${timestamp} ${level.toUpperCase()} ${formattedMessage}`;
        }
        
        if (env.LOGGING.enableServicePrefix && context) {
            const prefix = contextType ? `${contextType.toUpperCase()}:${context}` : context;
            formattedMessage = env.LOGGING.enableTimestamps 
                ? formattedMessage.replace(level.toUpperCase(), `${level.toUpperCase()} [${prefix}]`)
                : `[${prefix}] ${formattedMessage}`;
        }
        
        return formattedMessage;
    }

    debug(message: string, obj?: any, context?: string, contextType?: 'component' | 'service' | 'store'): void {
        if (!this.shouldLog(LogLevel.DEBUG)) return;
        
        console.debug(this.formatMessage(LogLevel.DEBUG, message, context, contextType));
        if (obj !== undefined && obj !== null) console.debug(obj);
    }

    info(message: string, obj?: any, context?: string, contextType?: 'component' | 'service' | 'store'): void {
        if (!this.shouldLog(LogLevel.INFO)) return;
        
        console.info(this.formatMessage(LogLevel.INFO, message, context, contextType));
        if (obj !== undefined && obj !== null) console.info(obj);
    }

    warn(message: string, obj?: any, context?: string, contextType?: 'component' | 'service' | 'store'): void {
        if (!this.shouldLog(LogLevel.WARN)) return;
        
        console.warn(this.formatMessage(LogLevel.WARN, message, context, contextType));
        if (obj !== undefined && obj !== null) console.warn(obj);
    }

    error(message: string, obj?: any, context?: string, contextType?: 'component' | 'service' | 'store'): void {
        if (!this.shouldLog(LogLevel.ERROR)) return;
        
        console.error(this.formatMessage(LogLevel.ERROR, message, context, contextType));
        if (obj !== undefined && obj !== null) console.error(obj);
    }

    // Context-specific logger creators
    createComponentLogger(componentName: string): ContextLogger {
        return {
            debug: (message: string, obj?: any) => this.debug(message, obj, componentName, 'component'),
            info: (message: string, obj?: any) => this.info(message, obj, componentName, 'component'),
            warn: (message: string, obj?: any) => this.warn(message, obj, componentName, 'component'),
            error: (message: string, obj?: any) => this.error(message, obj, componentName, 'component'),
        };
    }

    createServiceLogger(serviceName: string): ContextLogger {
        return {
            debug: (message: string, obj?: any) => this.debug(message, obj, serviceName, 'service'),
            info: (message: string, obj?: any) => this.info(message, obj, serviceName, 'service'),
            warn: (message: string, obj?: any) => this.warn(message, obj, serviceName, 'service'),
            error: (message: string, obj?: any) => this.error(message, obj, serviceName, 'service'),
        };
    }

    createStoreLogger(storeName: string): ContextLogger {
        return {
            debug: (message: string, obj?: any) => this.debug(message, obj, storeName, 'store'),
            info: (message: string, obj?: any) => this.info(message, obj, storeName, 'store'),
            warn: (message: string, obj?: any) => this.warn(message, obj, storeName, 'store'),
            error: (message: string, obj?: any) => this.error(message, obj, storeName, 'store'),
        };
    }
}

export const AppLogger = new Logger();

export const logger = {
    install(app: App) {
        // Global logger methods (without context)
        app.config.globalProperties.$logDebug = (message: string, obj?: any) => 
            AppLogger.debug(message, obj);
        app.config.globalProperties.$logInfo = (message: string, obj?: any) => 
            AppLogger.info(message, obj);
        app.config.globalProperties.$logWarn = (message: string, obj?: any) => 
            AppLogger.warn(message, obj);
        app.config.globalProperties.$logError = (message: string, obj?: any) => 
            AppLogger.error(message, obj);
        
        // Context-aware logger creators
        app.config.globalProperties.$createComponentLogger = (componentName: string) =>
            AppLogger.createComponentLogger(componentName);
        app.config.globalProperties.$createServiceLogger = (serviceName: string) =>
            AppLogger.createServiceLogger(serviceName);
        app.config.globalProperties.$createStoreLogger = (storeName: string) =>
            AppLogger.createStoreLogger(storeName);
    }
}

export const piniaLogger = function () {
    return {
        $logDebug: (message: string, obj?: any) => AppLogger.debug(message, obj),
        $logInfo: (message: string, obj?: any) => AppLogger.info(message, obj),
        $logWarn: (message: string, obj?: any) => AppLogger.warn(message, obj),
        $logError: (message: string, obj?: any) => AppLogger.error(message, obj),
        $createStoreLogger: (storeName: string) => AppLogger.createStoreLogger(storeName)
    }
}