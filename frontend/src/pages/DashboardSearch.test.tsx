// Mit AI geschrieben.
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { setAuthTokenProvider } from '../api/http';
import { jsonResponse, renderWithProviders } from '../test/utils';
import { DashboardPage } from './DashboardPage';

afterEach(() => setAuthTokenProvider(undefined));

function mockSearch(response: () => Promise<Response>) {
  const fetchMock = vi.fn((path: string) => path === '/api/documents'
    ? Promise.resolve(jsonResponse([]))
    : response());
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

async function search() {
  renderWithProviders(<DashboardPage />);
  await userEvent.type(await screen.findByRole('textbox', { name: 'Search documents' }), 'Hello');
}

describe('Dashboard full-text search', () => {
  it('searches through the authenticated API client and links to the matching document', async () => {
    setAuthTokenProvider(() => 'test-access-token');
    const fetchMock = mockSearch(async () => jsonResponse([{ id: 'doc-1', fileName: 'HelloWorld.pdf' }]));
    await search();

    expect(await screen.findByRole('link', { name: /HelloWorld.pdf/ })).toHaveAttribute('href', '/documents/doc-1');
    expect(fetchMock).toHaveBeenCalledWith('/api/search?query=Hello', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer test-access-token' }),
    }));
  });

  it('shows a loading state during search', async () => {
    mockSearch(() => new Promise<Response>(() => {}));
    await search();
    expect(await screen.findByRole('status')).toHaveTextContent('Searching documents…');
  });

  it('shows an empty state when nothing matches', async () => {
    mockSearch(async () => jsonResponse([]));
    await search();
    expect(await screen.findByText('No search results')).toBeInTheDocument();
  });

  it('shows a service error and allows retrying', async () => {
    let unavailable = true;
    mockSearch(async () => unavailable
      ? jsonResponse({ detail: 'Search is temporarily unavailable' }, 503)
      : jsonResponse([{ id: 'doc-1', fileName: 'HelloWorld.pdf' }]));
    await search();
    expect(await screen.findByRole('alert')).toHaveTextContent('Search is temporarily unavailable');
    unavailable = false;
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByRole('link', { name: /HelloWorld.pdf/ })).toBeInTheDocument();
  });

  it('does not send requests for whitespace-only searches', async () => {
    const fetchMock = mockSearch(async () => jsonResponse([]));
    renderWithProviders(<DashboardPage />);
    await userEvent.type(await screen.findByRole('textbox', { name: 'Search documents' }), '   ');
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Enter a term to search the OCR index.')).toBeInTheDocument();
  });
});
