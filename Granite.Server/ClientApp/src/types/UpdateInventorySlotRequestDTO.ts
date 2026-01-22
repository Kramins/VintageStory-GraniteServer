export interface UpdateInventorySlotRequestDTO {
    entityClass: string;
    entityId: number;
    slotIndex: number;
    stackSize: number | null;
}