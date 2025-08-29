import { describe, it, expect } from 'vitest';
import { 
  isPermissionChangingEndpoint, 
  getPermissionChangeDescription,
} from '../permissionEndpoints';

describe('Permission Endpoints Detection', () => {
  describe('isPermissionChangingEndpoint', () => {
    it('should detect project creation endpoint', () => {
      expect(isPermissionChangingEndpoint('/projects', 'POST')).toBe(true);
      expect(isPermissionChangingEndpoint('/projects', 'GET')).toBe(false);
    });

    it('should detect invitation acceptance endpoints', () => {
      expect(isPermissionChangingEndpoint('/invitations/accept/abc123', 'POST')).toBe(true);
      expect(isPermissionChangingEndpoint('/invitations/accept/xyz789', 'POST')).toBe(true);
      expect(isPermissionChangingEndpoint('/invitations/accept/abc123', 'GET')).toBe(false);
    });

    it('should detect project member management endpoints', () => {
      expect(isPermissionChangingEndpoint('/projects/123/projectmembers/456', 'PUT')).toBe(true);
      expect(isPermissionChangingEndpoint('/projects/123/projectmembers/456', 'PATCH')).toBe(true);
      expect(isPermissionChangingEndpoint('/projects/123/projectmembers/456', 'DELETE')).toBe(true);
      expect(isPermissionChangingEndpoint('/projects/123/projectmembers', 'POST')).toBe(true);
      expect(isPermissionChangingEndpoint('/projects/123/projectmembers', 'GET')).toBe(false);
    });

    it('should handle URL normalization', () => {
      expect(isPermissionChangingEndpoint('projects', 'POST')).toBe(true);
      expect(isPermissionChangingEndpoint('/projects?param=value', 'POST')).toBe(true);
    });

    it('should return false for non-permission endpoints', () => {
      expect(isPermissionChangingEndpoint('/assets/123', 'GET')).toBe(false);
      expect(isPermissionChangingEndpoint('/workflows', 'GET')).toBe(false);
      expect(isPermissionChangingEndpoint('/unknown/endpoint', 'POST')).toBe(false);
    });

    it('should handle invalid inputs', () => {
      expect(isPermissionChangingEndpoint(undefined, 'POST')).toBe(false);
      expect(isPermissionChangingEndpoint('/projects', '')).toBe(false);
      expect(isPermissionChangingEndpoint('', 'POST')).toBe(false);
    });
  });

  describe('getPermissionChangeDescription', () => {
    it('should return correct descriptions for permission-changing endpoints', () => {
      expect(getPermissionChangeDescription('/projects', 'POST'))
        .toBe('Project creation - user becomes manager');
      
      expect(getPermissionChangeDescription('/invitations/accept/abc123', 'POST'))
        .toBe('Invitation acceptance - user joins project');
      
      expect(getPermissionChangeDescription('/projects/123/projectmembers/456', 'PUT'))
        .toBe('Project member role updates');
    });

    it('should return null for non-permission endpoints', () => {
      expect(getPermissionChangeDescription('/assets/123', 'GET')).toBe(null);
      expect(getPermissionChangeDescription('/projects', 'GET')).toBe(null);
    });

    it('should handle invalid inputs', () => {
      expect(getPermissionChangeDescription(undefined, 'POST')).toBe(null);
      expect(getPermissionChangeDescription('/projects', '')).toBe(null);
    });
  });
});