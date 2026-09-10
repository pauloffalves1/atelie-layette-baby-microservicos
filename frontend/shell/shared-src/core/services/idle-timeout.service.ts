import { Injectable, NgZone } from '@angular/core';

const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'] as const;

/**
 * Generic "log out after N minutes of no interaction" timer, reused by both the customer session
 * (PublicLayout) and the admin session (AdminLayout) — each starts/stops its own instance around
 * its own auth state, so browsing the storefront never times out an open admin tab or vice versa.
 */
@Injectable({ providedIn: 'root' })
export class IdleTimeoutService {
  private timeoutHandle: ReturnType<typeof setTimeout> | null = null;
  private timeoutMs = 0;
  private onTimeout: (() => void) | null = null;
  private readonly boundReset = () => this.resetTimer();

  constructor(private readonly zone: NgZone) {}

  /** Starts (or restarts, if already running) the idle timer. Listeners run outside Angular's zone so mere mouse movement doesn't trigger change detection every time. */
  start(timeoutMs: number, onTimeout: () => void): void {
    this.stop();
    this.timeoutMs = timeoutMs;
    this.onTimeout = onTimeout;

    this.zone.runOutsideAngular(() => {
      for (const eventName of ACTIVITY_EVENTS) {
        window.addEventListener(eventName, this.boundReset, { passive: true });
      }
    });
    this.resetTimer();
  }

  stop(): void {
    for (const eventName of ACTIVITY_EVENTS) {
      window.removeEventListener(eventName, this.boundReset);
    }
    if (this.timeoutHandle) clearTimeout(this.timeoutHandle);
    this.timeoutHandle = null;
  }

  private resetTimer(): void {
    if (this.timeoutHandle) clearTimeout(this.timeoutHandle);
    this.timeoutHandle = setTimeout(() => {
      const callback = this.onTimeout;
      this.stop();
      this.zone.run(() => callback?.());
    }, this.timeoutMs);
  }
}
