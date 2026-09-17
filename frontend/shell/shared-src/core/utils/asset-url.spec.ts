import { smallAssetUrl } from './asset-url';

describe('smallAssetUrl', () => {
  it('points optimized uploads at their small copy', () => {
    expect(smallAssetUrl('/api/uploads/products/abc.webp')).toMatch(/\/api\/uploads\/products\/abc-sm\.webp$/);
  });

  it('leaves small copies, legacy formats, external and bundled images alone', () => {
    expect(smallAssetUrl('/api/uploads/products/abc-sm.webp')).toMatch(/abc-sm\.webp$/);
    expect(smallAssetUrl('/api/uploads/products/abc.jpg')).toMatch(/abc\.jpg$/);
    expect(smallAssetUrl('https://picsum.photos/seed/x/600.webp')).toBe('https://picsum.photos/seed/x/600.webp');
    expect(smallAssetUrl('/images/hero-fraldas.jpg')).toBe('/images/hero-fraldas.jpg');
  });
});
