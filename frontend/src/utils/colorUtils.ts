/**
 * Color utility functions for generating and manipulating colors
 */

const LABEL_COLOR_PALETTE = [
    '#3B82F6', // Blue
    '#EF4444', // Red
    '#10B981', // Green
    '#F59E0B', // Amber
    '#8B5CF6', // Violet
    '#F97316', // Orange
    '#06B6D4', // Cyan
    '#84CC16', // Lime
    '#EC4899', // Pink
    '#6366F1', // Indigo
    '#14B8A6', // Teal
    '#F472B6', // Rose
    '#A855F7', // Purple
    '#22C55E', // Emerald
    '#EAB308', // Yellow
    '#DC2626', // Red (darker)
    '#059669', // Green (darker)
    '#7C3AED', // Violet (darker)
    '#EA580C', // Orange (darker)
    '#0891B2', // Cyan (darker)
];

/**
 * Generates a random color from the predefined palette
 * @returns A hex color string
 */
export function generateRandomColor(): string {
    return LABEL_COLOR_PALETTE[Math.floor(Math.random() * LABEL_COLOR_PALETTE.length)];
}

/**
 * Gets a contrasting text color (black or white) for a given background color
 * @param backgroundColor - Hex color string (e.g., '#FF0000')
 * @returns '#000000' for light backgrounds, '#FFFFFF' for dark backgrounds
 */
export function getContrastingTextColor(backgroundColor: string): string {
    // Remove the # if present
    const hex = backgroundColor.replace('#', '');
    
    // Convert to RGB
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);
    
    // Calculate luminance
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    
    // Return black for light backgrounds, white for dark backgrounds
    return luminance > 0.5 ? '#000000' : '#FFFFFF';
}

/**
 * Validates if a string is a valid hex color
 * @param color - Color string to validate
 * @returns True if valid hex color, false otherwise
 */
export function isValidHexColor(color: string): boolean {
    return /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/.test(color);
}

/**
 * Converts a hex color to RGBA with opacity
 * @param hex - Hex color string (e.g., '#FF0000')
 * @param opacity - Opacity value between 0 and 1
 * @returns RGBA string (e.g., 'rgba(255, 0, 0, 0.5)')
 */
export function hexToRgba(hex: string, opacity: number): string {
    const cleanHex = hex.replace('#', '');
    const r = parseInt(cleanHex.substring(0, 2), 16);
    const g = parseInt(cleanHex.substring(2, 2), 16);
    const b = parseInt(cleanHex.substring(4, 2), 16);
    
    return `rgba(${r}, ${g}, ${b}, ${opacity})`;
}

/**
 * Gets the next color in the palette that hasn't been used
 * @param usedColors - Array of already used hex colors
 * @returns A hex color string that hasn't been used, or a random color if all are used
 */
export function getNextAvailableColor(usedColors: string[]): string {
    const availableColors = LABEL_COLOR_PALETTE.filter(color => !usedColors.includes(color));
    
    if (availableColors.length > 0) {
        return availableColors[0];
    }
    
    // If all colors are used, return a random one
    return generateRandomColor();
}
