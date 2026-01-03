import { useState } from "react";
import { usePagedQuery } from "@shared/lib/paging/usePagedQuery";
import {
  getUsersPage,
  lockUser,
  unlockUser,
} from "@services/identity/api/admin/adminApi";
import type { UserListItemResponse } from "@services/identity/api/admin/adminTypes";

const PAGE_SIZE = 10;

export function useAdminUsers() {
  const [refreshKey, setRefreshKey] = useState(0);

  const { data, pageNumber, setPageNumber, isLoading, error } = usePagedQuery(
    getUsersPage,
    PAGE_SIZE,
    [refreshKey],
    {
      errorMessage: "Failed to load users",
    }
  );

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  const refresh = () => setRefreshKey((key) => key + 1);

  const toggleLock = async (user: UserListItemResponse) => {
    try {
      if (user.isLocked) {
        await unlockUser(user.id);
      } else {
        await lockUser(user.id);
      }
      refresh();
    } catch (error) {
      console.error(error);
    }
  };

  return {
    data,
    items,
    pageNumber,
    setPageNumber,
    isLoading,
    error,
    totalPages,
    refresh,
    toggleLock,
  };
}
