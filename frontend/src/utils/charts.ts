// Chart animation utilities
export const animateValue = (
    start: number,
    end: number,
    duration: number,
    callback: (value: number) => void
) => {
    let startTime: number | null = null;

    const animate = (currentTime: number) => {
        if (!startTime) startTime = currentTime;
        
        const timeElapsed = currentTime - startTime;
        const progress = Math.min(timeElapsed / duration, 1);
        
        // Easing function for smooth animation
        const easeOutCubic = 1 - Math.pow(1 - progress, 3);
        const currentValue = start + (end - start) * easeOutCubic;
        
        callback(Math.round(currentValue));
        
        if (progress < 1) {
            requestAnimationFrame(animate);
        }
    };

    requestAnimationFrame(animate);
};

export const formatNumber = (num: number): string => {
    if (num >= 1000000) {
        return (num / 1000000).toFixed(1) + 'M';
    }
    if (num >= 1000) {
        return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
};
