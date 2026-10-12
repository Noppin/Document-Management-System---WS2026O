import Keycloak from 'keycloak-js';
import { setAuthTokenProvider, setUnauthorizedHandler } from '../api/http';

const url = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8180';
const realm = import.meta.env.VITE_KEYCLOAK_REALM ?? 'dms';
const clientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'frontend';

export const keycloak = new Keycloak({ url, realm, clientId });

export async function initAuth(): Promise<void> {
  const authenticated = await keycloak.init({
    onLoad: 'login-required',
    pkceMethod: 'S256',
    checkLoginIframe: false,
  });

  if (!authenticated) {
    await keycloak.login();
    return;
  }

  setAuthTokenProvider(async () => {
    await keycloak.updateToken(30);
    return keycloak.token;
  });

  setUnauthorizedHandler(() => {
    void keycloak.login();
  });
}

export function getUsername(): string | undefined {
  const username = keycloak.tokenParsed?.preferred_username;
  return typeof username === 'string' ? username : undefined;
}

export function logout(): void {
  void keycloak.logout({ redirectUri: window.location.origin });
}
