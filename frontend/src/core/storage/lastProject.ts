import { AppLogger } from '@/utils/logger';
import type { LastProjectData } from '@/types/storage/lastProject';

const LAST_PROJECT_STORAGE_KEY_PREFIX = 'laberis_last_project_';
const logger = AppLogger.createServiceLogger('LastProjectUtil');

/**
 * Utility for managing the user's last accessed project in localStorage
 */
export class LastProjectManager {
    /**
     * Generate user-specific storage key
     */
    private static getUserStorageKey(userEmail: string): string {
        return `${LAST_PROJECT_STORAGE_KEY_PREFIX}${userEmail}`;
    }

    /**
     * Save the current project as the user's last accessed project
     */
    static saveLastProject(projectId: number, projectName: string, userEmail: string): void {
        try {
            const lastProjectData: LastProjectData = {
                projectId,
                projectName,
                lastAccessedAt: Date.now()
            };
            
            const storageKey = this.getUserStorageKey(userEmail);
            localStorage.setItem(storageKey, JSON.stringify(lastProjectData));
            logger.info(`Saved last project for user ${userEmail}: ${projectName} (ID: ${projectId})`);
        } catch (error) {
            logger.error('Failed to save last project to localStorage', error);
        }
    }

    /**
     * Get the user's last accessed project
     */
    static getLastProject(userEmail: string): LastProjectData | null {
        try {
            const storageKey = this.getUserStorageKey(userEmail);
            const stored = localStorage.getItem(storageKey);
            if (!stored) {
                return null;
            }

            const parsed = JSON.parse(stored) as LastProjectData;
            
            // Validate the data structure
            if (!parsed.projectId || !parsed.projectName || !parsed.lastAccessedAt) {
                logger.warn(`Invalid last project data found for user ${userEmail}, clearing storage`);
                this.clearLastProject(userEmail);
                return null;
            }

            // Check if the data is too old (older than 30 days)
            const thirtyDaysAgo = Date.now() - (30 * 24 * 60 * 60 * 1000);
            if (parsed.lastAccessedAt < thirtyDaysAgo) {
                logger.info(`Last project data for user ${userEmail} is too old, clearing storage`);
                this.clearLastProject(userEmail);
                return null;
            }

            logger.info(`Retrieved last project for user ${userEmail}: ${parsed.projectName} (ID: ${parsed.projectId})`);
            return parsed;
        } catch (error) {
            logger.error(`Failed to retrieve last project for user ${userEmail} from localStorage`, error);
            this.clearLastProject(userEmail);
            return null;
        }
    }

    /**
     * Clear the stored last project data for a specific user
     */
    static clearLastProject(userEmail: string): void {
        try {
            const storageKey = this.getUserStorageKey(userEmail);
            localStorage.removeItem(storageKey);
            logger.info(`Cleared last project data for user ${userEmail}`);
        } catch (error) {
            logger.error(`Failed to clear last project for user ${userEmail} from localStorage`, error);
        }
    }

    /**
     * Check if there's a valid last project stored for a user
     */
    static hasLastProject(userEmail: string): boolean {
        return this.getLastProject(userEmail) !== null;
    }
}