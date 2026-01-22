export interface ServerDTO {
    id: string;
    name: string;
    description?: string;
    createdAt: string;
    isOnline: boolean;
    lastSeenAt?: string | null;
}
