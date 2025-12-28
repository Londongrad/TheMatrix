import type { ButtonHTMLAttributes } from "react";
import "./button.css";

type ButtonVariant = "default" | "primary" | "danger" | "success";

type ButtonSize = "md" | "sm";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  size?: ButtonSize;
};

export default function Button({
  variant = "default",
  size = "md",
  className = "",
  children,
  ...props
}: Props) {
  const classes = [
    "ui-button",
    `ui-button--${variant}`,
    size === "sm" ? "ui-button--sm" : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button className={classes} {...props}>
      {children}
    </button>
  );
}
