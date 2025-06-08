// TODO: Properly implement according to the backend API
export enum AssetStatus {
    NEW = 'new',
    ANNOTATED = 'annotated',
    IN_REVIEW = 'in_review',
    APPROVED = 'approved',
    REJECTED = 'rejected',
}

export interface Asset {
    assetId: number;
    dataSourceId: number;
    name: string;
    path: string;
    thumbnailUrl: string;
    status: AssetStatus;
    annotationsCount: number;
    createdAt: string;
}