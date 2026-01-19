export interface BanRequestDTO {
    issuedBy?: string | null;
    reason?: string | null;
    untilDate?: string | null; // ISO string
}
