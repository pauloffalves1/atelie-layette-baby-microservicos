import { Product } from './product.model';

export interface WishlistItem {
  id: string;
  product: Product;
  createdAt: string;
}

export interface WishlistStatus {
  isFavorited: boolean;
}
