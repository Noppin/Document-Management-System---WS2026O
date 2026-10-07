import { z } from 'zod';

export const collectionSchema = z.object({
  name: z.string().trim().min(2, 'Use at least 2 characters.').max(200, 'Use at most 200 characters.'),
});

export type CollectionFormValues = z.infer<typeof collectionSchema>;
