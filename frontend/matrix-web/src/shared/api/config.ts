// src/shared/api/config.ts
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

if (!API_BASE_URL) {
  throw new Error("VITE_API_BASE_URL is not set");
}

export const API_AUTH_URL = API_BASE_URL + "/api/auth";
export const API_ACCOUNT_URL = API_BASE_URL + "/api/account";
export const API_SESSIONS_URL = API_BASE_URL + "/api/me/sessions";
export const API_POPULATION_URL = API_BASE_URL + "/api/population";
export const API_PERSON_URL = API_BASE_URL + "/api/person";
export const API_ADMIN_URL = API_BASE_URL + "/api/admin";
export const API_ADMIN_USERS_URL = API_ADMIN_URL + "/users";
