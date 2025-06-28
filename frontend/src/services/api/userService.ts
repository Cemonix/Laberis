import apiClient from './apiClient';
import { transformApiError, isValidPaginatedResponse } from '@/services/utils';
import type { PaginatedResponse } from '@/types/api';
import type { UserDto } from '@/types/auth/auth';

class UserService {
    private readonly baseUrl = '/users';

    /**
     * Fetches all users from the backend, handling pagination automatically.
     * @returns A promise resolving to an array of all users.
     */
    async getAllUsers(): Promise<UserDto[]> {
        const allUsers: UserDto[] = [];
        let page = 1;
        let totalPages = 1;

        try {
            do {
                const response = await apiClient.get<PaginatedResponse<UserDto>>(this.baseUrl, {
                    params: { pageNumber: page, pageSize: 100 }
                });

                if (!isValidPaginatedResponse(response)) {
                    throw new Error('Invalid paginated response for users.');
                }

                allUsers.push(...response.data.data);
                totalPages = response.data.totalPages;
                page++;

            } while (page <= totalPages);

            return allUsers;
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch users');
        }
    }
}

export const userService = new UserService();