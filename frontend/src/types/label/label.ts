export interface Label {
    labelId: number;
    name: string;
    color: string;
    description?: string;
    labelSchemeId: number;
    metadata?: any;
}

export interface FormPayloadLabel {
    name: string;
    description: string;
    labels: Omit<Label, 'labelId' | 'labelSchemeId'>[];
}