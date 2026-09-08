// Plain relative import (not '@shared/environment' or any federated/shared package) — main.ts runs
// before Native Federation's import map is initialized, so anything beyond a local relative file
// fails to resolve here (see locale-pt.ts for the same constraint). Remote URLs are decided by
// hostname instead of an environment file for that reason.
const isLocalDev = location.hostname === 'localhost' || location.hostname === '127.0.0.1';

export const remoteEntries = {
  storefront: isLocalDev
    ? 'http://localhost:4201/remoteEntry.json'
    : '/mf/storefront/remoteEntry.json',
  admin: isLocalDev ? 'http://localhost:4202/remoteEntry.json' : '/mf/admin/remoteEntry.json',
};
