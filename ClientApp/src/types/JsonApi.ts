export interface PaginationMeta {
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface JsonApiMeta {
  pagination?: PaginationMeta;
}

export interface JsonApiError {
  code: string;
  message: string;
}

export interface JsonApiDocument<T> {
  data: T;
  meta?: JsonApiMeta;
  errors?: JsonApiError[];
}
