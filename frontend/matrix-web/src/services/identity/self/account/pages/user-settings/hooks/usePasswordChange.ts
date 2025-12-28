// src/services/identity/account/pages/user-settings/hooks/usePasswordChange.ts
import { useState } from "react";
import { changePassword } from "@services/identity/api/self/account/accountApi";

export function usePasswordChange(token: string | null) {
  const [securityError, setSecurityError] = useState<string | null>(null);
  const [isSavingSecurity, setIsSavingSecurity] = useState(false);
  const [securitySaved, setSecuritySaved] = useState(false);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSecurityError(null);

    if (!token) {
      setSecurityError("You are not authenticated.");
      return;
    }

    if (!currentPassword || !newPassword || !confirmNewPassword) {
      setSecurityError("Please fill in all password fields.");
      return;
    }

    if (newPassword !== confirmNewPassword) {
      setSecurityError("New password and confirmation do not match.");
      return;
    }

    try {
      setIsSavingSecurity(true);
      setSecuritySaved(false);

      await changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      });

      setSecuritySaved(true);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmNewPassword("");

      setTimeout(() => setSecuritySaved(false), 2000);
    } catch (err: any) {
      console.error(err);
      setSecurityError(
        err?.message || "Failed to change password. Please try again."
      );
    } finally {
      setIsSavingSecurity(false);
    }
  };

  return {
    currentPassword,
    setCurrentPassword,
    newPassword,
    setNewPassword,
    confirmNewPassword,
    setConfirmNewPassword,
    securityError,
    isSavingSecurity,
    securitySaved,
    submit,
  };
}
