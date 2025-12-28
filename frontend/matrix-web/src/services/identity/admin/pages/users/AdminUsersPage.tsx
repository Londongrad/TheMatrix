import { useEffect, useMemo, useState } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import {
  IconEdit,
  IconLock,
  IconUnlock,
  IconOpen,
  IconRefresh,
} from "@shared/ui/icons/Icons";
import {
  getUsersPage,
  lockUser,
  unlockUser,
} from "@services/identity/api/admin/adminApi";
import type { UserListItemResponse } from "@services/identity/api/admin/adminTypes";
import "./admin-users-page.css";

export default function AdminUsersPage() {
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize] = useState(10);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<UserListItemResponse[]>([]);
  const [totalCount, setTotalCount] = useState<number | null>(null);

  const totalPages = useMemo(() => {
    if (!totalCount) return null;
    return Math.max(1, Math.ceil(totalCount / pageSize));
  }, [totalCount, pageSize]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await getUsersPage(pageNumber, pageSize);
      // PagedResult<T> у тебя в shared — формат может быть { items, totalCount, pageNumber... }
      // Ниже — максимально безопасно:
      const anyRes: any = res as any;
      setItems(anyRes.items ?? anyRes.data ?? []);
      setTotalCount(anyRes.totalCount ?? anyRes.totalItems ?? null);
    } catch (e: any) {
      setError(e?.message ?? "Failed to load users");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize]);

  const toggleLock = async (u: UserListItemResponse) => {
    try {
      if (u.isLocked) await unlockUser(u.id);
      else await lockUser(u.id);
      await load();
    } catch (e: any) {
      setError(e?.message ?? "Failed to update user status");
    }
  };

  return (
    <div className="mx-admin-page">
      <Card
        title="Users"
        subtitle="Directory & status"
        right={
          <div className="mx-admin-users__headerRight">
            <Button onClick={() => void load()} disabled={loading}>
              <IconRefresh /> Refresh
            </Button>
            <Button variant="primary" type="button">
              + Add user
            </Button>
          </div>
        }
      >
        {error ? <div className="mx-admin-users__error">{error}</div> : null}

        <div
          className="mx-admin-users__table"
          role="table"
          aria-label="Users table"
        >
          <div className="mx-admin-users__tr mx-admin-users__th" role="row">
            <div role="columnheader">User</div>
            <div role="columnheader">Email</div>
            <div role="columnheader">Confirmed</div>
            <div role="columnheader">Locked</div>
            <div role="columnheader">Created</div>
            <div role="columnheader" className="mx-admin-users__right">
              Actions
            </div>
          </div>

          {items.map((u) => (
            <div className="mx-admin-users__tr" role="row" key={u.id}>
              <div role="cell" className="mx-admin-users__userCell">
                <div className="mx-admin-users__avatar">
                  {u.username?.[0]?.toUpperCase() ?? "U"}
                </div>
                <div className="mx-admin-users__userMeta">
                  <div className="mx-admin-users__username">{u.username}</div>
                  <div className="mx-admin-users__id">{u.id}</div>
                </div>
              </div>

              <div role="cell" className="mx-admin-users__muted">
                {u.email}
              </div>
              <div role="cell">
                {u.isEmailConfirmed ? (
                  <Badge kind="ok">Yes</Badge>
                ) : (
                  <Badge kind="warn">No</Badge>
                )}
              </div>
              <div role="cell">
                {u.isLocked ? (
                  <Badge kind="bad">Locked</Badge>
                ) : (
                  <Badge kind="ok">Active</Badge>
                )}
              </div>
              <div role="cell" className="mx-admin-users__muted">
                {formatUtc(u.createdAtUtc)}
              </div>

              <div role="cell" className="mx-admin-users__right">
                <div className="mx-admin-users__actions">
                  <IconButton
                    title="Open"
                    onClick={() => alert(`Open ${u.id} (mock)`)}
                  >
                    <IconOpen />
                  </IconButton>
                  <IconButton
                    title="Edit"
                    onClick={() => alert(`Edit ${u.id} (mock)`)}
                  >
                    <IconEdit />
                  </IconButton>
                  <IconButton
                    variant={u.isLocked ? "default" : "danger"}
                    title={u.isLocked ? "Unlock" : "Lock"}
                    onClick={() => void toggleLock(u)}
                    disabled={loading}
                  >
                    {u.isLocked ? <IconUnlock /> : <IconLock />}
                  </IconButton>
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="mx-admin-users__pager">
          <div className="mx-admin-users__muted">
            Page <b>{pageNumber}</b>
            {totalPages ? ` / ${totalPages}` : ""}
            {totalCount !== null ? ` • ${totalCount} total` : ""}
          </div>

          <div className="mx-admin-users__pagerBtns">
            <Button
              disabled={loading || pageNumber <= 1}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            >
              Prev
            </Button>
            <Button
              disabled={
                loading || (totalPages !== null && pageNumber >= totalPages)
              }
              onClick={() => setPageNumber((p) => p + 1)}
            >
              Next
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}

function Badge({
  kind,
  children,
}: {
  kind: "ok" | "warn" | "bad";
  children: React.ReactNode;
}) {
  return <span className={`mx-admin-users__badge ${kind}`}>{children}</span>;
}

function formatUtc(utc: string) {
  // без зависимостей — просто коротко
  // (потом можно заменить на нормальную дату)
  return utc?.replace("T", " ").replace("Z", "");
}
