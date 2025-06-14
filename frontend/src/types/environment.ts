export interface Environment {
    readonly API_BASE_URL: string;
    readonly NODE_ENV: string;
    readonly IS_DEVELOPMENT: boolean;
    readonly IS_PRODUCTION: boolean;
    readonly APP_NAME: string;
    readonly APP_VERSION: string;
}