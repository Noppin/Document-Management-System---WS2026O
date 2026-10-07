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

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw await readError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return await response.json() as T;
}
