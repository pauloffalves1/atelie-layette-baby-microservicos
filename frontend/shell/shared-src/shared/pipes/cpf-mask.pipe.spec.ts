import { CpfMaskPipe } from './cpf-mask.pipe';

describe('CpfMaskPipe', () => {
  const pipe = new CpfMaskPipe();

  it('masks a raw 11-digit CPF, keeping only the middle block visible', () => {
    expect(pipe.transform('52998224725')).toBe('***.982.247-**');
  });

  it('masks a formatted CPF the same way', () => {
    expect(pipe.transform('529.982.247-25')).toBe('***.982.247-**');
  });

  it('returns an em dash for null/undefined/empty CPF', () => {
    expect(pipe.transform(null)).toBe('—');
    expect(pipe.transform(undefined)).toBe('—');
    expect(pipe.transform('')).toBe('—');
  });

  it('returns the original value unmasked if it is not a valid 11-digit CPF', () => {
    expect(pipe.transform('123')).toBe('123');
  });
});
