import type { App } from 'vue'
import 'pinia'
import { LogLevel } from '@/types/logging'
import { env } from '@/config/env'

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

    private formatMessage(level: LogLevel, message: string, service?: string): string {
        let formattedMessage = message;
        
        if (env.LOGGING.enableTimestamps) {
            const timestamp = new Date().toISOString();
            formattedMessage = `${timestamp} ${level.toUpperCase()} ${formattedMessage}`;
        }
        
        if (env.LOGGING.enableServicePrefix && service) {
            formattedMessage = env.LOGGING.enableTimestamps 
                ? formattedMessage.replace(level.toUpperCase(), `${level.toUpperCase()} [${service}]`)
                : `[${service}] ${formattedMessage}`;
        }
        
        return formattedMessage;
    }

    debug(message: string, obj?: any, service?: string): void {
        if (!this.shouldLog(LogLevel.DEBUG)) return;
        
        console.debug(this.formatMessage(LogLevel.DEBUG, message, service));
        if (obj !== undefined && obj !== null) console.debug(obj);
    }

    info(message: string, obj?: any, service?: string): void {
        if (!this.shouldLog(LogLevel.INFO)) return;
        
        console.info(this.formatMessage(LogLevel.INFO, message, service));
        if (obj !== undefined && obj !== null) console.info(obj);
    }

    warn(message: string, obj?: any, service?: string): void {
        if (!this.shouldLog(LogLevel.WARN)) return;
        
        console.warn(this.formatMessage(LogLevel.WARN, message, service));
        if (obj !== undefined && obj !== null) console.warn(obj);
    }

    error(message: string, obj?: any, service?: string): void {
        if (!this.shouldLog(LogLevel.ERROR)) return;
        
        console.error(this.formatMessage(LogLevel.ERROR, message, service));
        if (obj !== undefined && obj !== null) console.error(obj);
    }

    createServiceLogger(serviceName: string) {
        return {
            debug: (message: string, obj?: any) => this.debug(message, obj, serviceName),
            info: (message: string, obj?: any) => this.info(message, obj, serviceName),
            warn: (message: string, obj?: any) => this.warn(message, obj, serviceName),
            error: (message: string, obj?: any) => this.error(message, obj, serviceName),
        };
    }
}

export const AppLogger = new Logger();

export const logger = {
    install(app: App) {
        app.config.globalProperties.$logDebug = (message: string, obj?: any) => 
            AppLogger.debug(message, obj);
        app.config.globalProperties.$logInfo = (message: string, obj?: any) => 
            AppLogger.info(message, obj);
        app.config.globalProperties.$logWarn = (message: string, obj?: any) => 
            AppLogger.warn(message, obj);
        app.config.globalProperties.$logError = (message: string, obj?: any) => 
            AppLogger.error(message, obj);
    }
}

export const piniaLogger = function () {
    return {
        $logDebug: (message: string, obj?: any) => AppLogger.debug(message, obj),
        $logInfo: (message: string, obj?: any) => AppLogger.info(message, obj),
        $logWarn: (message: string, obj?: any) => AppLogger.warn(message, obj),
        $logError: (message: string, obj?: any) => AppLogger.error(message, obj)
    }
}

// Type declarations for Vue components
declare module '@vue/runtime-core' {
    interface ComponentCustomProperties {
        $logDebug: (message: string, obj?: any) => void
        $logInfo: (message: string, obj?: any) => void
        $logWarn: (message: string, obj?: any) => void
        $logError: (message: string, obj?: any) => void
    }
}

// Type declarations for Pinia stores
declare module 'pinia' {
    export interface PiniaCustomProperties {
        $logDebug: (message: string, obj?: any) => void
        $logInfo: (message: string, obj?: any) => void
        $logWarn: (message: string, obj?: any) => void
        $logError: (message: string, obj?: any) => void
    }
}
