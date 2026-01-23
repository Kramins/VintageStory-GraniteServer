import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { PlayerService } from '../../services/PlayerService'
import type { AppDispatch } from '../../store/store';
import type { PlayerDTO } from '../../types/PlayerDTO';

interface PlayerState {
    players: PlayerDTO[];
    loading: boolean;
    error: string | null;
}

const initialState: PlayerState = {
    players: [],
    loading: false,
    error: null,
};

const playersSlice = createSlice({
    name: 'players',
    initialState,
    reducers: {
        fetchAllPlayersStart(state: PlayerState) {
            state.loading = true;
            state.error = null;
        },
        fetchAllPlayersSuccess(state: PlayerState, action: PayloadAction<PlayerDTO[]>) {
            state.loading = false;
            state.players = action.payload;
        },
        fetchAllPlayersFailure(state: PlayerState, action: PayloadAction<string>) {
            state.loading = false;
            state.error = action.payload;
        },
        // Event-based actions for real-time updates
        playerUpdated(state: PlayerState, action: PayloadAction<PlayerDTO>) {
            const idx = state.players.findIndex(
                p => p.serverId === action.payload.serverId && (p.id === action.payload.id || p.playerUID === action.payload.playerUID)
            );
            if (idx >= 0) {
                state.players[idx] = action.payload;
            } else {
                state.players.push(action.payload);
            }
        },
        playerBanned(state: PlayerState, action: PayloadAction<{ playerId: string; serverId: string }>) {
            const index = state.players.findIndex(p => p.id === action.payload.playerId && p.serverId === action.payload.serverId);
            if (index >= 0) {
                state.players[index] = {
                    ...state.players[index],
                    isBanned: true,
                };
            }
        },
        playerUnbanned(state: PlayerState, action: PayloadAction<{ playerId: string; serverId: string }>) {
            const index = state.players.findIndex(p => p.id === action.payload.playerId && p.serverId === action.payload.serverId);
            if (index >= 0) {
                state.players[index] = {
                    ...state.players[index],
                    isBanned: false,
                };
            }
        },
        playerWhitelisted(state: PlayerState, action: PayloadAction<{ playerId: string; serverId: string }>) {
            const index = state.players.findIndex(p => p.id === action.payload.playerId && p.serverId === action.payload.serverId);
            if (index >= 0) {
                state.players[index] = {
                    ...state.players[index],
                    isWhitelisted: true,
                };
            }
        },
        playerUnwhitelisted(state: PlayerState, action: PayloadAction<{ playerId: string; serverId: string }>) {
            const index = state.players.findIndex(p => p.id === action.payload.playerId && p.serverId === action.payload.serverId);
            if (index >= 0) {
                state.players[index] = {
                    ...state.players[index],
                    isWhitelisted: false,
                };
            }
        },
        playerJoined(state: PlayerState, action: PayloadAction<PlayerDTO>) {
            const idx = state.players.findIndex(
                p => p.serverId === action.payload.serverId && (p.id === action.payload.id || p.playerUID === action.payload.playerUID)
            );
            if (idx >= 0) {
                state.players[idx] = {
                    ...state.players[idx],
                    ...action.payload,
                    connectionState: 'Connected',
                };
            } else {
                state.players.push({ ...action.payload, connectionState: 'Connected' });
            }
        },
        playerLeft(state: PlayerState, action: PayloadAction<{ playerUID: string; serverId: string }>) {
            const idx = state.players.findIndex(p => p.serverId === action.payload.serverId && p.playerUID === action.payload.playerUID);
            if (idx >= 0) {
                state.players[idx] = {
                    ...state.players[idx],
                    connectionState: 'Disconnected',
                };
            }
        },
    },
});

export const {
    fetchAllPlayersStart,
    fetchAllPlayersSuccess,
    fetchAllPlayersFailure,
    playerUpdated,
    playerBanned,
    playerUnbanned,
    playerWhitelisted,
    playerUnwhitelisted,
    playerJoined,
    playerLeft,
} = playersSlice.actions;

export const fetchAllPlayers = (serverId?: string) => async (dispatch: AppDispatch) => {
    dispatch(fetchAllPlayersStart());
    try {
        // If serverId is provided, use it; otherwise we can fetch all
        // For now, we'll need the serverId to be provided
        if (!serverId) {
            throw new Error('serverId is required');
        }
        const players = await PlayerService.getAllPlayers(serverId);
        dispatch(fetchAllPlayersSuccess(players));
    } catch (error: any) {
        dispatch(fetchAllPlayersFailure(error.message));
    }
};

export default playersSlice.reducer;