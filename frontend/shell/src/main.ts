import { initFederation } from '@angular-architects/native-federation';
import { remoteEntries } from './federation-urls';

initFederation(remoteEntries, {
  hostRemoteEntry: { url: './remoteEntry.json' },
})
  .catch((err) => console.error(err))
  .then((_) => import('./bootstrap'))
  .catch((err) => console.error(err));
