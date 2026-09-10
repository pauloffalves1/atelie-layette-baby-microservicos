/** One of AdminPermission's flag names (backend: AtelieBebe.SharedKernel.Auth.AdminPermission). */
export type AdminPermissionName =
  | 'Products'
  | 'Orders'
  | 'Coupons'
  | 'Reviews'
  | 'ContactMessages'
  | 'Newsletter'
  | 'Customers'
  | 'SiteContent'
  | 'Dashboard'
  | 'AdminManagement';

export interface AuthResponse {
  token: string;
  id: string;
  name: string;
  email: string;
  /** Populated for an admin login only — which feature areas this admin can use. */
  permissions?: AdminPermissionName[];
}

export interface AdminLoginResponse {
  requiresTwoFactor: boolean;
  adminId: string | null;
  auth: AuthResponse | null;
}

export interface TwoFactorSetup {
  secret: string;
  otpAuthUri: string;
}

export interface RegisterCustomerRequest {
  name: string;
  email: string;
  cpf: string;
  password: string;
  phone: string | null;
  addressStreet?: string | null;
  addressNumber?: string | null;
  addressComplement?: string | null;
  addressNeighborhood?: string | null;
  addressCity?: string | null;
  addressState?: string | null;
  addressZipCode?: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  /** Only set for an admin session — see AdminPermissionName. */
  permissions?: AdminPermissionName[];
}

export interface AdminSummary {
  id: string;
  name: string;
  email: string;
  twoFactorEnabled: boolean;
  permissions: AdminPermissionName[];
  createdAt: string;
}

export interface CreateAdminRequest {
  name: string;
  email: string;
  password: string;
  permissions: AdminPermissionName[];
}

export interface CustomerProfile {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  cpf: string | null;
  addressStreet: string | null;
  addressNumber: string | null;
  addressComplement: string | null;
  addressNeighborhood: string | null;
  addressCity: string | null;
  addressState: string | null;
  addressZipCode: string | null;
  emailVerified: boolean;
}
