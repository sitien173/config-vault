import { z } from 'zod';

export const ConfigResponseSchema = z.object({
  key: z.string(),
  value: z.string(),
});

export type ConfigResponse = z.infer<typeof ConfigResponseSchema>;

export const ConfigListResponseSchema = z.object({
  namespace: z.string(),
  configs: z.record(z.string(), z.string()),
});

export type ConfigListResponse = z.infer<typeof ConfigListResponseSchema>;

export const HealthResponseSchema = z.object({
  status: z.string(),
  vault: z.string(),
  timestamp: z.string().transform((s) => new Date(s)),
});

export type HealthResponse = z.infer<typeof HealthResponseSchema>;
