import "./AdminButton.css";

export default function AdminButton({
  variant = "default",
  children,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "default" | "primary" | "danger";
}) {
  return (
    <button className={`mx-btn mx-btn--${variant}`} {...props}>
      {children}
    </button>
  );
}
