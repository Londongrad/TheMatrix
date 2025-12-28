import type { ButtonHTMLAttributes } from "react";
import "./icon-button.css";

type IconButtonVariant = "default" | "danger";

type IconButtonSize = "md" | "sm";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: IconButtonVariant;
  size?: IconButtonSize;
};

export default function IconButton({
  variant = "default",
  size = "md",
  className = "",
  ...props
}: Props) {
  const classes = [
    "ui-icon-button",
    `ui-icon-button--${variant}`,
    size === "sm" ? "ui-icon-button--sm" : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return <button className={classes} {...props} />;
}
