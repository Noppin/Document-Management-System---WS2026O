export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly details?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

async function readError(response: Response): Promise<ApiError> {
  let details: ProblemDetails | string | undefined;

  try {
    const contentType = response.headers.get('content-type') ?? '';
    details = contentType.includes('application/json')
      ? (await response.json() as ProblemDetails)
      : await response.text();
  } catch {
    details = undefined;
  }

  const message = typeof details === 'string'
    ? details || `Request failed with status ${response.status}`
    : details?.detail || details?.title || `Request failed with status ${response.status}`;

  return new ApiError(message, response.status, details);
}

type AuthTokenProvider = () => string | undefined | Promise<string | undefined>;
type UnauthorizedHandler = () => void;

let authTokenProvider: AuthTokenProvider | undefined;
let unauthorizedHandler: UnauthorizedHandler | undefined;

export function setAuthTokenProvider(provider?: AuthTokenProvider): void {
  authTokenProvider = provider;
}

export function setUnauthorizedHandler(handler?: UnauthorizedHandler): void {
  unauthorizedHandler = handler;
}

async function resolveAuthorizationHeader(): Promise<Record<string, string>> {
  if (!authTokenProvider) {
    return {};
  }

  const token = await authTokenProvider();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const authorization = await resolveAuthorizationHeader();

  const response = await fetch(`/api${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...authorization,
      ...init?.headers,
    },
  });

  if (response.status === 401) {
    unauthorizedHandler?.();
  }

  if (!response.ok) {
    throw await readError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return await response.json() as T;
}
