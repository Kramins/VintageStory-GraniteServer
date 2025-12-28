import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { PlayerService } from '../../services/PlayerService';
import type { PlayerSessionDTO } from '../../types/PlayerSessionDTO';
import type { JsonApiError, PaginationMeta } from '../../types/JsonApi';
import type { AppDispatch } from '../store';

interface PlayerSessionsState {
  sessions: PlayerSessionDTO[];
  loading: boolean;
  error: string | null;
  page: number;
  pageSize: number;
  hasMore: boolean;
  pagination?: PaginationMeta | null;
  apiErrors?: JsonApiError[];
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
}

const initialState: PlayerSessionsState = {
  sessions: [],
  loading: false,
  error: null,
  page: 0,
  pageSize: 20,
  hasMore: true,
  pagination: null,
  apiErrors: [],
  sortField: 'joinDate',
  sortDirection: 'desc',
};

const playerSessionsSlice = createSlice({
  name: 'playerSessions',
  initialState,
  reducers: {
    fetchSessionsStart(state: PlayerSessionsState) {
      state.loading = true;
      state.error = null;
    },
    fetchSessionsSuccess(
      state: PlayerSessionsState,
      action: PayloadAction<{ data: PlayerSessionDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[]; append: boolean }>
    ) {
      state.loading = false;
      const { data, append, pagination, errors } = action.payload;
      state.sessions = append ? [...state.sessions, ...data] : data;
      state.pagination = pagination ?? state.pagination;
      state.hasMore = pagination?.hasMore ?? data.length >= state.pageSize;
      state.apiErrors = errors ?? [];
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
      state.pagination = null;
      state.apiErrors = [];
    },
    setSort(state: PlayerSessionsState, action: PayloadAction<{ field: string; direction: 'asc' | 'desc' }>) {
      state.sortField = action.payload.field;
      state.sortDirection = action.payload.direction;
      state.page = 0;
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
  setSort,
} = playerSessionsSlice.actions;

export const fetchPlayerSessions = (playerId: string, page?: number) => async (dispatch: AppDispatch, getState: () => any) => {
  const { playerSessions } = getState();
  const currentPage = page ?? playerSessions.page;
  const pageSize = playerSessions.pageSize;
  const sortField = playerSessions.sortField ?? 'joinDate';
  const sortDirection = playerSessions.sortDirection ?? 'desc';

  dispatch(fetchSessionsStart());
  try {
    const { sessions, pagination, errors } = await PlayerService.getPlayerSessions(
      playerId,
      currentPage,
      pageSize,
      sortField,
      sortDirection
    );
    dispatch(
      fetchSessionsSuccess({
        data: sessions,
        pagination,
        errors,
        append: currentPage > 0,
      })
    );
    dispatch(setPage(currentPage));
  } catch (error: any) {
    dispatch(fetchSessionsFailure(error?.message ?? 'Failed to load sessions'));
  }
};

export default playerSessionsSlice.reducer;
