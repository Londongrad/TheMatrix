import type { ComponentProps } from "react";
import SessionsCard from "@services/identity/self/sessions/components/SessionsCard";

type Props = ComponentProps<typeof SessionsCard>;

const UserSettingsSessionsCard = (props: Props) => {
  return <SessionsCard {...props} />;
};

export default UserSettingsSessionsCard;
