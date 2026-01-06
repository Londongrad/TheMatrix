// src/shared/lib/paging/pagingUtils.ts
export function getPageRange(
  pageNumber: number,
  pageSize: number,
  totalCount: number
): { start: number; end: number } {
  if (totalCount <= 0) return { start: 0, end: 0 };
  if (pageSize <= 0) return { start: 0, end: 0 };

  const start = (pageNumber - 1) * pageSize + 1;
  const end = Math.min(pageNumber * pageSize, totalCount);

  return { start, end };
}
