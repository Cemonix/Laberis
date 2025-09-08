import { BaseService } from '../base/baseService';
import { AppLogger } from '@/core/logger/logger';

export interface AuthorizationCheckContext {
    projectId?: number;
    [key: string]: any;
}

export interface AuthorizationCheck {
    permission: string;
    context?: AuthorizationCheckContext;
}

export interface AuthorizationBatchResponse {
    results: boolean[];
    policyVersion?: string;
}

/**
 * AuthorizationService calls backend to evaluate permissions authoritatively.
 * This replaces frontend-cached permission context and supports batching.
 */
class AuthorizationService extends BaseService {
    protected readonly baseUrl = '/permissions';
    private readonly log = AppLogger.createServiceLogger('AuthorizationService');

    async authorize(check: AuthorizationCheck): Promise<boolean> {
        const resp = await this.post<AuthorizationCheck, { allow: boolean; policyVersion?: string }>(
            this.getBaseUrl('authorize'),
            check
        );
        this.log.info(`authorize(${check.permission}) => ${resp.allow}`);
        return resp.allow;
    }

    async authorizeBatch(checks: AuthorizationCheck[]): Promise<AuthorizationBatchResponse> {
        const resp = await this.post<{ checks: AuthorizationCheck[] }, AuthorizationBatchResponse>(
            this.getBaseUrl('authorize/batch'),
            { checks }
        );
        this.log.info(`authorizeBatch(${checks.length}) => [${resp.results.join(',')}]`);
        return resp;
    }
}

export const authorizationService = new AuthorizationService("AuthorizationService");
