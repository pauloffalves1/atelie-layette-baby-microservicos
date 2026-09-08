import { Pipe, PipeTransform } from '@angular/core';
import { resolveAssetUrl } from '../../core/utils/asset-url';

/** Template wrapper for resolveAssetUrl() — use on any `imageUrl`-style binding that might hold
 *  a backend-uploaded `/api/uploads/...` path (product photos, site images). */
@Pipe({ name: 'assetUrl', standalone: true })
export class AssetUrlPipe implements PipeTransform {
  transform(url: string | null | undefined): string {
    return url ? resolveAssetUrl(url) : '';
  }
}
