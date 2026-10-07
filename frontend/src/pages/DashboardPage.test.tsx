import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { jsonResponse, renderWithProviders, stubFetch } from '../test/utils';
import type { DocumentDto } from '../features/documents/types';
import { DashboardPage } from './DashboardPage';

const documents: DocumentDto[] = [
  { id: 'd1', fileName: 'contract.pdf', contentType: 'application/pdf', fileSize: 2048, createdAt: '2026-10-01T10:00:00.000Z' },
  { id: 'd2', fileName: 'invoice.pdf', contentType: 'application/pdf', fileSize: 1024, createdAt: '2026-10-02T10:00:00.000Z' },
];

describe('DashboardPage', () => {
  it('shows a loading state while the document list is pending', () => {
    vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>(() => {})));

    renderWithProviders(<DashboardPage />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading documents…');
  });

  it('shows an error state and recovers when the retry succeeds', async () => {
    const fetchMock = stubFetch(
      jsonResponse({ title: 'Service unavailable' }, 503),
      jsonResponse(documents),
    );

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Service unavailable');
    expect(screen.queryByText('contract.pdf')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));

    expect(await screen.findByText('contract.pdf')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[1][0]).toBe('/api/documents');
  });

  it('renders metrics and the recent documents table', async () => {
    stubFetch(jsonResponse(documents));

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText('contract.pdf')).toBeInTheDocument();
    expect(screen.getByText('invoice.pdf')).toBeInTheDocument();

    expect(screen.getByText('Documents').parentElement).toHaveTextContent('Documents2');
    expect(screen.getByText('Storage used').parentElement).toHaveTextContent('3.0 KB');

    expect(screen.getByRole('link', { name: 'Open contract.pdf' })).toHaveAttribute(
      'href',
      '/documents/d1',
    );
  });

  it('renders an empty state when the archive has no documents', async () => {
    stubFetch(jsonResponse([]));

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText('No documents yet')).toBeInTheDocument();
    expect(screen.getByText('Upload your first PDF to get started.')).toBeInTheDocument();
  });

  it('requests the document list from the REST API through the /api proxy path', async () => {
    const fetchMock = stubFetch(jsonResponse(documents));

    renderWithProviders(<DashboardPage />);

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(fetchMock.mock.calls[0][0]).toBe('/api/documents');
  });
});
