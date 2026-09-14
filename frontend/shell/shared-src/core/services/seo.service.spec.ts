import { SeoService } from './seo.service';

describe('SeoService.trimDescription', () => {
  it('keeps short descriptions and collapses admin-typed line breaks', () => {
    expect(SeoService.trimDescription('Fralda de boca\n\n  com bordado floral.')).toBe('Fralda de boca com bordado floral.');
  });

  it('cuts long descriptions at a word boundary within the search-result length', () => {
    const long = 'Fralda de ombro em algodão macio com bordado personalizado do nome do bebê, '.repeat(4);
    const trimmed = SeoService.trimDescription(long);
    expect(trimmed.length).toBeLessThanOrEqual(160);
    expect(trimmed.endsWith('…')).toBe(true);
    expect(trimmed).not.toMatch(/\s…$/);
    expect(long.startsWith(trimmed.slice(0, -1))).toBe(true);
  });
});
