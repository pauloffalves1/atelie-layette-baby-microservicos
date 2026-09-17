import { Pipe, PipeTransform } from '@angular/core';
import { resolveAssetUrl, smallAssetUrl } from '../../core/utils/asset-url';

/** Template wrapper for resolveAssetUrl() — use on any `imageUrl`-style binding that might hold
 *  a backend-uploaded `/api/uploads/...` path (product photos, site images). `| assetUrl:'sm'` gives
 *  the small copy for cards and thumbnails. */
@Pipe({ name: 'assetUrl', standalone: true })
export class AssetUrlPipe implements PipeTransform {
  transform(url: string | null | undefined, size?: 'sm'): string {
    if (!url) return '';
    return size === 'sm' ? smallAssetUrl(url) : resolveAssetUrl(url);
  }
}
