import createClient, { type Middleware } from "openapi-fetch";
import type { components, paths } from "./schema";

export type AdminAccount = components["schemas"]["AdminAccount"];

export const api = createClient<paths>({
  baseUrl: window.location.origin,
  credentials: "same-origin",
  fetch: (request) => globalThis.fetch(request),
});

let unauthorizedHandler: () => void = () => {};

export function onUnauthorized(handler: () => void): () => void {
  unauthorizedHandler = handler;
  return () => {
    unauthorizedHandler = () => {};
  };
}

const sessionGuard: Middleware = {
  onResponse({ request, response }) {
    if (response.status === 401 && !new URL(request.url).pathname.endsWith("/auth/login")) {
      unauthorizedHandler();
    }
  },
};

api.use(sessionGuard);
