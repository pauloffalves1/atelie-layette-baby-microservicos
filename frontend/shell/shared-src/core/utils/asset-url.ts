import { environment } from '@shared/environment';

/** environment.apiUrl minus its trailing "/api" — the backend's origin (empty string in
 *  production, where the app and API share an origin behind Nginx; a full "http://host:port"
 *  in dev, where `ng serve` and `dotnet run` are on different ports). */
const API_ORIGIN = environment.apiUrl.replace(/\/api\/?$/, '');

/**
 * Resolves a URL returned by the backend (e.g. an uploaded image's `/api/uploads/...` path)
 * against the API's actual origin. In production this is a no-op (same-origin relative path);
 * in local dev it turns it into an absolute URL pointing at the API's own port, since a plain
 * `<img src>` doesn't go through HttpClient/environment.apiUrl the way API calls do.
 */
export function resolveAssetUrl(url: string): string {
  if (/^https?:\/\//i.test(url)) return url;
  return url.startsWith('/api/') ? `${API_ORIGIN}${url}` : url;
}

/**
 * The ~600 px copy Catalog writes next to every uploaded photo ("x.webp" → "x-sm.webp", see
 * ImageVariants on the backend) — for cards, thumbnails and avatars, which used to download the
 * 1600 px original. Anything else (legacy .jpg/.png, external URLs, the bundled /images) is returned
 * as-is, since only optimized uploads have a small copy.
 */
export function smallAssetUrl(url: string): string {
  const resolved = resolveAssetUrl(url);
  return /\/api\/uploads\/.+(?<!-sm)\.webp$/i.test(resolved) ? resolved.replace(/\.webp$/i, '-sm.webp') : resolved;
}
