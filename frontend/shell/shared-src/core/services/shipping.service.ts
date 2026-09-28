import { Injectable } from '@angular/core';

/**
 * Estimated PAC-style freight by state, relative to the atelier's origin (SITE_ADDRESS in
 * core/constants/site.ts — São Bernardo do Campo/SP, also the PDF receipt's letterhead address) —
 * not a real Correios API quote (that requires a postage contract we don't have), just a
 * distance-based approximation so checkout can show a realistic total.
 */
const BASE_RATE_BY_REGION: Record<string, number> = {
  SP: 12.9,
  PR: 18.9,
  SC: 18.9,
  RS: 18.9,
  RJ: 18.9,
  MG: 18.9,
  ES: 18.9,
  DF: 24.9,
  GO: 24.9,
  MT: 24.9,
  MS: 24.9,
  BA: 24.9,
  SE: 24.9,
  AL: 24.9,
  PE: 24.9,
  PB: 24.9,
  RN: 24.9,
  CE: 24.9,
  PI: 24.9,
  MA: 24.9,
  AC: 32.9,
  AM: 32.9,
  AP: 32.9,
  PA: 32.9,
  RO: 32.9,
  RR: 32.9,
  TO: 32.9,
};

const DEFAULT_RATE = 24.9;

/**
 * Shipping weight the ateliê quotes per unit, as decided with the owner: a single piece counts as
 * 500 g, while a kit — or a bath towel on its own — counts as 1 kg, each adding its own kilo
 * (2 kits = 2 kg). These are posting weights, box and padding included, not the bare weight of the
 * fabric, which is why a fraldinha alone is already quoted at half a kilo.
 */
const KIT_WEIGHT_GRAMS = 1000;
const SINGLE_PIECE_WEIGHT_GRAMS = 500;

/**
 * Categories quoted at a full kilo without being named "Kit ...": "Boca, Ombro e Maternidade" is a
 * kit, and a bath towel on its own already fills a kilo's worth of box.
 */
const FULL_KILO_CATEGORIES = new Set(['Boca, Ombro e Maternidade', 'Toalha']);

function itemWeightGrams(category: string): number {
  const normalized = category.trim();
  return normalized.toLocaleLowerCase('pt-BR').startsWith('kit') || FULL_KILO_CATEGORIES.has(normalized)
    ? KIT_WEIGHT_GRAMS
    : SINGLE_PIECE_WEIGHT_GRAMS;
}

/** The base regional rate already covers one piece — half a kilo. */
const BASE_WEIGHT_GRAMS = 500;
/** Past the base weight the quote climbs half a kilo at a time, so the steps land on whole pieces and kits. */
const WEIGHT_STEP_GRAMS = 500;
/**
 * Surcharge per weight step, as a fraction of the destination's base rate — farther destinations
 * also pay more per extra 300g, not just a flat national add-on, so this scales with baseRate
 * rather than being its own by-state table.
 */
const WEIGHT_STEP_FACTOR = 0.22;

/**
 * São Bernardo do Campo (the ateliê's own city) gets the lowest free-shipping threshold (R$599),
 * below the rest of the state (R$799). Matched against the ViaCEP `localidade` field, normalized
 * (uppercase, no accents) for robust comparison.
 */
const SAO_BERNARDO_DO_CAMPO = 'SAO BERNARDO DO CAMPO';

const DIACRITICS_PATTERN = /[̀-ͯ]/g;

function normalizeCity(city: string): string {
  return city
    .normalize('NFD')
    .replace(DIACRITICS_PATTERN, '')
    .trim()
    .toUpperCase();
}

/**
 * Free-shipping subtotal threshold by destination — ateliê policy:
 * São Bernardo do Campo acima de R$599; resto do estado de São Paulo acima de R$799; Sul/Sudeste/Centro-Oeste
 * acima de R$899; Norte/Nordeste acima de R$999. Falls back to the Norte/Nordeste (highest) threshold
 * for an unrecognized state.
 */
const SAO_BERNARDO_DO_CAMPO_THRESHOLD = 599;

const FREE_SHIPPING_THRESHOLD_BY_REGION: Record<string, number> = {
  SP: 799,
  PR: 899,
  SC: 899,
  RS: 899,
  RJ: 899,
  MG: 899,
  ES: 899,
  DF: 899,
  GO: 899,
  MT: 899,
  MS: 899,
  BA: 999,
  SE: 999,
  AL: 999,
  PE: 999,
  PB: 999,
  RN: 999,
  CE: 999,
  PI: 999,
  MA: 999,
  AC: 999,
  AM: 999,
  AP: 999,
  PA: 999,
  RO: 999,
  RR: 999,
  TO: 999,
};

const DEFAULT_FREE_SHIPPING_THRESHOLD = 999;

@Injectable({ providedIn: 'root' })
export class ShippingService {
  /**
   * Free-shipping subtotal threshold for a destination — São Bernardo do Campo overrides the
   * state-level threshold; used to drive the cart's progress bar too.
   */
  freeShippingThreshold(state: string, city?: string): number {
    if (city && normalizeCity(city) === SAO_BERNARDO_DO_CAMPO) {
      return SAO_BERNARDO_DO_CAMPO_THRESHOLD;
    }
    return FREE_SHIPPING_THRESHOLD_BY_REGION[state.toUpperCase()] ?? DEFAULT_FREE_SHIPPING_THRESHOLD;
  }

  /**
   * Freight for a destination state/city and the cart's actual items — the raw Correios-style
   * rate, no markup — or 0 once the cart subtotal reaches this destination's free-shipping
   * threshold. Each piece is quoted at 500 g and each kit at 1 kg (see itemWeightGrams), so the
   * checkout can show a single "Frete" the ateliê is able to honour rather than an estimate that
   * moves when the package is actually weighed.
   */
  estimate(state: string, items: { category: string; quantity: number }[], subtotal: number, city?: string): number {
    if (subtotal >= this.freeShippingThreshold(state, city)) return 0;

    const baseRate = BASE_RATE_BY_REGION[state.toUpperCase()] ?? DEFAULT_RATE;

    const totalWeightGrams = items.reduce((sum, item) => sum + itemWeightGrams(item.category) * item.quantity, 0);
    const extraWeightGrams = Math.max(totalWeightGrams - BASE_WEIGHT_GRAMS, 0);
    const weightSteps = Math.ceil(extraWeightGrams / WEIGHT_STEP_GRAMS);

    return Math.round((baseRate + weightSteps * baseRate * WEIGHT_STEP_FACTOR) * 100) / 100;
  }
}
