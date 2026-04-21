export interface CursorPagedResult<T> {
    items: T[];
    pageSize: number;
    nextCursor: string | null;
    hasNext: boolean;
}
