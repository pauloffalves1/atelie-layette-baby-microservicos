export interface AuditLogEntry {
  id: string;
  adminName: string;
  action: string;
  details: string;
  createdAt: string;
}

export interface AuditLogFilters {
  admin?: string | null;
  action?: string | null;
  /** Calendar days (yyyy-MM-dd) in Brasília. */
  from?: string | null;
  to?: string | null;
  search?: string | null;
}

export interface AuditLogFilterOptions {
  admins: string[];
  actions: string[];
}
