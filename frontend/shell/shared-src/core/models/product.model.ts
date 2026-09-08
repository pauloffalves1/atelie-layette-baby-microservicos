export interface Product {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  price: number;
  category: string;
  imageUrl: string | null;
  active: boolean;
  featured: boolean;
  isExclusive: boolean;
  imageUrls: string[];
  discountPercentage: number | null;
  promotionStartsAt: string | null;
  promotionEndsAt: string | null;
  isOnPromotion: boolean;
  effectivePrice: number;
}

export interface AdminProduct extends Product {
  allowedCustomerIds: string[];
}

export interface SetPromotionRequest {
  discountPercentage: number | null;
  startsAt: string | null;
  endsAt: string | null;
}

export interface BulkApplyPromotionRequest extends SetPromotionRequest {
  productIds: string[];
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  price: number;
  category: string;
  imageUrl: string | null;
  featured: boolean;
}

export interface UpdateProductRequest {
  name: string;
  description: string | null;
  price: number;
  category: string;
  imageUrl: string | null;
  featured: boolean;
}
