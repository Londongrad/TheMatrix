import "./AdminIconButton.css";

export default function AdminIconButton({
  variant = "default",
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "default" | "danger";
}) {
  return <button className={`mx-ib mx-ib--${variant}`} {...props} />;
}
