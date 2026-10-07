import { z } from 'zod';

export const MAX_PDF_SIZE = 20 * 1024 * 1024;

export const uploadSchema = z.object({
  file: z
    .instanceof(File, { message: 'Choose a PDF file.' })
    .refine((file) => file.type === 'application/pdf', 'Only PDF documents are accepted.')
    .refine((file) => file.name.toLowerCase().endsWith('.pdf'), 'The file must use the .pdf extension.')
    .refine((file) => file.size > 0, 'The PDF must not be empty.')
    .refine((file) => file.size <= MAX_PDF_SIZE, 'The PDF must not exceed 20 MB.'),
});

export type UploadFormValues = z.infer<typeof uploadSchema>;
