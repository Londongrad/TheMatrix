// src/services/identity/api/auth/deviceInfo.ts
const DEVICE_ID_KEY = "matrix_device_id";
const DEVICE_NAME_KEY = "matrix_device_name";

function generateRandomId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  // fallback, если вдруг нет randomUUID
  return Date.now().toString(36) + Math.random().toString(36).substring(2, 10);
}

function detectDefaultDeviceName(): string {
  if (typeof navigator === "undefined") {
    return "Unknown device";
  }

  const ua = navigator.userAgent || "";
  let os = "Unknown OS";

  if (/Windows/i.test(ua)) os = "Windows";
  else if (/Mac OS X/i.test(ua)) os = "macOS";
  else if (/Android/i.test(ua)) os = "Android";
  else if (/iPhone|iPad|iPod/i.test(ua)) os = "iOS";
  else if (/Linux/i.test(ua)) os = "Linux";

  let browser = "Browser";

  if (/Edg\//.test(ua)) browser = "Edge";
  else if (/OPR\//.test(ua)) browser = "Opera";
  else if (/Chrome\//.test(ua)) browser = "Chrome";
  else if (/Firefox\//.test(ua)) browser = "Firefox";
  else if (/Safari\//.test(ua)) browser = "Safari";

  return `${browser} on ${os}`;
}

export function getOrCreateDeviceId(): string {
  if (typeof window === "undefined") {
    return "unknown-device";
  }

  const existing = window.localStorage.getItem(DEVICE_ID_KEY);
  if (existing) {
    return existing;
  }

  const id = generateRandomId();
  window.localStorage.setItem(DEVICE_ID_KEY, id);
  return id;
}

export function getOrCreateDeviceName(): string {
  if (typeof window === "undefined") {
    return "Unknown device";
  }

  const existing = window.localStorage.getItem(DEVICE_NAME_KEY);
  if (existing) {
    return existing;
  }

  const name = detectDefaultDeviceName();
  window.localStorage.setItem(DEVICE_NAME_KEY, name);
  return name;
}
