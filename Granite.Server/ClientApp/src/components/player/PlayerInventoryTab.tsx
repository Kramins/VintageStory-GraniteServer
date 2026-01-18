import React, { useState } from 'react';
import { Box, Typography, TableContainer, Paper, Table, TableHead, TableRow, TableCell, TableBody, IconButton, TextField, Alert } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import SaveIcon from '@mui/icons-material/Save';
import CancelIcon from '@mui/icons-material/Cancel';
import DeleteIcon from '@mui/icons-material/Delete';
import { useAppDispatch } from '../../store/store';
import { PlayerService } from '../../services/PlayerService';
import { fetchPlayerDetails } from '../../store/slices/playerDetailsSlice';
import CollectibleAutocomplete from '../../components/CollectibleAutocomplete';
import type { PlayerDetailsDTO, InventorySlotDTO } from '../../types/PlayerDetailsDTO';
import type { CollectibleObjectDTO } from '../../types/CollectibleObjectDTO';
import type { UpdateInventorySlotRequestDTO } from '../../types/UpdateInventorySlotRequestDTO';

interface Props {
  playerId: string;
  inventories: PlayerDetailsDTO['inventories'];
  collectibles: CollectibleObjectDTO[];
  collectiblesLoading: boolean;
  collectiblesError: string | null;
}

interface EditingState {
  inventoryName: string;
  slotIndex: number;
}

interface EditFormData {
  name: string;
  entityId: number;
  entityClass: string;
  stackSize: number;
}

const PlayerInventoryTab: React.FC<Props> = ({ playerId, inventories, collectibles, collectiblesLoading, collectiblesError }) => {
  const dispatch = useAppDispatch();
  const [removing, setRemoving] = useState<string | null>(null);
  const [removeError, setRemoveError] = useState<string | null>(null);
  const [editingSlot, setEditingSlot] = useState<EditingState | null>(null);
  const [editFormData, setEditFormData] = useState<EditFormData>({ name: '', entityClass: '', entityId: 0, stackSize: 0 });
  const [selectedCollectible, setSelectedCollectible] = useState<CollectibleObjectDTO | null>(null);
  const [saving, setSaving] = useState(false);

  const handleEditClick = (slot: InventorySlotDTO, inventoryName: string) => {
    setEditingSlot({ inventoryName, slotIndex: slot.slotIndex });
    setEditFormData({
      name: slot.name || '',
      entityClass: slot.entityClass || '',
      entityId: slot.entityId,
      stackSize: slot.stackSize,
    });
    const matchingCollectible = collectibles.find(c => c.id === slot.entityId);
    setSelectedCollectible(matchingCollectible || null);
  };

  const handleSaveEdit = async () => {
    if (!playerId || !editingSlot || !selectedCollectible) return;
    setSaving(true);
    try {
      await PlayerService.updatePlayerInventorySlot(playerId, editingSlot.inventoryName, {
        slotIndex: editingSlot.slotIndex,
        entityClass: selectedCollectible.type,
        entityId: selectedCollectible.id,
        stackSize: editFormData.stackSize,
      } as UpdateInventorySlotRequestDTO);
      dispatch(fetchPlayerDetails(playerId) as any);
      setEditingSlot(null);
      setSelectedCollectible(null);
    } catch (err: any) {
      setRemoveError(err.message || 'Failed to update item');
    } finally {
      setSaving(false);
    }
  };

  const handleCancelEdit = () => {
    setEditingSlot(null);
    setSelectedCollectible(null);
  };

  const isEditing = (inventoryName: string, slotIndex: number) =>
    editingSlot?.inventoryName === inventoryName && editingSlot?.slotIndex === slotIndex;

  const handleRemoveItem = async (inventoryName: string, slotIndex: number) => {
    if (!playerId) return;
    const key = `${inventoryName}-${slotIndex}`;
    setRemoving(key);
    setRemoveError(null);
    try {
      await PlayerService.removeItemFromInventory(playerId, inventoryName, slotIndex);
      dispatch(fetchPlayerDetails(playerId) as any);
    } catch (err: any) {
      setRemoveError(err.message || 'Failed to remove item');
    } finally {
      setRemoving(null);
    }
  };

  if (removeError) {
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="error" onClose={() => setRemoveError(null)}>{removeError}</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ mt: 0 }}>
      {Object.entries(inventories).map(([inventoryName, inventory]) => (
        <Box key={inventoryName} sx={{ mb: 3 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>
            {inventory.name}
          </Typography>
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell>Slot</TableCell>
                  <TableCell>Item Name</TableCell>
                  <TableCell align="right">Stack Size</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {inventory.slots.map((slot) => {
                  const isCurrentlyEditing = isEditing(inventoryName, slot.slotIndex);
                  return (
                    <TableRow key={`${inventoryName}-${slot.slotIndex}`} sx={{ backgroundColor: isCurrentlyEditing ? '#f5f5f5' : 'inherit' }}>
                      <TableCell>{slot.slotIndex}</TableCell>
                      <TableCell>
                        {isCurrentlyEditing ? (
                          <CollectibleAutocomplete
                            value={selectedCollectible}
                            onChange={(value) => {
                              setSelectedCollectible(value);
                              if (value) {
                                setEditFormData({
                                  ...editFormData,
                                  name: value.name,
                                  entityClass: value.type,
                                  entityId: value.id,
                                });
                              }
                            }}
                            collectibles={collectibles}
                            loading={collectiblesLoading}
                            error={collectiblesError}
                            disabled={saving}
                            label="Item"
                            placeholder="Search items..."
                          />
                        ) : (
                          slot.name || '-'
                        )}
                      </TableCell>
                      <TableCell align="right" sx={{ width: '80px' }}>
                        {isCurrentlyEditing ? (
                          <TextField
                            size="small"
                            type="number"
                            inputProps={{ min: 0, max: 64 }}
                            value={editFormData.stackSize}
                            onChange={(e) => setEditFormData({ ...editFormData, stackSize: parseInt(e.target.value) || 0 })}
                            variant="outlined"
                          />
                        ) : (
                          slot.stackSize
                        )}
                      </TableCell>
                      <TableCell align="center">
                        <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'center' }}>
                          {isCurrentlyEditing ? (
                            <>
                              <IconButton
                                size="small"
                                onClick={handleSaveEdit}
                                disabled={saving || !selectedCollectible}
                                color="success"
                                title={!selectedCollectible ? 'Select an item first' : 'Save'}
                              >
                                <SaveIcon fontSize="small" />
                              </IconButton>
                              <IconButton
                                size="small"
                                onClick={handleCancelEdit}
                                disabled={saving}
                                title="Cancel"
                              >
                                <CancelIcon fontSize="small" />
                              </IconButton>
                            </>
                          ) : (
                            <>
                              <IconButton
                                size="small"
                                onClick={() => handleEditClick(slot, inventoryName)}
                                title="Edit"
                              >
                                <EditIcon fontSize="small" />
                              </IconButton>
                              <IconButton
                                size="small"
                                onClick={() => handleRemoveItem(inventoryName, slot.slotIndex)}
                                disabled={removing === `${inventoryName}-${slot.slotIndex}`}
                                color="error"
                                title="Delete"
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </>
                          )}
                        </Box>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      ))}
    </Box>
  );
};

export default PlayerInventoryTab;
