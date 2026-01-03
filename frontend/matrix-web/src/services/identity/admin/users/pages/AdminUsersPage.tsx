import { useState } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import { IconRefresh } from "@shared/ui/icons/icons";
import { useAdminUsers } from "../hooks/useAdminUsers";
import UserCard from "../components/UserCard";
import UserAccessModal from "../components/UserAccessModal";
import "../admin-users-page.css";

export default function AdminUsersPage() {
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const {
    data,
    items,
    pageNumber,
    setPageNumber,
    isLoading,
    error,
    totalPages,
    refresh,
    toggleLock,
  } = useAdminUsers();

  return (
    <div className="mx-admin-page">
      <Card
        title="Users"
        subtitle="Directory & access"
        right={
          <div className="mx-admin-users__headerRight">
            <Button onClick={refresh} disabled={isLoading}>
              <IconRefresh /> Refresh
            </Button>
            <Button variant="primary" type="button" disabled>
              + Add user
            </Button>
          </div>
        }
      >
        {error ? <div className="mx-admin-users__error">{error}</div> : null}

        {isLoading && items.length === 0 ? (
          <div className="mx-admin-users__loading">
            <LoadingIndicator label="Loading users" />
          </div>
        ) : null}

        <div className="mx-admin-users__grid" role="list">
          {items.map((user) => (
            <UserCard
              key={user.id}
              user={user}
              onOpenAccess={setSelectedUserId}
              onToggleLock={toggleLock}
              isLoading={isLoading}
            />
          ))}
        </div>

        {data ? (
          <div className="mx-admin-users__pager">
            <div className="mx-admin-users__muted">
              Page <b>{pageNumber}</b> / {totalPages} • {data.totalCount} total
            </div>
            <Pagination
              page={pageNumber}
              totalPages={totalPages}
              onChange={setPageNumber}
              disabled={isLoading}
            />
          </div>
        ) : null}
      </Card>

      {selectedUserId ? (
        <UserAccessModal
          userId={selectedUserId}
          onClose={() => setSelectedUserId(null)}
        />
      ) : null}
    </div>
  );
}
