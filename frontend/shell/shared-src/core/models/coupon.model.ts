export interface Coupon {
  id: string;
  code: string;
  discountPercentage: number;
  expiresAt: string | null;
  maxUses: number | null;
  usesCount: number;
  active: boolean;
  isValid: boolean;
  createdAt: string;
}

export interface CreateCouponRequest {
  code: string;
  discountPercentage: number;
  expiresAt: string | null;
  maxUses: number | null;
}

export interface ValidateCouponResponse {
  valid: boolean;
  discountPercentage: number;
  discountAmount: number;
  error: string | null;
}
