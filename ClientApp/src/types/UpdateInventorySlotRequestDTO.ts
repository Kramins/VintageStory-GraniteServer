export interface UpdateInventorySlotRequestDTO {
    class: string;
    id: number;
    slotIndex: number;
    stackSize: number | null;
}