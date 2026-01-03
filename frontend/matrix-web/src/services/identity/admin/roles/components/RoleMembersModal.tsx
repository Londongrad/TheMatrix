import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Modal from "@shared/ui/components/Modal/Modal";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import Button from "@shared/ui/controls/Button/Button";
import { usePagedQuery } from "@shared/lib/paging/usePagedQuery";
import { getRoleMembersPage } from "@services/identity/api/admin/adminApi";
import type {
  RoleResponse,
  UserListItemResponse,
} from "@services/identity/api/admin/adminTypes";

function RoleMemberCard({ member }: { member: UserListItemResponse }) {
  return (
    <div className="mx-admin-roles__memberCard">
      <div className="mx-admin-roles__memberName">{member.username}</div>
      <div className="mx-admin-roles__memberEmail">{member.email}</div>
      <div className="mx-admin-roles__memberMeta">{member.id}</div>
    </div>
  );
}

export default function RoleMembersModal({
  role,
  onClose,
}: {
  role: RoleResponse;
  onClose: () => void;
}) {
  const pageSize = 8;
  const { data, pageNumber, setPageNumber, isLoading, error } = usePagedQuery(
    (page, size) => getRoleMembersPage(role.id, page, size),
    pageSize,
    [role.id],
    {
      errorMessage: "Failed to load members",
    }
  );

  const members = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  return (
    <Modal
      open
      title={`Members · ${role.name}`}
      onClose={onClose}
      footer={
        <Button variant="primary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      {isLoading && members.length === 0 ? (
        <div className="mx-admin-roles__loading">
          <LoadingIndicator label="Loading members" />
        </div>
      ) : null}

      <div className="mx-admin-roles__members">
        {members.map((member) => (
          <RoleMemberCard key={member.id} member={member} />
        ))}
      </div>

      {data ? (
        <div className="mx-admin-roles__membersPager">
          <Pagination
            page={pageNumber}
            totalPages={totalPages}
            onChange={setPageNumber}
            disabled={isLoading}
          />
        </div>
      ) : null}
    </Modal>
  );
}
