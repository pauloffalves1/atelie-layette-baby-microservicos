import { Injectable } from '@angular/core';

/**
 * Estimated PAC-style freight by state, relative to the atelier's origin (São Bernardo do
 * Campo/SP) — not a real Correios API quote (that requires a postage contract we don't have),
 * just a distance-based approximation so checkout can show a realistic total.
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
const EXTRA_ITEM_SURCHARGE = 2.5;

/**
 * Free-shipping subtotal threshold by destination — ateliê policy:
 * São Paulo (state) acima de R$599; Sul/Sudeste/Centro-Oeste acima de R$699; Norte/Nordeste
 * acima de R$799. Falls back to the Norte/Nordeste (highest) threshold for an unrecognized state.
 */
const FREE_SHIPPING_THRESHOLD_BY_REGION: Record<string, number> = {
  SP: 599,
  PR: 699,
  SC: 699,
  RS: 699,
  RJ: 699,
  MG: 699,
  ES: 699,
  DF: 699,
  GO: 699,
  MT: 699,
  MS: 699,
  BA: 799,
  SE: 799,
  AL: 799,
  PE: 799,
  PB: 799,
  RN: 799,
  CE: 799,
  PI: 799,
  MA: 799,
  AC: 799,
  AM: 799,
  AP: 799,
  PA: 799,
  RO: 799,
  RR: 799,
  TO: 799,
};

const DEFAULT_FREE_SHIPPING_THRESHOLD = 799;

@Injectable({ providedIn: 'root' })
export class ShippingService {
  /** Free-shipping subtotal threshold for a destination state — used to drive the cart's progress bar too. */
  freeShippingThreshold(state: string): number {
    return FREE_SHIPPING_THRESHOLD_BY_REGION[state.toUpperCase()] ?? DEFAULT_FREE_SHIPPING_THRESHOLD;
  }

  /**
   * Estimated freight for a destination state and total item count in the cart — the raw
   * Correios-style rate, no markup — or 0 once the cart subtotal reaches this state's free-shipping threshold.
   */
  estimate(state: string, totalItems: number, subtotal: number): number {
    if (subtotal >= this.freeShippingThreshold(state)) return 0;

    const baseRate = BASE_RATE_BY_REGION[state.toUpperCase()] ?? DEFAULT_RATE;
    const extraItems = Math.max(totalItems - 1, 0);
    return Math.round((baseRate + extraItems * EXTRA_ITEM_SURCHARGE) * 100) / 100;
  }
}
