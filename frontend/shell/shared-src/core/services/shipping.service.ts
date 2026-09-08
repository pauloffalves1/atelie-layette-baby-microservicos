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

@Injectable({ providedIn: 'root' })
export class ShippingService {
  /** Estimated freight for a destination state and total item count in the cart — the raw Correios-style rate, no markup. */
  estimate(state: string, totalItems: number): number {
    const baseRate = BASE_RATE_BY_REGION[state.toUpperCase()] ?? DEFAULT_RATE;
    const extraItems = Math.max(totalItems - 1, 0);
    return Math.round((baseRate + extraItems * EXTRA_ITEM_SURCHARGE) * 100) / 100;
  }
}
