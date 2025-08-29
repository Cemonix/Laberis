import { BaseService } from '../base/baseService';
import type { UserDto } from '@/services/auth/auth.types';

/**
 * Service class for managing user-related operations.
 * Extends BaseService to inherit common functionality.
 */
class UserService extends BaseService {
    protected readonly baseUrl = '/users';

    constructor() {
        super('UserService');
    }

    /**
     * Fetches all users from the backend, handling pagination automatically.
     * @returns A promise resolving to an array of all users.
     */
    async getAllUsers(): Promise<UserDto[]> {
        this.logger.info('Fetching all users with automatic pagination');

        const allUsers: UserDto[] = [];
        let page = 1;
        let totalPages = 1;

        do {
            const response = await this.getPaginated<UserDto>(this.baseUrl, {
                pageNumber: page,
                pageSize: 100
            });

            allUsers.push(...response.data);
            totalPages = response.totalPages;
            page++;

        } while (page <= totalPages);

        this.logger.info('Successfully fetched all users', {
            totalUsers: allUsers.length,
            totalPages: totalPages - 1
        });

        return allUsers;
    }
}

export const userService = new UserService();