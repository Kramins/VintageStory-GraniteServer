import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { PlayerService } from '../../services/PlayerService';
import type { PlayerSessionDTO } from '../../types/PlayerSessionDTO';
import type { AppDispatch } from '../store';

interface PlayerSessionsState {
  sessions: PlayerSessionDTO[];
  loading: boolean;
  error: string | null;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

const initialState: PlayerSessionsState = {
  sessions: [],
  loading: false,
  error: null,
  page: 0,
  pageSize: 20,
  hasMore: true,
};

const playerSessionsSlice = createSlice({
  name: 'playerSessions',
  initialState,
  reducers: {
    fetchSessionsStart(state: PlayerSessionsState) {
      state.loading = true;
      state.error = null;
    },
    fetchSessionsSuccess(state: PlayerSessionsState, action: PayloadAction<{ data: PlayerSessionDTO[]; append: boolean }>) {
      state.loading = false;
      const { data, append } = action.payload;
      state.sessions = append ? [...state.sessions, ...data] : data;
      state.hasMore = data.length >= state.pageSize;
    },
    fetchSessionsFailure(state: PlayerSessionsState, action: PayloadAction<string>) {
      state.loading = false;
      state.error = action.payload;
    },
    setPage(state: PlayerSessionsState, action: PayloadAction<number>) {
      state.page = action.payload;
    },
    setPageSize(state: PlayerSessionsState, action: PayloadAction<number>) {
      state.pageSize = action.payload;
    },
    clearSessions(state: PlayerSessionsState) {
      state.sessions = [];
      state.page = 0;
      state.hasMore = true;
      state.error = null;
    },
  },
});

export const {
  fetchSessionsStart,
  fetchSessionsSuccess,
  fetchSessionsFailure,
  setPage,
  setPageSize,
  clearSessions,
} = playerSessionsSlice.actions;

export const fetchPlayerSessions = (playerId: string, page?: number) => async (dispatch: AppDispatch, getState: () => any) => {
  const { playerSessions } = getState();
  const currentPage = page ?? playerSessions.page;
  const pageSize = playerSessions.pageSize;

  dispatch(fetchSessionsStart());
  try {
    const data = await PlayerService.getPlayerSessions(playerId, currentPage, pageSize);
    dispatch(fetchSessionsSuccess({ data, append: currentPage > 0 }));
    dispatch(setPage(currentPage));
  } catch (error: any) {
    dispatch(fetchSessionsFailure(error?.message ?? 'Failed to load sessions'));
  }
};

export default playerSessionsSlice.reducer;
