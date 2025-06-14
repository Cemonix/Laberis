import type { Environment } from "@/types/environment";

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

function createEnv(): Environment {
    const nodeEnv = import.meta.env.NODE_ENV || "development";

    const environment = {
        API_BASE_URL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api",
        NODE_ENV: nodeEnv,
        IS_DEVELOPMENT: nodeEnv === "development",
        IS_PRODUCTION: nodeEnv === "production",
        APP_NAME: import.meta.env.VITE_APP_NAME || "Laberis",
        APP_VERSION: import.meta.env.VITE_APP_VERSION || "0.1.0",
    } as const;

    validateEnv(environment);
    return environment;
}

export const env = createEnv();
