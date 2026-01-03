import { useEffect, useState } from "react";
import { getRolesCatalog } from "@services/identity/api/admin/adminApi";
import type { RoleResponse } from "@services/identity/api/admin/adminTypes";

export function useAdminRoles() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [roles, setRoles] = useState<RoleResponse[]>([]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await getRolesCatalog();
      setRoles(response);
    } catch (error: any) {
      setError(error?.message ?? "Failed to load roles");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  return {
    loading,
    error,
    roles,
    setError,
    setLoading,
    load,
  };
}
