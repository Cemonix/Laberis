import type { Environment } from "./config.types";
import { LogLevel, type LogConfig } from "@/core/logger/logger.types";

function validateEnv(environment: Environment): void {
    const required = [
        "API_BASE_URL",
        "NODE_ENV",
        "APP_NAME",
        "APP_VERSION",
    ] as const;

    for (const key of required) {
        if (!environment[key]) {
            throw new Error(`Missing required environment variable: ${key}`);
        }
    }
}

function createLogConfig(): LogConfig {
    const logLevel = import.meta.env.VITE_LOG_LEVEL as LogLevel;
    const validLogLevels = Object.values(LogLevel);
    
    return {
        level: validLogLevels.includes(logLevel) ? logLevel : LogLevel.INFO,
        enableInProduction: import.meta.env.VITE_LOG_IN_PRODUCTION === 'true',
        enableTimestamps: import.meta.env.VITE_LOG_TIMESTAMPS !== 'false',
        enableServicePrefix: import.meta.env.VITE_LOG_SERVICE_PREFIX !== 'false'
    };
}

function createEnv(): Environment {
    const nodeEnv = import.meta.env.NODE_ENV || "development";

    const environment = {
        API_BASE_URL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api",
        NODE_ENV: nodeEnv,
        IS_DEVELOPMENT: nodeEnv === "development",
        IS_PRODUCTION: nodeEnv === "production",
        APP_NAME: import.meta.env.VITE_APP_NAME || "Laberis",
        APP_VERSION: import.meta.env.VITE_APP_VERSION || "0.1.0",
        LOGGING: createLogConfig(),
        AUTO_LOGIN_DEV: import.meta.env.VITE_AUTO_LOGIN_DEV === 'true'
    } as const;

    validateEnv(environment);
    return environment;
}

export const env = createEnv();
