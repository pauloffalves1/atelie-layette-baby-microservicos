export interface AuthResponse {
  token: string;
  id: string;
  name: string;
  email: string;
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
