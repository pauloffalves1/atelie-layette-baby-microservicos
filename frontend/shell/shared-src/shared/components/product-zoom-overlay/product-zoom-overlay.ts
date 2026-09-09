import { Component, inject } from '@angular/core';
import { ProductZoomService } from '../../../core/services/product-zoom.service';

/**
 * Full-screen product photo zoom, mounted once in PublicLayout (like CheckoutModal/CookieBanner)
 * instead of living inside the lazily-loaded product-detail route — local signals owned by a
 * lazy-loaded route component didn't reliably trigger a re-render in the zoneless production
 * build, while root-provided-service signals read by an eagerly-mounted shell component did.
 */
@Component({
  selector: 'app-product-zoom-overlay',
  standalone: true,
  templateUrl: './product-zoom-overlay.html',
})
export class ProductZoomOverlay {
  readonly zoom = inject(ProductZoomService);
}
