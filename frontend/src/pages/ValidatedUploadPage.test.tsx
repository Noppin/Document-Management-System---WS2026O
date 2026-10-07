import { fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { jsonResponse, renderWithProviders, stubFetch } from '../test/utils';
import type { DocumentDto } from '../features/documents/types';
import { ValidatedUploadPage } from './ValidatedUploadPage';

const uploadedDocument: DocumentDto = {
  id: 'd1',
  fileName: 'contract.pdf',
  contentType: 'application/pdf',
  fileSize: 2048,
  createdAt: '2026-10-01T10:00:00.000Z',
};

function selectFile(container: HTMLElement, file: File) {
  const input = container.querySelector('input[type="file"]');
  if (!input) throw new Error('file input not found');
  Object.defineProperty(input, 'files', { configurable: true, value: [file] });
  fireEvent.change(input);
}

function pdf(name: string, content = '%PDF-1.4') {
  return new File([content], name, { type: 'application/pdf' });
}

async function submitUpload() {
  await userEvent.click(screen.getByRole('button', { name: 'Upload PDF' }));
}

describe('ValidatedUploadPage', () => {
  it('rejects files that are not PDFs', async () => {
    const { container } = renderWithProviders(<ValidatedUploadPage />);

    selectFile(container, new File(['hello'], 'notes.txt', { type: 'text/plain' }));
    await submitUpload();

    expect(await screen.findByRole('alert')).toHaveTextContent('Only PDF documents are accepted.');
    expect(screen.queryByText('Upload complete.')).not.toBeInTheDocument();
  });

  it('rejects a PDF mime type with a non-.pdf extension', async () => {
    const { container } = renderWithProviders(<ValidatedUploadPage />);

    selectFile(container, new File(['hello'], 'notes.txt', { type: 'application/pdf' }));
    await submitUpload();

    expect(await screen.findByRole('alert')).toHaveTextContent('The file must use the .pdf extension.');
  });

  it('rejects an empty file', async () => {
    const { container } = renderWithProviders(<ValidatedUploadPage />);

    selectFile(container, new File([], 'empty.pdf', { type: 'application/pdf' }));
    await submitUpload();

    expect(await screen.findByRole('alert')).toHaveTextContent('The PDF must not be empty.');
  });

  it('rejects PDFs larger than 20 MB', async () => {
    const { container } = renderWithProviders(<ValidatedUploadPage />);
    const oversized = pdf('huge.pdf');
    Object.defineProperty(oversized, 'size', { value: 21 * 1024 * 1024 });

    selectFile(container, oversized);
    await submitUpload();

    expect(await screen.findByRole('alert')).toHaveTextContent('The PDF must not exceed 20 MB.');
  });

  it('uploads a valid PDF and links to the new document', async () => {
    const fetchMock = stubFetch(jsonResponse(uploadedDocument, 201));
    const { container } = renderWithProviders(<ValidatedUploadPage />);
    const file = pdf('contract.pdf');

    selectFile(container, file);
    await submitUpload();

    expect(await screen.findByText('Upload complete.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Open document' })).toHaveAttribute(
      'href',
      '/documents/d1',
    );

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('/api/documents');
    expect(init.method).toBe('POST');
    expect(init.body).toBeInstanceOf(FormData);
    expect((init.body as FormData).get('file')).toBe(file);
  });

  it('disables the submit button while the upload is pending', async () => {
    let resolveUpload!: (response: Response) => void;
    vi.stubGlobal(
      'fetch',
      vi.fn(() => new Promise<Response>((resolve) => { resolveUpload = resolve; })),
    );

    const { container } = renderWithProviders(<ValidatedUploadPage />);

    selectFile(container, pdf('contract.pdf'));
    await submitUpload();

    const pendingButton = await screen.findByRole('button', { name: 'Uploading…' });
    expect(pendingButton).toBeDisabled();

    resolveUpload(jsonResponse(uploadedDocument, 201));

    expect(await screen.findByText('Upload complete.')).toBeInTheDocument();
  });
});
