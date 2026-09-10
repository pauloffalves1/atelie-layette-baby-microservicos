export interface ProductReview {
  id: string;
  productId: string;
  customerName: string;
  rating: number;
  comment: string | null;
  photoUrl: string | null;
  createdAt: string;
}

export interface CreateReviewRequest {
  rating: number;
  comment: string | null;
  photoUrl?: string | null;
}

export interface ReviewEligibility {
  hasPurchased: boolean;
  alreadyReviewed: boolean;
}

export interface FeaturedReview {
  id: string;
  productName: string;
  productSlug: string;
  customerName: string;
  rating: number;
  comment: string | null;
  photoUrl: string | null;
  createdAt: string;
}

export interface AdminProductReview {
  id: string;
  productId: string;
  productName: string;
  customerName: string;
  rating: number;
  comment: string | null;
  photoUrl: string | null;
  approved: boolean;
  createdAt: string;
}
