import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'atelie-bebe.cookie-consent';

export type CookieConsentChoice = 'accepted' | 'declined';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly choiceSignal = signal<CookieConsentChoice | null>(this.readStoredChoice());

  readonly choice = this.choiceSignal.asReadonly();

  /** Only true once the visitor has actively accepted — undecided is treated the same as declined. */
  get isAccepted(): boolean {
    return this.choiceSignal() === 'accepted';
  }

  get isDecided(): boolean {
    return this.choiceSignal() !== null;
  }

  accept(): void {
    this.setChoice('accepted');
  }

  decline(): void {
    this.setChoice('declined');
  }

  private setChoice(choice: CookieConsentChoice): void {
    localStorage.setItem(STORAGE_KEY, choice);
    this.choiceSignal.set(choice);
  }

  private readStoredChoice(): CookieConsentChoice | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw === 'accepted' || raw === 'declined' ? raw : null;
  }
}
